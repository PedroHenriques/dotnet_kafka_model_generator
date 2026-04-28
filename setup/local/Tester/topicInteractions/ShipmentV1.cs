using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaUtilsV1 = Toolkit.Utils.Kafka<Tester.Models.ShipmentV1.ShipmentV1Key, Tester.Models.ShipmentV1.ShipmentV1Value>;
using Toolkit.Types;
using Toolkit;
using Newtonsoft.Json;
using Tester.Models.ShipmentV1;

namespace Tester;

public class ShipmentV1
{
  private const string TOPIC_NAME = "shipment.v1";
  private readonly IKafka<ShipmentV1Key, ShipmentV1Value> _kafka;

  public ShipmentV1(SchemaRegistryConfig schemaRegistryConfig, ProducerConfig producerConfig, ConsumerConfig consumerConfig)
  {
    KafkaInputs<ShipmentV1Key, ShipmentV1Value> kafkaInputsOrderV1 = KafkaUtilsV1.PrepareInputs(
      schemaRegistryConfig, producerConfig, consumerConfig, null, SchemaFormat.Avro
    );

    this._kafka = new Kafka<ShipmentV1Key, ShipmentV1Value>(kafkaInputsOrderV1);
  }

  public Task Publish()
  {
    ShipmentV1Key key = new ShipmentV1Key
    {
      shipmentId = Guid.NewGuid().ToString(),
      tenantId = Guid.NewGuid().ToString(),
    };
    ShipmentV1Value value = new ShipmentV1Value
    {
      shipmentId = key.shipmentId,
      status = ShipmentStatus.IN_TRANSIT,
      items = new ShipmentItem[]
      {
        new ShipmentItem
        {
          sku = Guid.NewGuid().ToString(),
          quantity = 2,
        },
      },
      destination = new Address
      {
        city = "Some city name",
        country = "Some country name",
        postalCode = "1234-567",
        street = "Some street name",
      },
    };

    var tcs = new TaskCompletionSource<bool>();
    this._kafka.Publish(
      TOPIC_NAME,
      new Message<ShipmentV1Key, ShipmentV1Value>
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