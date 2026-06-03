# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

A Blazor WebAssembly + ASP.NET Core 8 app that simulates a virtual SLT (Sort & Load Test) device. Users log in, compose a sequence of test steps in a drag-and-drop executor, run them, and see step-by-step output. All test runs are persisted to SQLite.

## Commands

```bash
# Run both API and client with one command (recommended)
./run.sh

# Run only the API (http on port 5295, swagger at /swagger)
cd src/SltVirtualTest.Api && dotnet run --launch-profile http

# Run only the Blazor client
cd src/SltVirtualTest.Client && dotnet run

# Build the whole solution
dotnet build SltVirtualTest.sln

# Restore NuGet packages
dotnet restore SltVirtualTest.sln
```

No test projects exist yet.

## Architecture

Three projects sharing one solution:

**`SltVirtualTest.Shared`** — referenced by both Api and Client.
- `Models/TestModule.cs`, `Models/TestMethod.cs` — enums for the step catalog.
- `Dtos/TestDtos.cs` — all request/response records plus `StepCatalog.All`, the single source of truth for every available step and its metadata (which inputs it requires, whether it implies power-on).
- `Dtos/AuthDtos.cs` — login/signup request and response records.

**`SltVirtualTest.Api`** — ASP.NET Core minimal host.
- `Program.cs` — wires DI, CORS (hardcoded localhost origins), EF Core SQLite, Swagger, and runs `EnsureCreated` on startup. No EF migrations; schema is recreated from the model.
- `Data/AppDbContext.cs` — three tables: `Users`, `TestRuns`, `TestRunSteps`.
- `Services/AuthService.cs` — plain-text passwords (intentional for this prototype).
- `Services/TestExecutorService.cs` — stateless per-request execution. Walks steps in order, dispatches via `(Module, Method)` tuple switch, mutates a `TestInstanceState` object, stops on first failure. Persists the run and each step to the DB.
- `Services/TestInstanceState.cs` — mutable bag of simulated device state (power, USB/handler connections, handler XYZ position and rotation).
- `Controllers/AuthController.cs` — `POST /api/auth/signup`, `POST /api/auth/login`.
- `Controllers/TestRunsController.cs` — `POST /api/testruns/execute`.

**`SltVirtualTest.Client`** — Blazor WASM.
- `Services/UserSession.cs` — in-memory auth session (no persistence across page refreshes).
- `Services/ApiClient.cs` — thin typed wrapper over `HttpClient`; base URL comes from `wwwroot/appsettings.json` (`ApiBaseUrl`, default `http://localhost:5295`).
- `Models/ExecutorStepItem.cs` — client-side mutable step with string input fields for binding.
- `Validation/StepValidation.cs` — validates steps before the Run button is enabled.
- `Pages/Dashboard.razor` — the main UI. Left pane = step palette (from `StepCatalog.All`), center = executor drop zone (drag-and-drop via `sltExecutorDrag` JS interop), right = output log.

## Key data-flow

1. Client `Dashboard.razor` calls `Api.ExecuteTestAsync(new ExecuteTestRequest(userId, steps))`.
2. `TestRunsController` delegates to `TestExecutorService.ExecuteAsync`.
3. The service creates a `TestRunEntity`, iterates steps, calls the appropriate private method, records each `TestRunStepEntity`, stops on failure, persists everything, returns `ExecuteTestResponse` with a log and optional failure popup message.

## SQLite DB location

On macOS: `~/Library/Application Support/SltVirtualTest/sltvirtualtest.db`. Override with `ConnectionStrings:DefaultConnection` in `appsettings.json`.