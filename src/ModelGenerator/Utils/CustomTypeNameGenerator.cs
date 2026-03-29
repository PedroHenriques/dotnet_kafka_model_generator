using NJsonSchema;

namespace ModelGenerator.Utils;

public class CustomTypeNameGenerator : ITypeNameGenerator
{
  private readonly string _rootName;

  public CustomTypeNameGenerator(string rootName)
  {
    _rootName = rootName;
  }

  public string Generate(
    JsonSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames
  )
  {
    if (schema == schema.ActualSchema && string.IsNullOrWhiteSpace(typeNameHint))
    {
      return _rootName;
    }

    return typeNameHint ?? _rootName;
  }
}