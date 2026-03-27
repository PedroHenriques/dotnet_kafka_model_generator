using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaUtilsV1 = Toolkit.Utils.Kafka<Tester.Models.EopItemTrackingExpObjectV1.ObjectKey, Tester.Models.EopItemTrackingExpObjectV1.EopItemTrackingExpObjectV1Value>;
using Toolkit.Types;
using Toolkit;
using Tester.Models.EopItemTrackingExpObjectV1;
using Newtonsoft.Json;

namespace Tester;

public class EopItemTrackingExpObjectV1
{
  private const string TOPIC_NAME = "eop.item-tracking.exp-object.v1";
  private readonly IKafka<ObjectKey, EopItemTrackingExpObjectV1Value> _kafka;

  public EopItemTrackingExpObjectV1(SchemaRegistryConfig schemaRegistryConfig, ProducerConfig producerConfig, ConsumerConfig consumerConfig)
  {
    KafkaInputs<ObjectKey, EopItemTrackingExpObjectV1Value> kafkaInputsEopItemTrackingShippingNexusV1 = KafkaUtilsV1.PrepareInputs(
      schemaRegistryConfig, producerConfig, consumerConfig, null, SchemaFormat.Json
    );

    this._kafka = new Kafka<ObjectKey, EopItemTrackingExpObjectV1Value>(kafkaInputsEopItemTrackingShippingNexusV1);
  }

  public Task Publish()
  {
    ObjectKey key = new ObjectKey
    {
      Id = "some test key",
    };
    EopItemTrackingExpObjectV1Value value = new EopItemTrackingExpObjectV1Value
    {
      Metadata = new Models.EopItemTrackingExpObjectV1.Metadata
      {
        Interchange_id = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.Now,
        Correlation_id = "Correlation_id",
        Source = "Source",
        Data_type = "Data_type",
      },
      Shipping = new Shipping
      {
        Shipping_code = "Shipping_code",
      },
    };

    var tcs = new TaskCompletionSource();
    this._kafka.Publish(
      TOPIC_NAME,
      new Message<ObjectKey, EopItemTrackingExpObjectV1Value>
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
        tcs.SetResult();
      }
    );

    return tcs.Task;
  }

  public Task Subscribe(CancellationTokenSource cts)
  {
    var tcs = new TaskCompletionSource();

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

        tcs.SetResult();
        return Task.CompletedTask;
      },
      cts
    );

    return tcs.Task;
  }
}