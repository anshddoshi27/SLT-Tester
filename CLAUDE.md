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
- `Services/TestInstanceState.cs` — mutable bag of simulated device state (power, connections, handler/device XYZ, MLT/PSU state, active session timers).
- `Controllers/AuthController.cs` — `POST /api/auth/signup`, `POST /api/auth/login`.
- `Controllers/TestRunsController.cs` — `POST /api/testruns/execute`.

**`SltVirtualTest.Client`** — Blazor WASM.
- `Services/UserSession.cs` — in-memory auth session (no persistence across page refreshes).
- `Services/ApiClient.cs` — thin typed wrapper over `HttpClient`; base URL comes from `wwwroot/appsettings.json` (`ApiBaseUrl`, default `http://localhost:5295`).
- `Models/ExecutorStepItem.cs` — client-side mutable step with string input fields for binding.
- `Validation/StepValidation.cs` — input validation (temperature, mm, degrees, volts, amps). Controls Run button.
- `Validation/StepSequenceValidator.cs` — full state-machine simulation. Walks all steps in order, tracks every state variable, flags each step that would fail with its exact reason. Any flagged step disables Run and highlights the step in red with an inline error message.
- `Pages/Dashboard.razor` — the main UI. Left pane = collapsible step palette (from `StepCatalog.All`), center = executor drop zone (drag-and-drop via `sltExecutorDrag` JS interop), right = output log. Layout is viewport-locked (100vh) so each panel scrolls independently.

## Key data-flow

1. Client `Dashboard.razor` calls `Api.ExecuteTestAsync(new ExecuteTestRequest(userId, steps))`.
2. `TestRunsController` delegates to `TestExecutorService.ExecuteAsync`.
3. The service creates a `TestRunEntity`, iterates steps, calls the appropriate private method, records each `TestRunStepEntity`, stops on failure, persists everything, returns `ExecuteTestResponse` with a log and optional failure popup message.

## SQLite DB location

On macOS: `~/Library/Application Support/SltVirtualTest/sltvirtualtest.db`. Override with `ConnectionStrings:DefaultConnection` in `appsettings.json`.

---

## Simulation State Model

Every test run starts with a fresh `TestInstanceState` (server) and is mirrored by `StepSequenceValidator` (client). All coordinates are in millimetres.

```
TestInstanceState (initial values)
├── IsDevicePowered             = false   ← set by PowerSupply.PowerOn or Device.Connect (when PSU at 5V/3A)
├── IsHandlerConnected          = false   ← SLT physically connected to handler arm
│
├── HandlerX / HandlerY / HandlerZ        = 0, 0, 0
├── HandlerRotationDegrees      = 0
│
├── DeviceX / DeviceY / DeviceZ           = 300, 0, 0   ← DUT starts away from board
├── IsDevicePickedUp            = false
│
├── IsDeviceConnectedToBoard    = false   ← device seated on the physical board
├── IsDeviceConnectedToThermal  = false   ← thermal cell attached to device
├── LastValidatedTemperatureCelsius = null
│
├── SupplyVolts                 = null    ← set by PowerSupply.SupplyPower
├── SupplyCurrent               = null    ← set by PowerSupply.SupplyCurrent
├── IsPowerSupplyOn             = false   ← set by PowerSupply.PowerOn/Off
│
├── IsMltPowered                = false
├── IsMltConnected              = false
├── IsMltTransmissionStarted    = false
├── IsDeviceConnectedToNetwork  = false
│
├── IsSmsInFlight               = false   ── timer fields (server only)
├── SmsSentAt                   = null
├── IsVoiceCallActive           = false
├── VoiceCallStartedAt          = null
├── IsYoutubeSessionActive      = false
├── YoutubeStartedAt            = null
├── IsSpeedTestInFlight         = false
└── SpeedTestStartedAt          = null
```

**Fixed locations (constants, never change)**
```
Board / Thermal Cell  →  (100, 0, 0)
Device start position →  (300, 0, 0)
Handler start position → (0,   0, 0)
```

---

## Module Command Reference & Logic Tree

### POWER SUPPLY module
> The Power Supply is physically pre-connected to the board — no connect/disconnect. It must be powered on first, then voltage and current are dialled in. The device can only connect to the board when the supply is on at exactly 5 V and 3 A.

```
PowerSupply
│
├── Power On
│   ├── Description : Turns the power supply unit on. No voltage/current required.
│   ├── Preconditions : none
│   └── State change  : IsPowerSupplyOn = true
│
├── Power Off
│   ├── Description : Turns the power supply unit off. Cuts device power.
│   ├── Preconditions : none
│   └── State change  : IsPowerSupplyOn = false, IsDevicePowered = false
│
├── Supply Power   (requires Volts input, integer 1–10)
│   ├── Description : Sets the output voltage of the supply.
│   ├── Preconditions : IsPowerSupplyOn = true
│   │                   ❌ FAILS if supply is not on
│   │                   ❌ FAILS if voltage is missing, non-integer, or out of range
│   └── State change  : SupplyVolts = V
│
└── Supply Current  (requires Amps input, integer 1–10)
    ├── Description : Sets the output current of the supply.
    ├── Preconditions : IsPowerSupplyOn = true
    │                   ❌ FAILS if supply is not on
    │                   ❌ FAILS if current is missing, non-integer, or out of range
    └── State change  : SupplyCurrent = A
```

---

### HANDLER module
> The Handler is the robotic arm. The SLT connects to it (`Handler.Connect`) before it can do anything. It physically moves the DUT from its starting position to the board. Handler.Connect/Disconnect have no effect on device power.

```
Handler
│
├── Connect
│   ├── Description : SLT connects to the handler arm.
│   ├── Preconditions : none
│   └── State change  : IsHandlerConnected = true
│
├── Disconnect
│   ├── Description : SLT disconnects from handler arm.
│   ├── Preconditions : none
│   └── State change  : IsHandlerConnected = false
│
├── Move Right   (requires Millimeters input)
│   ├── Description : Moves handler arm in the +X direction N mm.
│   │                 If carrying the device, device coordinates update too.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   ❌ FAILS if handler not connected
│   └── State change  : HandlerX += mm
│                       if IsDevicePickedUp → DeviceX = HandlerX
│
├── Move Left    (requires Millimeters input)
│   ├── Description : Moves handler arm in the -X direction N mm.
│   ├── Preconditions : IsHandlerConnected = true
│   └── State change  : HandlerX -= mm
│                       if IsDevicePickedUp → DeviceX = HandlerX
│
├── Move Up      (requires Millimeters input)
│   ├── Description : Moves handler arm in the +Y direction N mm.
│   ├── Preconditions : IsHandlerConnected = true
│   └── State change  : HandlerY += mm
│                       if IsDevicePickedUp → DeviceY = HandlerY
│
├── Move Down    (requires Millimeters input)
│   ├── Description : Moves handler arm in the -Y direction N mm.
│   ├── Preconditions : IsHandlerConnected = true
│   └── State change  : HandlerY -= mm
│                       if IsDevicePickedUp → DeviceY = HandlerY
│
├── Rotation     (requires Degrees input, + = clockwise, − = counterclockwise)
│   ├── Description : Rotates the handler arm N degrees.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   ❌ FAILS if handler not connected
│   └── State change  : HandlerRotationDegrees += degrees
│
├── Move to Board
│   ├── Description : Shortcut — teleports handler directly to board location (100,0,0).
│   │                 If carrying the device, device coordinates update too.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   ❌ FAILS if handler not connected
│   └── State change  : HandlerX/Y/Z = (100,0,0)
│                       if IsDevicePickedUp → DeviceX/Y/Z = (100,0,0)
│
├── Move to Device
│   ├── Description : Shortcut — teleports handler to the device's current location.
│   │                 Use before Pick Up Device to avoid manual Move steps.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   ❌ FAILS if handler not connected
│   └── State change  : HandlerX/Y/Z = DeviceX/Y/Z
│
├── Move to Origin
│   ├── Description : Shortcut — teleports handler back to (0,0,0).
│   │                 If carrying the device, device coordinates update too.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   ❌ FAILS if handler not connected
│   └── State change  : HandlerX/Y/Z = (0,0,0)
│                       if IsDevicePickedUp → DeviceX/Y/Z = (0,0,0)
│
├── Pick Up Device
│   ├── Description : Handler grabs the DUT. After this, all Move commands
│   │                 also translate the device coordinates in lock-step.
│   ├── Preconditions : IsHandlerConnected = true
│   │                   (HandlerX, HandlerY, HandlerZ) == (DeviceX, DeviceY, DeviceZ)
│   │                   ❌ FAILS if handler not connected
│   │                   ❌ FAILS if handler not at device's current location
│   └── State change  : IsDevicePickedUp = true
│
└── Release Device
    ├── Description : Handler drops the DUT at its current position.
    │                 Device coordinates are set to current handler coordinates.
    ├── Preconditions : IsHandlerConnected = true
    │                   IsDevicePickedUp = true
    │                   ❌ FAILS if handler not connected
    │                   ❌ FAILS if no device is currently held
    └── State change  : DeviceX/Y/Z = HandlerX/Y/Z
                        IsDevicePickedUp = false
```

---

### DEVICE module
> The Device is the DUT (Device Under Test). Connecting it means seating it on the **board** (physical PCB socket). This now requires the Power Supply to be on at exactly 5 V and 3 A. The thermal cell and MLT network are separate connections on top.

```
Device
│
├── Connect  (Connect to Board)
│   ├── Description : Seats the DUT onto the board socket. Powers device on as side-effect.
│   │                 Requires the Power Supply to be on at 5 V and 3 A.
│   ├── Preconditions : IsDevicePickedUp = false
│   │                   (DeviceX, DeviceY, DeviceZ) == (100, 0, 0)
│   │                   IsPowerSupplyOn = true AND SupplyVolts == 5 AND SupplyCurrent == 3
│   │                   ❌ FAILS if handler is still holding the device
│   │                   ❌ FAILS if device is not at board location
│   │                   ❌ FAILS if power supply is not on at 5 V / 3 A
│   └── State change  : IsDeviceConnectedToBoard = true, IsDevicePowered = true
│
├── Disconnect  (Disconnect from Board)
│   ├── Description : Lifts the DUT off the board socket.
│   ├── Preconditions : IsDeviceConnectedToThermal = false  (thermal must be off first)
│   │                   IsDevicePickedUp = false
│   │                   (DeviceX, DeviceY, DeviceZ) == (100, 0, 0)
│   │                   ❌ FAILS if thermal is still connected
│   │                   ❌ FAILS if handler is holding the device
│   │                   ❌ FAILS if device is not at board location
│   └── State change  : IsDeviceConnectedToBoard = false
│
├── Read Temperature
│   ├── Description : Reads the currently validated temperature from the thermal cell.
│   ├── Preconditions : IsDeviceConnectedToBoard = true
│   │                   IsDeviceConnectedToThermal = true
│   │                   ❌ FAILS if device not connected to board
│   │                   ❌ FAILS if thermal cell not connected
│   └── Returns       : "Temperature: N °C."  or  "No temperature set."
│
├── Connect to Network
│   ├── Description : Connects the device to the MLT network.
│   ├── Preconditions : IsMltTransmissionStarted = true
│   │                   ❌ FAILS if MLT transmission not started
│   └── State change  : IsDeviceConnectedToNetwork = true
│
├── Send SMS
│   ├── Description : Sends an SMS over the network. Starts a timer.
│   │                 MLT.Receive SMS stops the timer and outputs elapsed ms.
│   ├── Preconditions : IsDeviceConnectedToNetwork = true
│   │                   IsSmsInFlight = false
│   │                   ❌ FAILS if not connected to network
│   │                   ❌ FAILS if an SMS is already in transit
│   └── State change  : IsSmsInFlight = true, SmsSentAt = now
│
├── Make Voice Call
│   ├── Description : Initiates a voice call over the network. Starts a timer.
│   │                 MLT.Receive Voice Call outputs elapsed ms (does not end the call).
│   ├── Preconditions : IsDeviceConnectedToNetwork = true
│   │                   IsVoiceCallActive = false
│   │                   ❌ FAILS if not connected to network
│   │                   ❌ FAILS if a call is already active
│   └── State change  : IsVoiceCallActive = true, VoiceCallStartedAt = now
│
├── End Voice Call
│   ├── Description : Hangs up the active voice call.
│   ├── Preconditions : IsVoiceCallActive = true
│   │                   ❌ FAILS if no call is active
│   └── State change  : IsVoiceCallActive = false
│
├── Go on YouTube
│   ├── Description : Opens a YouTube session over the network. Starts a timer.
│   │                 MLT.Receive YouTube Req outputs elapsed ms (does not end the session).
│   ├── Preconditions : IsDeviceConnectedToNetwork = true
│   │                   IsYoutubeSessionActive = false
│   │                   ❌ FAILS if not connected to network
│   │                   ❌ FAILS if a YouTube session is already active
│   └── State change  : IsYoutubeSessionActive = true, YoutubeStartedAt = now
│
├── Get Off YouTube
│   ├── Description : Closes the active YouTube session.
│   ├── Preconditions : IsYoutubeSessionActive = true
│   │                   ❌ FAILS if no YouTube session is active
│   └── State change  : IsYoutubeSessionActive = false
│
└── Start Speed Test
    ├── Description : Sends 8 sample bytes to the MLT. Starts a timer.
    │                 MLT.Stop Speed Test outputs elapsed ms and reports 8 bytes transferred.
    ├── Preconditions : IsDeviceConnectedToNetwork = true
    │                   IsSpeedTestInFlight = false
    │                   ❌ FAILS if not connected to network
    │                   ❌ FAILS if a speed test is already running
    └── State change  : IsSpeedTestInFlight = true, SpeedTestStartedAt = now
```

---

### THERMAL module
> The Thermal Cell is physically attached to the board. It wraps around the DUT once it is seated on the board and applies a controlled temperature for testing. Thermal.Connect/Disconnect have no effect on device power.

```
Thermal
│
├── Connect  (Connect Thermal Cell to Device)
│   ├── Description : Attaches the thermal cell to the DUT and sets/validates temperature.
│   │                 Requires a temperature value (20–100 °C).
│   ├── Preconditions : IsDeviceConnectedToBoard = true
│   │                   TemperatureCelsius input ∈ [20, 100] (integer)
│   │                   ❌ FAILS if device is not connected to board
│   │                   ❌ FAILS if temperature is missing, non-integer, or out of range
│   └── State change  : IsDeviceConnectedToThermal = true
│                       LastValidatedTemperatureCelsius = temp
│
└── Disconnect  (Disconnect Thermal Cell)
    ├── Description : Detaches the thermal cell from the DUT.
    ├── Preconditions : IsDeviceConnectedToThermal = true
    │                   (DeviceX, DeviceY, DeviceZ) == (100, 0, 0)
    │                   ❌ FAILS if thermal cell is not currently connected
    │                   ❌ FAILS if device is not at board location
    └── State change  : IsDeviceConnectedToThermal = false
```

---

### MLT module
> The MLT (Mobile-Link Test) box simulates a network-in-a-box for testing the device's radio functions (SMS, voice, data). It must be connected after the device is on the board, then transmission started before the device can join the network. Receive methods measure and output elapsed milliseconds from the matching Device send/start — they error if called without a preceding send.

```
MLT
│
├── Connect
│   ├── Description : Connects the MLT box to the test setup.
│   ├── Preconditions : IsDeviceConnectedToBoard = true
│   │                   ❌ FAILS if device is not on the board
│   └── State change  : IsMltConnected = true
│
├── Disconnect
│   ├── Description : Disconnects the MLT box.
│   ├── Preconditions : IsMltTransmissionStarted = false  (end transmission first)
│   │                   ❌ FAILS if transmission is still active
│   └── State change  : IsMltConnected = false
│
├── Power On
│   ├── Description : Powers on the MLT box.
│   ├── Preconditions : none
│   └── State change  : IsMltPowered = true
│
├── Power Off
│   ├── Description : Powers off the MLT box.
│   ├── Preconditions : none
│   └── State change  : IsMltPowered = false
│
├── Start Transmission
│   ├── Description : Brings up the MLT network, ready for the device to join.
│   ├── Preconditions : IsMltConnected = true
│   │                   IsDeviceConnectedToBoard = true
│   │                   ❌ FAILS if MLT not connected
│   │                   ❌ FAILS if device not on board
│   └── State change  : IsMltTransmissionStarted = true
│
├── End Transmission
│   ├── Description : Tears down the MLT network.
│   ├── Preconditions : IsMltTransmissionStarted = true
│   │                   ❌ FAILS if no active transmission
│   └── State change  : IsMltTransmissionStarted = false
│
├── Receive SMS
│   ├── Description : MLT receives the SMS sent by the device. Outputs elapsed ms.
│   ├── Preconditions : IsSmsInFlight = true  (Device.Send SMS must precede this)
│   │                   ❌ FAILS if no SMS is in transit
│   └── State change  : IsSmsInFlight = false
│                       Returns "SMS received in X ms."
│
├── Receive Voice Call
│   ├── Description : MLT receives the voice call. Outputs elapsed ms since Make Voice Call.
│   │                 Does NOT end the call — use Device.End Voice Call for that.
│   ├── Preconditions : IsVoiceCallActive = true
│   │                   ❌ FAILS if no active call
│   └── Returns       : "Voice call received in X ms."
│
├── Receive YouTube Req
│   ├── Description : MLT receives the YouTube request. Outputs elapsed ms since Go on YouTube.
│   │                 Does NOT end the session — use Device.Get Off YouTube for that.
│   ├── Preconditions : IsYoutubeSessionActive = true
│   │                   ❌ FAILS if no active YouTube session
│   └── Returns       : "YouTube request received in X ms."
│
└── Stop Speed Test
    ├── Description : MLT receives the 8 sample bytes. Outputs elapsed ms and byte count.
    ├── Preconditions : IsSpeedTestInFlight = true
    │                   ❌ FAILS if no speed test is in progress
    └── State change  : IsSpeedTestInFlight = false
                        Returns "Speed test complete: 8 bytes transferred in X ms."
```

---

## Full Happy-Path Sequence (canonical test run)

The steps below represent the minimum correct sequence to power up, pick up the DUT, place it on the board, run thermal and network tests, and clean up.

```
Step  Module        Command               Notes
────  ────────────  ──────────────────    ───────────────────────────────────────────────
 1    PowerSupply   Power On              IsPowerSupplyOn = true.
 2    PowerSupply   Supply Power [5V]     SupplyVolts = 5.
 3    PowerSupply   Supply Current [3A]   SupplyCurrent = 3.
 4    Handler       Connect               IsHandlerConnected = true.
 5    Handler       Move to Device        Handler teleports to (300,0,0).
 6    Handler       Pick Up Device        IsDevicePickedUp = true.
 7    Handler       Move to Board         Handler + device teleport to (100,0,0).
 8    Handler       Release Device        IsDevicePickedUp = false.
 9    Device        Connect               PSU on at 5V/3A → board seated, device powered on.
10    MLT           Connect               IsDeviceConnectedToBoard required.
11    MLT           Start Transmission    Network ready.
12    Device        Connect to Network    IsDeviceConnectedToNetwork = true.
13    Thermal       Connect [25°C]        Thermal cell attached.
14    Device        Read Temperature      Returns "Temperature: 25 °C."
15    Device        Send SMS              Timer starts.
16    MLT           Receive SMS           "SMS received in X ms."
17    Device        Make Voice Call       Timer starts.
18    MLT           Receive Voice Call    "Voice call received in X ms."
19    Device        End Voice Call        Call ended.
20    Device        Go on YouTube         Timer starts.
21    MLT           Receive YouTube Req   "YouTube request received in X ms."
22    Device        Get Off YouTube       Session ended.
23    Device        Start Speed Test      Timer starts, 8 bytes in transit.
24    MLT           Stop Speed Test       "Speed test complete: 8 bytes in X ms."
25    MLT           End Transmission      Network torn down.
26    MLT           Disconnect            MLT box removed.
27    Thermal       Disconnect            Thermal cell removed.
28    Device        Disconnect            DUT lifted off board.
29    PowerSupply   Power Off             Supply off, device powered off.
```

---

## Error Conditions Reference

All errors are caught **twice**: client-side (disables Run button + highlights step) and server-side (stops execution, returns popup message).

| Condition | Error message |
|---|---|
| SupplyPower/SupplyCurrent — PSU not on | `Power supply must be on before setting voltage/current.` |
| SupplyPower/SupplyCurrent — out of range | `Invalid voltage/current. Must be an integer from 1 to 10 V/A.` |
| Move/Rotate/MoveToX without handler connected | `Handler must be connected to move/rotate.` |
| Pick Up Device — handler not connected | `Handler must be connected to pick up device.` |
| Pick Up Device — handler not at device location | `Handler is at (x,y,z) but device is at (x,y,z). Move handler to device location first.` |
| Release Device — no device held | `No device is currently picked up by the handler.` |
| Device.Connect — device still held | `Device must be released by the handler before connecting to board.` |
| Device.Connect — device not at board | `Device is at (x,y,z) but must be at board location (100,0,0).` |
| Device.Connect — PSU not on at 5V/3A | `Power supply must be on at 5V and 3A before connecting device to board.` |
| Device.Disconnect — thermal still connected | `Thermal must be disconnected before disconnecting device from board.` |
| Device.Disconnect — device not at board | `Device must be at board location (100,0,0) to disconnect.` |
| Thermal.Connect — device not on board | `Device must be connected to board before connecting thermal cell.` |
| Thermal.Connect — bad temperature | `Invalid temperature. Must be an integer from 20 to 100 °C.` |
| Thermal.Disconnect — thermal not connected | `Thermal cell is not connected — nothing to disconnect.` |
| Thermal.Disconnect — device not at board | `Device must be at board location (100,0,0) to disconnect thermal.` |
| Read Temperature — device not on board | `Device must be connected to board before reading temperature.` |
| Read Temperature — thermal not connected | `Thermal cell must be connected before reading temperature.` |
| MLT.Connect — device not on board | `Device must be connected to board before connecting MLT box.` |
| MLT.Disconnect — transmission still active | `MLT transmission must be ended before disconnecting MLT box.` |
| MLT.StartTransmission — MLT not connected | `MLT box must be connected before starting transmission.` |
| MLT.StartTransmission — device not on board | `Device must be connected to board before starting MLT transmission.` |
| MLT.EndTransmission — no active transmission | `No active MLT transmission to end.` |
| Device.ConnectToNetwork — no transmission | `MLT transmission must be started before connecting device to network.` |
| Device.SendSms — not on network | `Device must be connected to network before sending SMS.` |
| Device.SendSms — SMS already in transit | `An SMS is already in transit. Wait for MLT to receive it first.` |
| Device.MakeVoiceCall — not on network | `Device must be connected to network before making a voice call.` |
| Device.MakeVoiceCall — call already active | `A voice call is already active.` |
| Device.EndVoiceCall — no active call | `No active voice call to end.` |
| Device.GoOnYoutube — not on network | `Device must be connected to network before going on YouTube.` |
| Device.GoOnYoutube — session already active | `A YouTube session is already active.` |
| Device.GetOffYoutube — no active session | `No active YouTube session to end.` |
| Device.StartSpeedTest — not on network | `Device must be connected to network before starting a speed test.` |
| Device.StartSpeedTest — test already running | `A speed test is already in progress.` |
| MLT.ReceiveSms — no SMS in transit | `No SMS in transit — Device.SendSms must be called first.` |
| MLT.ReceiveVoiceCall — no active call | `No active voice call — Device.MakeVoiceCall must be called first.` |
| MLT.ReceiveYoutubeRequest — no active session | `No active YouTube session — Device.GoOnYoutube must be called first.` |
| MLT.StopSpeedTest — no test running | `No speed test in progress — Device.StartSpeedTest must be called first.` |

---

## Client-Side Validation Files

- `Validation/StepValidation.cs` — input validation (temperature range, millimeter/degree fields, voltage 1–10 V, current 1–10 A). Controls Run button.
- `Validation/StepSequenceValidator.cs` — full state-machine simulation. Walks all steps in order, tracks every state variable, flags each step that would fail with its exact reason. Any flagged step disables Run and highlights the step in red with an inline error message.
