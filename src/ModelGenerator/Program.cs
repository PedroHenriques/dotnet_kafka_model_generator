using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Newtonsoft.Json.Linq;
using ModelGenerator.Types;
using System.Diagnostics.CodeAnalysis;
using ModelGenerator.Utils;

[ExcludeFromCodeCoverage(Justification = "Not unit testable due to instantiating classes for tool setup.")]
internal class Program
{
  private static async Task Main(string[] args)
  {
    CliOptions options;
    try
    {
      options = Utilities.ParseArgs(args);
    }
    catch (ArgumentException ex)
    {
      Console.Error.WriteLine(ex.Message);
      Console.Error.WriteLine();
      Utilities.PrintUsage();
      Environment.ExitCode = 1;
      return;
    }

    if (options.ShowHelp)
    {
      Utilities.PrintUsage();
      return;
    }

    if (string.IsNullOrWhiteSpace(options.OutputDir))
    {
      Console.Error.WriteLine("Missing required argument: --output-dir");
      Console.Error.WriteLine();
      Utilities.PrintUsage();
      Environment.ExitCode = 1;
      return;
    }

    var hasSchemaJson = string.IsNullOrWhiteSpace(options.SchemaJson) == false;
    var hasSchemaFile = string.IsNullOrWhiteSpace(options.SchemaFile) == false;

    if (hasSchemaJson == false && hasSchemaFile == false)
    {
      Console.Error.WriteLine("You must provide either --schema-json or --schema-file.");
      Console.Error.WriteLine();
      Utilities.PrintUsage();
      Environment.ExitCode = 1;
      return;
    }

    if (hasSchemaJson && hasSchemaFile)
    {
      Console.Error.WriteLine("Arguments --schema-json and --schema-file cannot be used together.");
      Console.Error.WriteLine();
      Utilities.PrintUsage();
      Environment.ExitCode = 1;
      return;
    }

    string schemaJson;
    if (hasSchemaFile)
    {
      if (File.Exists(options.SchemaFile!) == false)
      {
        Console.Error.WriteLine($"Schema file not found: {options.SchemaFile}");
        Environment.ExitCode = 1;
        return;
      }

      schemaJson = await File.ReadAllTextAsync(options.SchemaFile!);
    }
    else
    {
      schemaJson = options.SchemaJson!;
    }

    var outputDir = options.OutputDir!;
    var targetNamespace = string.IsNullOrWhiteSpace(options.Namespace)
      ? "Generated.Kafka.Models"
      : options.Namespace!;

    var explicitRootClassName = string.IsNullOrWhiteSpace(options.RootClassName)
      ? null
      : options.RootClassName;

    Directory.CreateDirectory(outputDir);

    try
    {
      var parsedRoot = JObject.Parse(schemaJson);

      var originalTitle = parsedRoot["title"]?.Value<string>();
      var rootName = string.IsNullOrWhiteSpace(explicitRootClassName) == false
        ? explicitRootClassName!
        : string.IsNullOrWhiteSpace(originalTitle) == false
          ? Models.ToPascalCaseIdentifier(originalTitle!)
          : "KafkaMessage";

      if (string.IsNullOrWhiteSpace(parsedRoot["title"]?.Value<string>()))
      {
        parsedRoot["title"] = rootName;
      }

      var normalizedSchemaJson = parsedRoot.ToString();
      var schema = await JsonSchema.FromJsonAsync(normalizedSchemaJson);

      var settings = new CSharpGeneratorSettings
      {
        Namespace = targetNamespace,
        ClassStyle = CSharpClassStyle.Poco,
        JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
        GenerateDataAnnotations = false,
        GenerateJsonMethods = false,
        RequiredPropertiesMustBeDefined = true,
        GenerateOptionalPropertiesAsNullable = true,
        GenerateNullableReferenceTypes = true,
        TypeAccessModifier = "public",
        TypeNameGenerator = new CustomTypeNameGenerator(rootName),
        PropertyNameGenerator = new CSharpPropertyNameGenerator(),
      };

      var generator = new CSharpGenerator(schema, settings);
      var generatedCode = generator.GenerateFile();

      generatedCode = Models.AddHeader(generatedCode, parsedRoot["title"]?.Value<string>() ?? rootName);
      generatedCode = Models.EnsurePartialClasses(generatedCode);
      generatedCode = Models.EnsureRootJsonObjectTitle(generatedCode, parsedRoot["title"]?.Value<string>() ?? rootName);

      var rootFileName = $"{rootName}.g.cs";
      var schemaFileName = $"{rootName}.schema.json";

      var outputCodePath = Path.Combine(outputDir, rootFileName);
      var outputSchemaPath = Path.Combine(outputDir, schemaFileName);

      await File.WriteAllTextAsync(
        outputCodePath,
        generatedCode,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
      );

      await File.WriteAllTextAsync(
        outputSchemaPath,
        normalizedSchemaJson,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
      );

      Console.WriteLine($"Generated: {outputCodePath}");
      Console.WriteLine($"Saved schema: {outputSchemaPath}");
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine("Failed to generate models.");
      Console.Error.WriteLine(ex.ToString());
      Environment.ExitCode = 1;
    }
  }
}