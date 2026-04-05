namespace ModelGenerator.Types;

public struct GeneratedOutput
{
  public required string GeneratedCode { get; set; }
  public required string RootClassName { get; set; }
  public required string NormalizedSchema { get; set; }
}