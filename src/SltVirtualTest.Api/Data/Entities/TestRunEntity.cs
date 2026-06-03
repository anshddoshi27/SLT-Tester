namespace SltVirtualTest.Api.Data.Entities;

public class TestRunEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? FailureMessage { get; set; }
    public ICollection<TestRunStepEntity> Steps { get; set; } = new List<TestRunStepEntity>();
}
