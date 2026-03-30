using NJsonSchema;

namespace ModelGenerator.Utils.Tests;

[Trait("Type", "Unit")]
public class CustomTypeNameGeneratorTests : IDisposable
{
  public CustomTypeNameGeneratorTests() { }

  public void Dispose() { }

  [Fact]
  public void Generate_IfTheSchemaIsActualAndNoTypeNameHintIsProvided_ItShouldReturnTheRootNameProvidedToTheConstructor()
  {
    var schema = new JsonSchema { Type = JsonObjectType.String };

    var sut = new ModelGenerator.Utils.CustomTypeNameGenerator("test root name");

    Assert.Equal("test root name", sut.Generate(schema, null, new List<string> { }));
  }

  [Fact]
  public void Generate_IfTheSchemaIsActualAndATypeNameHintIsProvided_ItShouldReturnTheProvidedTypeNameHint()
  {
    var schema = new JsonSchema { Type = JsonObjectType.String };

    var sut = new ModelGenerator.Utils.CustomTypeNameGenerator("test root name");

    Assert.Equal("some type name hint", sut.Generate(schema, "some type name hint", new List<string> { }));
  }

  [Fact]
  public void Generate_IfTheSchemaIsNotActualAndNoTypeNameHintIsProvided_ItShouldReturnTheRootNameProvidedToTheConstructor()
  {
    var referencedSchema = new JsonSchema { Type = JsonObjectType.String };
    var schema = new JsonSchema { Reference = referencedSchema };

    var sut = new ModelGenerator.Utils.CustomTypeNameGenerator("another root name");

    Assert.Equal("another root name", sut.Generate(schema, null, new List<string> { }));
  }

  [Fact]
  public void Generate_IfTheSchemaIsNotActualAndATypeNameHintIsProvided_ItShouldReturnTheProvidedTypeNameHint()
  {
    var referencedSchema = new JsonSchema { Type = JsonObjectType.String };
    var schema = new JsonSchema { Reference = referencedSchema };

    var sut = new ModelGenerator.Utils.CustomTypeNameGenerator("another root name");

    Assert.Equal("some name", sut.Generate(schema, "some name", new List<string> { }));
  }
}
