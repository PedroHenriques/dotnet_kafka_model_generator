using System.Text;
using ModelGenerator.Types;
using System.Diagnostics.CodeAnalysis;
using ModelGenerator.Utils;
using ModelGenerator.Core;

[ExcludeFromCodeCoverage(Justification = "Not unit testable due to handling file IO operations. Will be tested with integration tests.")]
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

    try
    {
      Validations.ValidateArgs(options);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex.Message);
      Console.Error.WriteLine();
      Utilities.PrintUsage();
      Environment.ExitCode = 1;
      return;
    }

    string schema;
    if (string.IsNullOrWhiteSpace(options.SchemaFile) == false)
    {
      if (File.Exists(options.SchemaFile!) == false)
      {
        Console.Error.WriteLine($"Schema file not found: {options.SchemaFile}");
        Environment.ExitCode = 1;
        return;
      }

      schema = await File.ReadAllTextAsync(options.SchemaFile!);
    }
    else
    {
      schema = options.SchemaContent!;
    }

    if (string.IsNullOrWhiteSpace(schema))
    {
      Console.Error.WriteLine("Schema content is empty.");
      Environment.ExitCode = 1;
      return;
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
      var generatorOutput = await ClassGenerator.Generate(
        options.SchemaType, schema, targetNamespace, explicitRootClassName
      );

      var rootFileName = $"{generatorOutput.RootClassName}.g.cs";
      var schemaFileName = $"{generatorOutput.RootClassName}.schema.json";

      var outputCodePath = Path.Combine(outputDir, rootFileName);
      var outputSchemaPath = Path.Combine(outputDir, schemaFileName);

      await File.WriteAllTextAsync(
        outputCodePath,
        generatorOutput.GeneratedCode,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
      );

      await File.WriteAllTextAsync(
        outputSchemaPath,
        generatorOutput.NormalizedSchema,
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