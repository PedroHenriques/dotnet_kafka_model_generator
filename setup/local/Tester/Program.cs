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
  AutoOffsetReset = AutoOffsetReset.Latest,
  EnableAutoCommit = false,
};

var tasks = new List<Task>();

var eopItemTrackingShippingNexusV1 = new EopItemTrackingShippingNexusV1(schemaRegistryConfig, producerConfigJson, consumerConfigJson);
tasks.Add(eopItemTrackingShippingNexusV1.Publish());


await Task.WhenAll(tasks);