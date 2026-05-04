using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaUtilsV1 = Toolkit.Utils.Kafka<Tester.Models.OrderV1.OrderV1Key, Tester.Models.OrderV1.OrderV1Value>;
using Toolkit.Types;
using Toolkit;
using Newtonsoft.Json;
using Tester.Models.OrderV1;

namespace Tester;

public class OrderV1
{
  private const string TOPIC_NAME = "order.v1";
  private readonly IKafka<OrderV1Key, OrderV1Value> _kafka;

  public OrderV1(SchemaRegistryConfig schemaRegistryConfig, ProducerConfig producerConfig, ConsumerConfig consumerConfig)
  {
    KafkaInputs<OrderV1Key, OrderV1Value> kafkaInputsOrderV1 = KafkaUtilsV1.PrepareInputs(
      schemaRegistryConfig, producerConfig, consumerConfig, null, SchemaFormat.Avro
    );

    this._kafka = new Kafka<OrderV1Key, OrderV1Value>(kafkaInputsOrderV1);
  }

  public Task Publish()
  {
    OrderV1Key key = new OrderV1Key
    {
      orderId = Guid.NewGuid().ToString(),
    };
    OrderV1Value value = new OrderV1Value
    {
      orderId = key.orderId,
      customerId = Guid.NewGuid().ToString(),
      amount = 4.7,
      currency = "EUR",
      createdAt = DateTime.UtcNow,
    };

    var tcs = new TaskCompletionSource<bool>();
    this._kafka.Publish(
      TOPIC_NAME,
      new Message<OrderV1Key, OrderV1Value>
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