using StackExchange.Redis;
using Confluent.Kafka;
using Toolkit;
using Toolkit.Types;
using MongodbUtils = Toolkit.Utils.Mongodb;
using RedisUtils = Toolkit.Utils.Redis;
using KafkaUtils = Toolkit.Utils.Kafka<MyKey, MyValue>;
using Confluent.SchemaRegistry;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapGet(
  "/",
  async () =>
  {
    MyValue document = new MyValue
    {
      Prop1 = "value 1",
      Prop2 = "value 2",
    };

    string? mongoConStr = Environment.GetEnvironmentVariable("MONGO_CON_STR");
    if (mongoConStr == null)
    {
      throw new Exception("Could not get the 'MONGO_CON_STR' environment variable");
    }
    MongoDbInputs mongodbInputs = MongodbUtils.PrepareInputs(mongoConStr);
    IMongodb mongoDb = new Mongodb(mongodbInputs);
    await mongoDb.InsertOne<MyValue>("myTestDb", "myTestCol", document);


    string? redisConStr = Environment.GetEnvironmentVariable("REDIS_CON_STR");
    if (redisConStr == null)
    {
      throw new Exception("Could not get the 'REDIS_CON_STR' environment variable");
    }
    ConfigurationOptions redisConOpts = new ConfigurationOptions
    {
      EndPoints = { redisConStr },
      Password = "password",
    };
    RedisInputs redisInputs = RedisUtils.PrepareInputs(redisConOpts, "test consumer group");
    ICache redis = new Redis(redisInputs);
    await redis.Set("prop1", document.Prop1);
    await redis.Set("prop2", document.Prop2);


    string? schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_URL");
    if (schemaRegistryUrl == null)
    {
      throw new Exception("Could not get the 'KAFKA_SCHEMA_REGISTRY_URL' environment variable");
    }
    SchemaRegistryConfig schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

    string? kafkaConStr = Environment.GetEnvironmentVariable("KAFKA_CON_STR");
    if (kafkaConStr == null)
    {
      throw new Exception("Could not get the 'KAFKA_CON_STR' environment variable");
    }
    var producerConfig = new ProducerConfig
    {
      BootstrapServers = kafkaConStr,
    };

    KafkaInputs<MyKey, MyValue> kafkaInputs = KafkaUtils.PrepareInputs(
      schemaRegistryConfig, producerConfig
    );
    IKafka<MyKey, MyValue> kafka = new Kafka<MyKey, MyValue>(kafkaInputs);
    kafka.Publish(
      "myTestTopic",
      new Message<MyKey, MyValue> { Key = new MyKey { Id = Guid.NewGuid().ToString() }, Value = document },
      (res, ex) =>
      {
        if (ex != null) { Console.WriteLine($"Exception inserting in Kafka. Message: '{ex.Message}' | Trace: '{ex.StackTrace}'"); }
        Console.WriteLine($"Event inserted in partition: {res.Partition} and offset: {res.Offset}.");
      }
    );


    return Results.Ok("Hello World!");
  }
);

app.Run();

public class MyKey
{
  [JsonPropertyName("id")]
  [JsonProperty("id")]
  public required string Id { get; set; }
}

public class MyValue
{
  [JsonPropertyName("prop1")]
  [JsonProperty("prop1")]
  public required string Prop1 { get; set; }

  [JsonPropertyName("prop2")]
  [JsonProperty("prop2")]
  public required string Prop2 { get; set; }
}