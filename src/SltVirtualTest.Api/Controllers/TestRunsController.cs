using Microsoft.AspNetCore.Mvc;
using SltVirtualTest.Api.Services;
using SltVirtualTest.Shared.Dtos;

namespace SltVirtualTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestRunsController(TestExecutorService executor) : ControllerBase
{
    [HttpPost("execute")]
    public Task<ExecuteTestResponse> Execute([FromBody] ExecuteTestRequest request, CancellationToken ct) =>
        executor.ExecuteAsync(request, ct);
}
