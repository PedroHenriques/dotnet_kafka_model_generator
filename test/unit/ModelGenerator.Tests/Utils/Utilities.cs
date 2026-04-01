using Newtonsoft.Json;

namespace ModelGenerator.Utils.Tests;

[Trait("Type", "Unit")]
public class UtilitiesTests : IDisposable
{
  public UtilitiesTests() { }

  public void Dispose() { }

  [Theory]
  [InlineData(
    new[] {
      "--help", "--schema-content", "schema json", "--schema-file", "schema file",
      "--output-dir", "output dir", "--namespace", "some namespace",
      "--root-class-name", "TestClassName", "--schema-type", "json"
    },
    "{\"ShowHelp\":true,\"SchemaContent\":\"schema json\",\"SchemaFile\":\"schema file\",\"SchemaType\":0,\"OutputDir\":\"output dir\",\"Namespace\":\"some namespace\",\"RootClassName\":\"TestClassName\"}"
  )]
  [InlineData(
    new[] {
      "-h", "--root-class-name", "SomeTestClassName"
    },
    "{\"ShowHelp\":true,\"SchemaContent\":null,\"SchemaFile\":null,\"SchemaType\":null,\"OutputDir\":null,\"Namespace\":null,\"RootClassName\":\"SomeTestClassName\"}"
  )]
  public void ParseArgs_ItShouldReturnTheExpectedObject(string[] inputArgs, string expectedSerializedOutput)
  {
    Assert.Equal(
      expectedSerializedOutput,
      JsonConvert.SerializeObject(ModelGenerator.Utils.Utilities.ParseArgs(inputArgs))
    );
  }

  [Fact]
  public void ParseArgs_IfAnInvalidArgumentIsProvided_ItShouldThrowAnException()
  {
    var ex = Assert.Throws<ArgumentException>(() => ModelGenerator.Utils.Utilities.ParseArgs(["-h", "--something"]));
    Assert.Equal("Unknown argument: --something", ex.Message);
  }

  [Fact]
  public void ParseArgs_IfTheLastArgumentThatRequiresAValueDoesNotHaveOne_ItShouldThrowAnException()
  {
    var ex = Assert.Throws<ArgumentException>(() => ModelGenerator.Utils.Utilities.ParseArgs(["--namespace", "name", "-h", "--root-class-name"]));
    Assert.Equal("Missing value for argument: --root-class-name", ex.Message);
  }

  [Fact]
  public void ParseArgs_IfAnArgumentThatRequiresAValueDoesNotHaveOne_ItShouldThrowAnException()
  {
    var ex = Assert.Throws<ArgumentException>(() => ModelGenerator.Utils.Utilities.ParseArgs(["--root-class-name", "name", "--namespace", "-h", "--schema-file", "some/file"]));
    Assert.Equal("Missing value for argument: --namespace", ex.Message);
  }
}
