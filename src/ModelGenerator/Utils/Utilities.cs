using System.Diagnostics.CodeAnalysis;
using ModelGenerator.Types;

namespace ModelGenerator.Utils;

public static class Utilities
{
  [ExcludeFromCodeCoverage(Justification = "Not worth unit testing since it only prints to stdout a static message.")]
  public static void PrintUsage()
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

  public static CliOptions ParseArgs(string[] args)
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

  private static string ReadNextValue(string[] args, ref int index, string argName)
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
}