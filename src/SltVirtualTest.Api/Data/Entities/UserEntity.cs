namespace SltVirtualTest.Api.Data.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Employer { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
}
