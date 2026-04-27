using ModelGenerator.Types;
using ModelGenerator.Utils;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace ModelGenerator.Core;

public static class ClassGenerator
{
  public static async Task<GeneratedOutput> Generate(
    SchemaTypes? schemaType, string schema, string targetNamespace,
    string? explicitRootClassName
  )
  {
    switch (schemaType)
    {
      case SchemaTypes.JSON:
        return await GenerateJson(schema, targetNamespace, explicitRootClassName);

      case SchemaTypes.AVRO:
        return await GenerateAvro(schema, targetNamespace, explicitRootClassName);

      default:
        throw new Exception($"The provided schema type '{schemaType}' is not supported.");
    }
  }

  private static async Task<GeneratedOutput> GenerateJson(
    string schema, string targetNamespace, string? explicitRootClassName
  )
  {
    var parsedRoot = JObject.Parse(schema);

    var originalTitle = parsedRoot["title"]?.Value<string>();
    var rootClassName = string.IsNullOrWhiteSpace(explicitRootClassName) == false
      ? explicitRootClassName!
      : string.IsNullOrWhiteSpace(originalTitle) == false
        ? Models.ToPascalCaseIdentifier(originalTitle!)
        : "KafkaMessage";

    if (string.IsNullOrWhiteSpace(parsedRoot["title"]?.Value<string>()))
    {
      parsedRoot["title"] = rootClassName;
    }

    var normalizedSchema = parsedRoot.ToString();
    var schemaJson = await JsonSchema.FromJsonAsync(normalizedSchema);

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
      TypeNameGenerator = new CustomTypeNameGenerator(rootClassName),
      PropertyNameGenerator = new CSharpPropertyNameGenerator(),
    };

    var generator = new CSharpGenerator(schemaJson, settings);
    var generatedCode = generator.GenerateFile();

    generatedCode = Models.AddHeader(generatedCode, SchemaTypes.JSON, parsedRoot["title"]?.Value<string>() ?? rootClassName);
    generatedCode = Models.EnsurePartialClasses(generatedCode);
    generatedCode = Models.EnsureRootJsonObjectTitle(generatedCode, parsedRoot["title"]?.Value<string>() ?? rootClassName);

    return new GeneratedOutput
    {
      GeneratedCode = generatedCode,
      RootClassName = rootClassName,
      NormalizedSchema = normalizedSchema,
    };
  }
}