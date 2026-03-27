using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Newtonsoft.Json.Linq;

CliOptions options;
try
{
  options = ParseArgs(args);
}
catch (ArgumentException ex)
{
  Console.Error.WriteLine(ex.Message);
  Console.Error.WriteLine();
  PrintUsage();
  Environment.ExitCode = 1;
  return;
}

if (options.ShowHelp)
{
  PrintUsage();
  return;
}

if (string.IsNullOrWhiteSpace(options.OutputDir))
{
  Console.Error.WriteLine("Missing required argument: --output-dir");
  Console.Error.WriteLine();
  PrintUsage();
  Environment.ExitCode = 1;
  return;
}

var hasSchemaJson = string.IsNullOrWhiteSpace(options.SchemaJson) == false;
var hasSchemaFile = string.IsNullOrWhiteSpace(options.SchemaFile) == false;

if (hasSchemaJson == false && hasSchemaFile == false)
{
  Console.Error.WriteLine("You must provide either --schema-json or --schema-file.");
  Console.Error.WriteLine();
  PrintUsage();
  Environment.ExitCode = 1;
  return;
}

if (hasSchemaJson && hasSchemaFile)
{
  Console.Error.WriteLine("Arguments --schema-json and --schema-file cannot be used together.");
  Console.Error.WriteLine();
  PrintUsage();
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
      ? ToPascalCaseIdentifier(originalTitle!)
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

  generatedCode = AddHeader(generatedCode, parsedRoot["title"]?.Value<string>() ?? rootName);
  generatedCode = EnsurePartialClasses(generatedCode);
  generatedCode = EnsureRootJsonObjectTitle(generatedCode, parsedRoot["title"]?.Value<string>() ?? rootName);

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

static void PrintUsage()
{
  Console.WriteLine("""
Usage:
  kafka-model-generator --schema-json "<schema-json>" --output-dir "<output-directory>" [--namespace "<namespace>"] [--root-class-name "<root-class-name>"]
  kafka-model-generator --schema-file "<schema-file-path>" --output-dir "<output-directory>" [--namespace "<namespace>"] [--root-class-name "<root-class-name>"]

Examples:
  kafka-model-generator --schema-json "{ \"title\": \"OrderCreated\", \"type\": \"object\", \"properties\": { \"id\": { \"type\": \"string\" } }, \"required\": [\"id\"] }" --output-dir "./Generated"

  kafka-model-generator --schema-file "./schemas/order-created.json" --output-dir "./Generated" --namespace "MyCompany.Kafka.Models"

Notes:
  --schema-json       Full JSON schema as a string
  --schema-file       Path to a file containing the JSON schema
  --output-dir        Directory where generated files will be written
  --namespace         Optional. Default: Generated.Kafka.Models
  --root-class-name   Optional. Overrides the generated root class name
  --help              Show help
""");
}

static CliOptions ParseArgs(string[] args)
{
  var options = new CliOptions();

  for (int i = 0; i < args.Length; i++)
  {
    var arg = args[i];

    switch (arg)
    {
      case "--help":
      case "-h":
        options.ShowHelp = true;
        break;

      case "--schema-json":
        options.SchemaJson = ReadNextValue(args, ref i, arg);
        break;

      case "--schema-file":
        options.SchemaFile = ReadNextValue(args, ref i, arg);
        break;

      case "--output-dir":
        options.OutputDir = ReadNextValue(args, ref i, arg);
        break;

      case "--namespace":
        options.Namespace = ReadNextValue(args, ref i, arg);
        break;

      case "--root-class-name":
        options.RootClassName = ReadNextValue(args, ref i, arg);
        break;

      default:
        throw new ArgumentException($"Unknown argument: {arg}");
    }
  }

  return options;
}

static string ReadNextValue(string[] args, ref int index, string argName)
{
  if (index + 1 >= args.Length)
  {
    throw new ArgumentException($"Missing value for argument: {argName}");
  }

  index++;
  var value = args[index];

  if (string.IsNullOrWhiteSpace(value) || value.StartsWith("-"))
  {
    throw new ArgumentException($"Missing value for argument: {argName}");
  }

  return value;
}

static string AddHeader(string code, string schemaTitle)
{
  var header = $$"""
// ------------------------------------------------------------------------------
// <auto-generated>
//     Generated from Kafka JSON Schema.
//     Root schema title: {{schemaTitle}}
//     This file is intended for use with Confluent.SchemaRegistry.Serdes.Json.
// </auto-generated>
// ------------------------------------------------------------------------------

""";

  return header + code;
}

static string EnsureRootJsonObjectTitle(string code, string schemaTitle)
{
  var pattern = @"(?m)^(\s*)(public\s+(?:partial\s+)?class\s+\w+)";
  var replacement = $"$1[Newtonsoft.Json.JsonObject(Title = \"{EscapeCSharpString(schemaTitle)}\")]{Environment.NewLine}$1$2";

  var regex = new Regex(pattern);
  return regex.Replace(code, replacement, 1);
}

static string EnsurePartialClasses(string code)
{
  return Regex.Replace(
    code,
    @"(?m)^(\s*public\s+)(class\s+)",
    "$1partial $2");
}

static string ToPascalCaseIdentifier(string value)
{
  if (string.IsNullOrWhiteSpace(value))
  {
    return "KafkaMessage";
  }

  var parts = Regex.Split(value, @"[^A-Za-z0-9_]+")
    .Where(x => string.IsNullOrWhiteSpace(x) == false)
    .Select(x =>
    {
      var cleaned = Regex.Replace(x, @"[^A-Za-z0-9_]", "");
      if (string.IsNullOrWhiteSpace(cleaned))
      {
        return string.Empty;
      }

      if (cleaned.Length == 1)
      {
        return cleaned.ToUpperInvariant();
      }

      return char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
    })
    .Where(x => string.IsNullOrWhiteSpace(x) == false)
    .ToArray();

  var result = string.Concat(parts);
  if (string.IsNullOrWhiteSpace(result))
  {
    result = "KafkaMessage";
  }

  if (char.IsDigit(result[0]))
  {
    result = "_" + result;
  }

  return result;
}

static string EscapeCSharpString(string value)
{
  return value
    .Replace("\\", "\\\\")
    .Replace("\"", "\\\"");
}

sealed class CliOptions
{
  public bool ShowHelp { get; set; }
  public string? SchemaJson { get; set; }
  public string? SchemaFile { get; set; }
  public string? OutputDir { get; set; }
  public string? Namespace { get; set; }
  public string? RootClassName { get; set; }
}

class CustomTypeNameGenerator : ITypeNameGenerator
{
  private readonly string _rootName;

  public CustomTypeNameGenerator(string rootName)
  {
    _rootName = rootName;
  }

  public string Generate(JsonSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames)
  {
    if (schema == schema.ActualSchema && string.IsNullOrWhiteSpace(typeNameHint))
    {
      return _rootName;
    }

    return typeNameHint ?? _rootName;
  }
}