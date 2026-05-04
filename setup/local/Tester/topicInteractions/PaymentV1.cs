using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaUtilsV1 = Toolkit.Utils.Kafka<Tester.Models.PaymentV1.PaymentV1Key, Tester.Models.PaymentV1.PaymentV1Value>;
using Toolkit.Types;
using Toolkit;
using Newtonsoft.Json;
using Tester.Models.PaymentV1;

namespace Tester;

public class PaymentV1
{
  private const string TOPIC_NAME = "payment.v1";
  private readonly IKafka<PaymentV1Key, PaymentV1Value> _kafka;

  public PaymentV1(SchemaRegistryConfig schemaRegistryConfig, ProducerConfig producerConfig, ConsumerConfig consumerConfig)
  {
    KafkaInputs<PaymentV1Key, PaymentV1Value> kafkaInputsOrderV1 = KafkaUtilsV1.PrepareInputs(
      schemaRegistryConfig, producerConfig, consumerConfig, null, SchemaFormat.Avro
    );

    this._kafka = new Kafka<PaymentV1Key, PaymentV1Value>(kafkaInputsOrderV1);
  }

  public Task Publish()
  {
    PaymentV1Key key = new PaymentV1Key
    {
      paymentId = Guid.NewGuid().ToString(),
      sourceSystem = SourceSystem.WEB,
      region = "some region name",
    };
    PaymentV1Value value = new PaymentV1Value
    {
      paymentId = key.paymentId,
      status = PaymentStatus.COMPLETED,
      amount = 2.1,
      processedAt = DateTime.UtcNow,
      metadata = new Dictionary<string, string>
      {
        { "key", "value" },
      },
      paymentMethod = new CardDetails
      {
        brand = "mastercard",
        last4 = "9371",
      },
    };

    var tcs = new TaskCompletionSource<bool>();
    this._kafka.Publish(
      TOPIC_NAME,
      new Message<PaymentV1Key, PaymentV1Value>
      {
        Key = key,
        Value = value,
      },
      (res, ex) =>
      {
        if (ex != null)
        {
          Console.WriteLine($"---\nPublish on topic: {TOPIC_NAME} | Ex: {ex.Message}\n---");
          tcs.SetException(ex);
          return;
        }

        Console.WriteLine($"---\nPublished event: topic {res.Topic} | partition: {res.Partition} | offset: {res.Offset}\n---");
        tcs.SetResult(true);
      }
    );

    return tcs.Task;
  }

  public Task Subscribe(CancellationTokenSource cts)
  {
    var tcs = new TaskCompletionSource<bool>();

    this._kafka.Subscribe(
      [TOPIC_NAME],
      (res, ex) =>
      {
        if (ex != null)
        {
          Console.WriteLine($"---\nConsume from topic: {TOPIC_NAME} | Ex: {ex.Message}\n---");
          tcs.SetException(ex);

          return Task.CompletedTask;
        }

        this._kafka.Commit(res);
        Console.WriteLine($"---\nConsumed event: topic {res.Topic} | partition: {res.Partition} | offset: {res.Offset} | message key: {JsonConvert.SerializeObject(res.Message.Key)} | message value: {JsonConvert.SerializeObject(res.Message.Value)}\n---");

        tcs.SetResult(true);
        return Task.CompletedTask;
      },
      cts
    );

    return tcs.Task;
  }
}