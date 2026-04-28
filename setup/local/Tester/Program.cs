using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Tester;

string? schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_URL");
if (schemaRegistryUrl == null)
{
  throw new Exception("Could not get the 'KAFKA_SCHEMA_REGISTRY_URL' environment variable");
}
var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

string? kafkaConStr = Environment.GetEnvironmentVariable("KAFKA_CON_STR");
if (kafkaConStr == null)
{
  throw new Exception("Could not get the 'KAFKA_CON_STR' environment variable");
}

var producerConfigJson = new ProducerConfig
{
  BootstrapServers = kafkaConStr,
  AllowAutoCreateTopics = false,
};
var consumerConfigJson = new ConsumerConfig
{
  BootstrapServers = kafkaConStr,
  GroupId = "example-consumer-group-json",
  AutoOffsetReset = AutoOffsetReset.Earliest,
  EnableAutoCommit = false,
};

var tasks = new List<Task>();
var cts = new CancellationTokenSource(10000);

var eopItemTrackingShippingNexusV1 = new EopItemTrackingShippingNexusV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(eopItemTrackingShippingNexusV1.Publish());
tasks.Add(eopItemTrackingShippingNexusV1.Subscribe(cts));

var eopItemTrackingExpObjectV1 = new EopItemTrackingExpObjectV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(eopItemTrackingExpObjectV1.Publish());
tasks.Add(eopItemTrackingExpObjectV1.Subscribe(cts));

var eopItemTrackingShippingMailV1 = new EopItemTrackingShippingMailV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(eopItemTrackingShippingMailV1.Publish());
tasks.Add(eopItemTrackingShippingMailV1.Subscribe(cts));

var orderV1 = new OrderV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(orderV1.Publish());
tasks.Add(orderV1.Subscribe(cts));

var paymentV1 = new PaymentV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(paymentV1.Publish());
tasks.Add(paymentV1.Subscribe(cts));

var shipmentV1 = new ShipmentV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(shipmentV1.Publish());
tasks.Add(shipmentV1.Subscribe(cts));

await Task.WhenAny(new List<Task>
{
  Task.Delay(10000, cts.Token),
  Task.WhenAll(tasks)
});