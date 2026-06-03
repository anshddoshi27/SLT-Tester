using Microsoft.EntityFrameworkCore;
using SltVirtualTest.Api.Data;
using SltVirtualTest.Api.Data.Entities;
using SltVirtualTest.Shared.Dtos;

namespace SltVirtualTest.Api.Services;

public class AuthService(AppDbContext db)
{
    public async Task<AuthResponse> SignUpAsync(SignUpRequest request, CancellationToken ct = default)
    {
        if (!IsNonEmpty(request.Username, request.Password, request.JobId, request.Employer, request.JobTitle))
            return new AuthResponse(false, "All fields are required.", null);

        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return new AuthResponse(false, "Username already exists.", null);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Password = request.Password,
            JobId = request.JobId.Trim(),
            Employer = request.Employer.Trim(),
            JobTitle = request.JobTitle.Trim()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(true, null, ToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (!IsNonEmpty(request.Username, request.Password))
            return new AuthResponse(false, "Username and password are required.", null);

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == request.Username.Trim() && u.Password == request.Password, ct);

        if (user is null)
            return new AuthResponse(false, "Invalid username or password.", null);

        return new AuthResponse(true, null, ToDto(user));
    }

    private static bool IsNonEmpty(params string?[] values) =>
        values.All(v => !string.IsNullOrWhiteSpace(v));

    private static UserDto ToDto(UserEntity u) =>
        new(u.Id, u.Username, u.JobId, u.Employer, u.JobTitle);
}
