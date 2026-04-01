using ModelGenerator.Types;

namespace ModelGenerator.Utils;

public static class Validations
{
  public static void ValidateArgs(CliOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.OutputDir))
    {
      throw new Exception("Missing required argument: --output-dir");
    }

    var hasSchemaJson = string.IsNullOrWhiteSpace(options.SchemaJson) == false;
    var hasSchemaFile = string.IsNullOrWhiteSpace(options.SchemaFile) == false;

    if (hasSchemaJson == false && hasSchemaFile == false)
    {
      throw new Exception("You must provide either --schema-json or --schema-file.");
    }

    if (hasSchemaJson && hasSchemaFile)
    {
      throw new Exception("Arguments --schema-json and --schema-file cannot be used together.");
    }
  }
}