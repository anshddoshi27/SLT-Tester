using System.Net.Http.Json;
using SltVirtualTest.Shared.Dtos;

namespace SltVirtualTest.Client.Services;

public class ApiClient(HttpClient http)
{
    public async Task<AuthResponse> SignUpAsync(SignUpRequest request) =>
        await PostAsync<SignUpRequest, AuthResponse>("api/auth/signup", request);

    public async Task<AuthResponse> LoginAsync(LoginRequest request) =>
        await PostAsync<LoginRequest, AuthResponse>("api/auth/login", request);

    public async Task<ExecuteTestResponse> ExecuteTestAsync(ExecuteTestRequest request) =>
        await PostAsync<ExecuteTestRequest, ExecuteTestResponse>("api/testruns/execute", request);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await http.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}
