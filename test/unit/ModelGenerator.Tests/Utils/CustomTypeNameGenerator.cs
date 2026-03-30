using NJsonSchema;

namespace ModelGenerator.Utils.Tests;

[Trait("Type", "Unit")]
public class CustomTypeNameGeneratorTests : IDisposable
{
  public CustomTypeNameGeneratorTests() { }

  public void Dispose() { }

  [Fact]
  public void Generate_ItShouldReturnTheRootNameProvidedToTheConstructor()
  {
    var schema = new JsonSchema();

    var sut = new ModelGenerator.Utils.CustomTypeNameGenerator("test root name");
  }
}
