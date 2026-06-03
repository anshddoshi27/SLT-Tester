namespace SltVirtualTest.Api.Data.Entities;

public class TestRunStepEntity
{
    public Guid Id { get; set; }
    public Guid TestRunId { get; set; }
    public TestRunEntity TestRun { get; set; } = null!;
    public int StepOrder { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
}
