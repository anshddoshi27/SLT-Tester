using Microsoft.AspNetCore.Mvc;
using SltVirtualTest.Api.Services;
using SltVirtualTest.Shared.Dtos;

namespace SltVirtualTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("signup")]
    public Task<AuthResponse> SignUp([FromBody] SignUpRequest request, CancellationToken ct) =>
        authService.SignUpAsync(request, ct);

    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        authService.LoginAsync(request, ct);
}
