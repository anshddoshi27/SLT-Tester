# SLT Virtual Test

Blazor WebAssembly frontend with ASP.NET Core API and SQLite for virtual SLT device testing.

## Projects

- `SltVirtualTest.Client` — Blazor WASM UI (landing, login, signup, dashboard)
- `SltVirtualTest.Api` — REST API, test executor, EF Core SQLite
- `SltVirtualTest.Shared` — DTOs and step catalog

## Run locally (one command)

From the project root:

```bash
./run.sh
```

This starts the API in the background, waits until it is ready, then starts the Blazor client. Press **Ctrl+C** once to stop both.

Open the client URL from the console (typically `https://localhost:7107` or `http://localhost:5215`).

### Run API and client separately (optional)

Terminal 1 — API:

```bash
cd src/SltVirtualTest.Api
dotnet run --launch-profile http
```

Terminal 2 — Client:

```bash
cd src/SltVirtualTest.Client
dotnet run
```

The client uses `http://localhost:5295` as the API URL (`wwwroot/appsettings.json`).

## Features

- User sign-up / login (plain-text passwords for now)
- Drag-or-click test steps into the executor
- Per-step inputs: temperature (20–100 °C), millimeters, rotation degrees
- Run executes steps sequentially; stops on first failure
- Separate USB vs handler connection state
- Test runs persisted to SQLite (`~/Library/Application Support/SltVirtualTest/sltvirtualtest.db` on macOS)
