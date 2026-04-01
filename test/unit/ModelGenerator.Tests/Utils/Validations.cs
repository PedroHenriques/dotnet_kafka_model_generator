using ModelGenerator.Types;

namespace ModelGenerator.Utils.Tests;

[Trait("Type", "Unit")]
public class ValidationsTests : IDisposable
{
  public ValidationsTests() { }

  public void Dispose() { }

  [Fact]
  public void ValidateArgs_ItShouldNotThrow()
  {
    CliOptions options = new CliOptions
    {
      OutputDir = "some/out/dir",
      SchemaFile = "some/schema/file",
    };

    var ex = Record.Exception(() => Validations.ValidateArgs(options));

    Assert.Null(ex);
  }

  [Fact]
  public void ValidateArgs_IftheOutputDirIsProvided_ItShouldThrowAnExceptionWithTheExpectedMessage()
  {
    CliOptions options = new CliOptions
    {
      SchemaFile = "some/schema/file",
    };

    var ex = Assert.Throws<Exception>(() => Validations.ValidateArgs(options));
    Assert.Equal("Missing required argument: --output-dir", ex.Message);
  }

  [Fact]
  public void ValidateArgs_IfNeitherASchemaJsonContentNorASchemaFileAreProvided_ItShouldThrowAnExceptionWithTheExpectedMessage()
  {
    CliOptions options = new CliOptions
    {
      OutputDir = "something",
    };

    var ex = Assert.Throws<Exception>(() => Validations.ValidateArgs(options));
    Assert.Equal("You must provide either --schema-json or --schema-file.", ex.Message);
  }

  [Fact]
  public void ValidateArgs_IfBothASchemaJsonContentAndASchemaFileAreProvided_ItShouldThrowAnExceptionWithTheExpectedMessage()
  {
    CliOptions options = new CliOptions
    {
      OutputDir = "something",
      SchemaJson = "json stuff",
      SchemaFile = "file/path",
    };

    var ex = Assert.Throws<Exception>(() => Validations.ValidateArgs(options));
    Assert.Equal("Arguments --schema-json and --schema-file cannot be used together.", ex.Message);
  }
}
