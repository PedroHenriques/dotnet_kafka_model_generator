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
    if (options.SchemaType == null)
    {
      throw new Exception("Missing required argument: --schema-type");
    }

    var hasSchemaContent = string.IsNullOrWhiteSpace(options.SchemaContent) == false;
    var hasSchemaFile = string.IsNullOrWhiteSpace(options.SchemaFile) == false;

    if (hasSchemaContent == false && hasSchemaFile == false)
    {
      throw new Exception("You must provide either --schema-json or --schema-file.");
    }

    if (hasSchemaContent && hasSchemaFile)
    {
      throw new Exception("Arguments --schema-json and --schema-file cannot be used together.");
    }
  }
}