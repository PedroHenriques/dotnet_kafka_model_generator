using Confluent.Kafka;
using Confluent.SchemaRegistry;
using KafkaUtilsV1 = Toolkit.Utils.Kafka<Tester.Models.EopItemTrackingShippingNexusV1.ShippingNexusKey, Tester.Models.EopItemTrackingShippingNexusV1.EopItemTrackingShippingNexusV1Value>;
using Tester.Models.EopItemTrackingShippingNexusV1;
using Toolkit.Types;
using Toolkit;
using Newtonsoft.Json;

namespace Tester;

public class EopItemTrackingShippingNexusV1
{
  private const string TOPIC_NAME = "eop.item-tracking.shipping-nexus.v1";
  private readonly IKafka<ShippingNexusKey, EopItemTrackingShippingNexusV1Value> _kafka;

  public EopItemTrackingShippingNexusV1(SchemaRegistryConfig schemaRegistryConfig, ProducerConfig producerConfig, ConsumerConfig consumerConfig)
  {
    KafkaInputs<ShippingNexusKey, EopItemTrackingShippingNexusV1Value> kafkaInputsEopItemTrackingShippingNexusV1 = KafkaUtilsV1.PrepareInputs(
      schemaRegistryConfig, producerConfig, consumerConfig, null, SchemaFormat.Json
    );

    this._kafka = new Kafka<ShippingNexusKey, EopItemTrackingShippingNexusV1Value>(kafkaInputsEopItemTrackingShippingNexusV1);
  }

  public Task Publish()
  {
    ShippingNexusKey key = new ShippingNexusKey
    {
      Id = "some test key",
    };
    EopItemTrackingShippingNexusV1Value value = new EopItemTrackingShippingNexusV1Value
    {
      Metadata = new Models.EopItemTrackingShippingNexusV1.Metadata
      {
        Interchange_id = Guid.NewGuid(),
        Timestamp = DateTimeOffset.Now,
        Correlation_id = "Correlation_id",
        Source = "Source",
        Data_type = "Data_type",
      },
      Shipping = new Shipping
      {
        Shipping_code = "Shipping_code",
        Additionals = new System.Collections.ObjectModel.Collection<additionals>(),
        Audit = new Audit
        {
          Created_datetime = DateTimeOffset.Now,
        },
        Sender_name = "Sender_name",
        Origin_address = "Origin_address",
        Origin_town_name = "Origin_town_name",
        Origin_postal_code = "Origin_postal_code",
        Origin_country_code = "PT",
        Recipient_name = "Recipient_name",
        Destin_address = "Destin_address",
        Destin_town_name = "Destin_town_name",
        Destin_postal_code = "Destin_postal_code",
        Destin_country_code = "PT",
        Items_count = 1,
        Items = new System.Collections.ObjectModel.Collection<items>
        {
          new items
          {
            Item_code = "Item_code",
          },
        },
        Manifest_datetime = DateTimeOffset.Now,
        Shipping_date = DateTimeOffset.Now,
        Shipping_type_code = "Shipping_type_code",
        Shipping_weight_declared_gr = 2.1,
      },
    };

    var tcs = new TaskCompletionSource();
    this._kafka.Publish(
      TOPIC_NAME,
      new Message<ShippingNexusKey, EopItemTrackingShippingNexusV1Value>
      {
        Key = key,
        Value = value,
      },
      (res, ex) =>
      {
        if (ex != null)
        {
          Console.WriteLine($"Publish on topic: {TOPIC_NAME} | Ex: {ex.Message}");
          tcs.SetException(ex);
          return;
        }

        Console.WriteLine($"Published event: topic {res.Topic} | partition: {res.Partition} | offset: {res.Offset}");
        tcs.SetResult();
      }
    );

    return tcs.Task;
  }

  public Task Subscribe()
  {
    var tcs = new TaskCompletionSource();

    var cts = new CancellationTokenSource();
    this._kafka.Subscribe(
      [TOPIC_NAME],
      (res, ex) =>
      {
        if (ex != null)
        {
          Console.WriteLine($"Consume from topic: {TOPIC_NAME} | Ex: {ex.Message}");
          tcs.SetException(ex);

          cts.Cancel();
          return Task.CompletedTask;
        }

        this._kafka.Commit(res);
        Console.WriteLine($"Consumed event: topic {res.Topic} | partition: {res.Partition} | offset: {res.Offset} | message key: {JsonConvert.SerializeObject(res.Message.Key)} | message value: {JsonConvert.SerializeObject(res.Message.Value)}");

        cts.Cancel();
        tcs.SetResult();
        return Task.CompletedTask;
      },
      cts
    );

    return tcs.Task;
  }
}