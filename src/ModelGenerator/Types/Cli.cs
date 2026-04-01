using System.Diagnostics.CodeAnalysis;

namespace ModelGenerator.Types;

public sealed class CliOptions
{
  public bool ShowHelp { get; set; }
  public string? SchemaContent { get; set; }
  public string? SchemaFile { get; set; }
  public SchemaTypes? SchemaType { get; set; }
  public string? OutputDir { get; set; }
  public string? Namespace { get; set; }
  public string? RootClassName { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Not unit testable due to being a config static class.")]
public static class CliConfigs
{
  public static Dictionary<string, SchemaTypes> SchemaTypeMap = new Dictionary<string, SchemaTypes>
  {
    { "json", SchemaTypes.JSON },
  };
}