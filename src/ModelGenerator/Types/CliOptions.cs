namespace ModelGenerator.Types;

public sealed class CliOptions
{
  public bool ShowHelp { get; set; }
  public string? SchemaJson { get; set; }
  public string? SchemaFile { get; set; }
  public string? OutputDir { get; set; }
  public string? Namespace { get; set; }
  public string? RootClassName { get; set; }
}