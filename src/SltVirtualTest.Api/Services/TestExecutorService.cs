using System.Text.Json;
using SltVirtualTest.Api.Data;
using SltVirtualTest.Api.Data.Entities;
using SltVirtualTest.Shared.Dtos;
using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Api.Services;

public class TestExecutorService(AppDbContext db)
{
    public async Task<ExecuteTestResponse> ExecuteAsync(ExecuteTestRequest request, CancellationToken ct = default)
    {
        var testRun = new TestRunEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            StartedAt = DateTime.UtcNow,
            Success = false
        };

        db.TestRuns.Add(testRun);

        var state = new TestInstanceState();
        var logEntries = new List<LogEntryDto>();
        string? failurePopup = null;
        var failed = false;

        for (var i = 0; i < request.Steps.Count; i++)
        {
            var step = request.Steps[i];
            var label = $"{step.Module}.{step.Method}";
            var (success, message, popup) = ExecuteStep(step, state);

            var stepEntity = new TestRunStepEntity
            {
                Id = Guid.NewGuid(),
                TestRunId = testRun.Id,
                StepOrder = i + 1,
                Module = step.Module.ToString(),
                Method = step.Method.ToString(),
                ParametersJson = SerializeParameters(step),
                Success = success,
                Message = message,
                ExecutedAt = DateTime.UtcNow
            };
            db.TestRunSteps.Add(stepEntity);

            logEntries.Add(new LogEntryDto(i + 1, label, success, message, stepEntity.ExecutedAt));

            if (!success)
            {
                failed = true;
                failurePopup = popup ?? message;
                testRun.FailureMessage = failurePopup;
                break;
            }
        }

        testRun.CompletedAt = DateTime.UtcNow;
        testRun.Success = !failed && request.Steps.Count > 0;
        if (request.Steps.Count == 0)
        {
            testRun.Success = false;
            testRun.FailureMessage = "No steps to execute.";
        }

        await db.SaveChangesAsync(ct);

        return new ExecuteTestResponse(
            testRun.Id,
            testRun.Success,
            failurePopup,
            logEntries);
    }

    private static (bool Success, string Message, string? Popup) ExecuteStep(ExecutorStepDto step, TestInstanceState state)
    {
        return (step.Module, step.Method) switch
        {
            (TestModule.Thermal, TestMethod.Connect)    => ThermalConnect(step, state),
            (TestModule.Thermal, TestMethod.Disconnect) => ThermalDisconnect(state),

            (TestModule.Handler, TestMethod.Connect)       => HandlerConnect(state),
            (TestModule.Handler, TestMethod.Disconnect)    => HandlerDisconnect(state),
            (TestModule.Handler, TestMethod.MoveRight)     => Move(state, step.Millimeters ?? 0, dx: 1),
            (TestModule.Handler, TestMethod.MoveLeft)      => Move(state, step.Millimeters ?? 0, dx: -1),
            (TestModule.Handler, TestMethod.MoveUp)        => Move(state, step.Millimeters ?? 0, dy: 1),
            (TestModule.Handler, TestMethod.MoveDown)      => Move(state, step.Millimeters ?? 0, dy: -1),
            (TestModule.Handler, TestMethod.Rotation)      => Rotate(state, step.RotationDegrees ?? 0),
            (TestModule.Handler, TestMethod.PickUpDevice)  => PickUpDevice(state),
            (TestModule.Handler, TestMethod.ReleaseDevice) => ReleaseDevice(state),
            (TestModule.Handler, TestMethod.MoveToBoard)   => MoveToBoard(state),
            (TestModule.Handler, TestMethod.MoveToDevice)  => MoveToDevice(state),
            (TestModule.Handler, TestMethod.MoveToOrigin)  => MoveToOrigin(state),

            (TestModule.Device, TestMethod.Connect)          => DeviceConnect(state),
            (TestModule.Device, TestMethod.Disconnect)        => DeviceDisconnect(state),
            (TestModule.Device, TestMethod.ReadTemperature)   => ReadTemperature(state),
            (TestModule.Device, TestMethod.ConnectToNetwork)  => DeviceConnectToNetwork(state),
            (TestModule.Device, TestMethod.SendSms)           => DeviceSendSms(state),
            (TestModule.Device, TestMethod.MakeVoiceCall)     => DeviceMakeVoiceCall(state),
            (TestModule.Device, TestMethod.EndVoiceCall)      => DeviceEndVoiceCall(state),
            (TestModule.Device, TestMethod.GoOnYoutube)       => DeviceGoOnYoutube(state),
            (TestModule.Device, TestMethod.GetOffYoutube)     => DeviceGetOffYoutube(state),
            (TestModule.Device, TestMethod.StartSpeedTest)    => DeviceStartSpeedTest(state),

            (TestModule.Mlt, TestMethod.Connect)               => MltConnect(state),
            (TestModule.Mlt, TestMethod.Disconnect)            => MltDisconnect(state),
            (TestModule.Mlt, TestMethod.PowerOn)               => MltPowerOn(state),
            (TestModule.Mlt, TestMethod.PowerOff)              => MltPowerOff(state),
            (TestModule.Mlt, TestMethod.StartTransmission)     => MltStartTransmission(state),
            (TestModule.Mlt, TestMethod.EndTransmission)       => MltEndTransmission(state),
            (TestModule.Mlt, TestMethod.ReceiveSms)            => MltReceiveSms(state),
            (TestModule.Mlt, TestMethod.ReceiveVoiceCall)      => MltReceiveVoiceCall(state),
            (TestModule.Mlt, TestMethod.ReceiveYoutubeRequest) => MltReceiveYoutubeRequest(state),
            (TestModule.Mlt, TestMethod.StopSpeedTest)         => MltStopSpeedTest(state),

            (TestModule.PowerSupply, TestMethod.PowerOn)       => PowerSupplyPowerOn(state),
            (TestModule.PowerSupply, TestMethod.PowerOff)      => PowerSupplyPowerOff(state),
            (TestModule.PowerSupply, TestMethod.SupplyPower)   => SetSupplyPower(step, state),
            (TestModule.PowerSupply, TestMethod.SupplyCurrent) => SetSupplyCurrent(step, state),

            _ => (false, $"Unknown step: {step.Module}.{step.Method}", null)
        };
    }

    // ── Handler ──────────────────────────────────────────────────────────────

    private static (bool, string, string?) HandlerConnect(TestInstanceState state)
    {
        state.IsHandlerConnected = true;
        return (true, "SLT connected to handler.", null);
    }

    private static (bool, string, string?) HandlerDisconnect(TestInstanceState state)
    {
        state.IsHandlerConnected = false;
        return (true, "SLT disconnected from handler.", null);
    }

    private static (bool, string, string?) PickUpDevice(TestInstanceState state)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected (SLT connected to handler) to pick up device.", "Handler not connected.");
        if (state.HandlerX != state.DeviceX || state.HandlerY != state.DeviceY || state.HandlerZ != state.DeviceZ)
            return (false,
                $"Handler is at ({state.HandlerX},{state.HandlerY},{state.HandlerZ}) but device is at ({state.DeviceX},{state.DeviceY},{state.DeviceZ}). Handler must be at device location to pick it up.",
                "Handler not at device location.");
        state.IsDevicePickedUp = true;
        return (true, $"Device picked up at ({state.DeviceX},{state.DeviceY},{state.DeviceZ}).", null);
    }

    private static (bool, string, string?) ReleaseDevice(TestInstanceState state)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to release device.", "Handler not connected.");
        if (!state.IsDevicePickedUp)
            return (false, "No device is currently being held by the handler.", "No device picked up.");
        state.DeviceX = state.HandlerX;
        state.DeviceY = state.HandlerY;
        state.DeviceZ = state.HandlerZ;
        state.IsDevicePickedUp = false;
        return (true, $"Device released at ({state.DeviceX},{state.DeviceY},{state.DeviceZ}).", null);
    }

    private static (bool, string, string?) Move(TestInstanceState state, int mm, int dx = 0, int dy = 0)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to move.", "Handler not connected.");
        state.HandlerX += dx * mm;
        state.HandlerY += dy * mm;
        if (state.IsDevicePickedUp)
        {
            state.DeviceX = state.HandlerX;
            state.DeviceY = state.HandlerY;
            state.DeviceZ = state.HandlerZ;
        }
        var direction = dx switch
        {
            1 => "right",
            -1 => "left",
            _ => dy switch { 1 => "up", -1 => "down", _ => "unknown" }
        };
        var deviceInfo = state.IsDevicePickedUp ? $" Device: ({state.DeviceX},{state.DeviceY},{state.DeviceZ})." : "";
        return (true, $"Moved {direction} {Math.Abs(mm)} mm. Handler: ({state.HandlerX},{state.HandlerY},{state.HandlerZ}).{deviceInfo}", null);
    }

    private static (bool, string, string?) Rotate(TestInstanceState state, int degrees)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to rotate.", "Handler not connected.");
        state.HandlerRotationDegrees += degrees;
        var direction = degrees >= 0 ? "clockwise (right)" : "counterclockwise (left)";
        return (true, $"Rotated {Math.Abs(degrees)}° {direction}. Total rotation: {state.HandlerRotationDegrees}°.", null);
    }

    private static (bool, string, string?) MoveToBoard(TestInstanceState state)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to move.", "Handler not connected.");
        state.HandlerX = TestInstanceState.BoardX;
        state.HandlerY = TestInstanceState.BoardY;
        state.HandlerZ = TestInstanceState.BoardZ;
        if (state.IsDevicePickedUp)
        {
            state.DeviceX = state.HandlerX;
            state.DeviceY = state.HandlerY;
            state.DeviceZ = state.HandlerZ;
        }
        var deviceInfo = state.IsDevicePickedUp ? $" Device: ({state.DeviceX},{state.DeviceY},{state.DeviceZ})." : "";
        return (true, $"Handler moved to board location ({TestInstanceState.BoardX},{TestInstanceState.BoardY},{TestInstanceState.BoardZ}).{deviceInfo}", null);
    }

    private static (bool, string, string?) MoveToDevice(TestInstanceState state)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to move.", "Handler not connected.");
        state.HandlerX = state.DeviceX;
        state.HandlerY = state.DeviceY;
        state.HandlerZ = state.DeviceZ;
        return (true, $"Handler moved to device location ({state.DeviceX},{state.DeviceY},{state.DeviceZ}).", null);
    }

    private static (bool, string, string?) MoveToOrigin(TestInstanceState state)
    {
        if (!state.IsHandlerConnected)
            return (false, "Handler must be connected to move.", "Handler not connected.");
        state.HandlerX = 0;
        state.HandlerY = 0;
        state.HandlerZ = 0;
        if (state.IsDevicePickedUp)
        {
            state.DeviceX = 0;
            state.DeviceY = 0;
            state.DeviceZ = 0;
        }
        var deviceInfo = state.IsDevicePickedUp ? " Device: (0,0,0)." : "";
        return (true, $"Handler moved to origin (0,0,0).{deviceInfo}", null);
    }

    // ── Device ───────────────────────────────────────────────────────────────

    private static (bool, string, string?) DeviceConnect(TestInstanceState state)
    {
        if (state.IsDevicePickedUp)
            return (false, "Device must be released by the handler before connecting to board.", "Release device before connecting.");
        if (state.DeviceX != TestInstanceState.BoardX || state.DeviceY != TestInstanceState.BoardY || state.DeviceZ != TestInstanceState.BoardZ)
            return (false,
                $"Device is at ({state.DeviceX},{state.DeviceY},{state.DeviceZ}) but must be at board location ({TestInstanceState.BoardX},{TestInstanceState.BoardY},{TestInstanceState.BoardZ}) to connect.",
                "Device not at board location.");
        if (!state.IsPowerSupplyOn || state.SupplyVolts != 5 || state.SupplyCurrent != 3)
            return (false,
                $"Power supply must be on at 5V and 3A before connecting device to board. Current: {(state.IsPowerSupplyOn ? "" : "off, ")}{state.SupplyVolts?.ToString() ?? "not set"} V, {state.SupplyCurrent?.ToString() ?? "not set"} A.",
                "Power supply not at 5V / 3A.");
        state.IsDeviceConnectedToBoard = true;
        state.IsDevicePowered = true;
        return (true, $"Device connected to board at ({TestInstanceState.BoardX},{TestInstanceState.BoardY},{TestInstanceState.BoardZ}). Device powered on.", null);
    }

    private static (bool, string, string?) DeviceDisconnect(TestInstanceState state)
    {
        if (state.IsDeviceConnectedToThermal)
            return (false, "Thermal must be disconnected before disconnecting device from board.", "Disconnect thermal first.");
        if (state.IsDevicePickedUp)
            return (false, "Device must be released before disconnecting from board.", "Release device before disconnecting.");
        if (state.DeviceX != TestInstanceState.BoardX || state.DeviceY != TestInstanceState.BoardY || state.DeviceZ != TestInstanceState.BoardZ)
            return (false,
                $"Device must be at board location ({TestInstanceState.BoardX},{TestInstanceState.BoardY},{TestInstanceState.BoardZ}) to disconnect.",
                "Device not at board location.");
        state.IsDeviceConnectedToBoard = false;
        return (true, "Device disconnected from board.", null);
    }

    private static (bool, string, string?) ReadTemperature(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToBoard)
            return (false, "Device must be connected to board before reading temperature.", "Connect device to board first.");
        if (!state.IsDeviceConnectedToThermal)
            return (false, "Device must be connected to thermal cell before reading temperature.", "Connect thermal cell first.");
        if (state.LastValidatedTemperatureCelsius is not int temp)
            return (true, "No temperature set.", null);
        return (true, $"Temperature: {temp} °C.", null);
    }

    private static (bool, string, string?) DeviceConnectToNetwork(TestInstanceState state)
    {
        if (!state.IsMltTransmissionStarted)
            return (false, "MLT transmission must be started before connecting device to network.", "Start MLT transmission first.");
        state.IsDeviceConnectedToNetwork = true;
        return (true, "Device connected to MLT network.", null);
    }

    private static (bool, string, string?) DeviceSendSms(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToNetwork)
            return (false, "Device must be connected to network before sending SMS.", "Connect to network first.");
        if (state.IsSmsInFlight)
            return (false, "An SMS is already in transit. Wait for MLT to receive it first.", "SMS already in transit.");
        state.IsSmsInFlight = true;
        state.SmsSentAt = DateTime.UtcNow;
        return (true, "SMS sent. Waiting for MLT to receive.", null);
    }

    private static (bool, string, string?) DeviceMakeVoiceCall(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToNetwork)
            return (false, "Device must be connected to network before making a voice call.", "Connect to network first.");
        if (state.IsVoiceCallActive)
            return (false, "A voice call is already active.", "Call already active.");
        state.IsVoiceCallActive = true;
        state.VoiceCallStartedAt = DateTime.UtcNow;
        return (true, "Voice call initiated. Waiting for MLT to receive.", null);
    }

    private static (bool, string, string?) DeviceEndVoiceCall(TestInstanceState state)
    {
        if (!state.IsVoiceCallActive)
            return (false, "No active voice call to end.", "No active call.");
        state.IsVoiceCallActive = false;
        state.VoiceCallStartedAt = null;
        return (true, "Voice call ended.", null);
    }

    private static (bool, string, string?) DeviceGoOnYoutube(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToNetwork)
            return (false, "Device must be connected to network before going on YouTube.", "Connect to network first.");
        if (state.IsYoutubeSessionActive)
            return (false, "A YouTube session is already active.", "YouTube session already active.");
        state.IsYoutubeSessionActive = true;
        state.YoutubeStartedAt = DateTime.UtcNow;
        return (true, "YouTube session started. Waiting for MLT to receive request.", null);
    }

    private static (bool, string, string?) DeviceGetOffYoutube(TestInstanceState state)
    {
        if (!state.IsYoutubeSessionActive)
            return (false, "No active YouTube session to end.", "No YouTube session active.");
        state.IsYoutubeSessionActive = false;
        state.YoutubeStartedAt = null;
        return (true, "YouTube session ended.", null);
    }

    private static (bool, string, string?) DeviceStartSpeedTest(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToNetwork)
            return (false, "Device must be connected to network before starting a speed test.", "Connect to network first.");
        if (state.IsSpeedTestInFlight)
            return (false, "A speed test is already in progress.", "Speed test already running.");
        state.IsSpeedTestInFlight = true;
        state.SpeedTestStartedAt = DateTime.UtcNow;
        return (true, "Speed test started. Sending 8 sample bytes to MLT.", null);
    }

    // ── Thermal ──────────────────────────────────────────────────────────────

    private static (bool, string, string?) ThermalConnect(ExecutorStepDto step, TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToBoard)
            return (false, "Device must be connected to board before connecting thermal cell.", "Connect device to board first.");
        if (step.TemperatureCelsius is not int temp || temp < 20 || temp > 100)
            return (false, "Invalid temperature. Must be an integer from 20 to 100 °C.", "Invalid temperature.");
        state.LastValidatedTemperatureCelsius = temp;
        state.IsDeviceConnectedToThermal = true;
        return (true, $"Thermal cell connected to device. Temperature set and validated at {temp} °C.", null);
    }

    private static (bool, string, string?) ThermalDisconnect(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToThermal)
            return (false, "Thermal cell is not connected.", "Thermal not connected.");
        if (state.DeviceX != TestInstanceState.BoardX || state.DeviceY != TestInstanceState.BoardY || state.DeviceZ != TestInstanceState.BoardZ)
            return (false,
                $"Device must be at board location ({TestInstanceState.BoardX},{TestInstanceState.BoardY},{TestInstanceState.BoardZ}) to disconnect thermal.",
                "Device not at board location.");
        state.IsDeviceConnectedToThermal = false;
        return (true, "Thermal cell disconnected.", null);
    }

    // ── MLT ──────────────────────────────────────────────────────────────────

    private static (bool, string, string?) MltPowerOn(TestInstanceState state)
    {
        state.IsMltPowered = true;
        return (true, "MLT box powered on.", null);
    }

    private static (bool, string, string?) MltPowerOff(TestInstanceState state)
    {
        state.IsMltPowered = false;
        return (true, "MLT box powered off.", null);
    }

    private static (bool, string, string?) MltConnect(TestInstanceState state)
    {
        if (!state.IsDeviceConnectedToBoard)
            return (false, "Device must be connected to board before connecting MLT box.", "Connect device to board first.");
        state.IsMltConnected = true;
        return (true, "MLT box connected.", null);
    }

    private static (bool, string, string?) MltDisconnect(TestInstanceState state)
    {
        if (state.IsMltTransmissionStarted)
            return (false, "MLT transmission must be ended before disconnecting MLT box.", "End transmission first.");
        state.IsMltConnected = false;
        return (true, "MLT box disconnected.", null);
    }

    private static (bool, string, string?) MltStartTransmission(TestInstanceState state)
    {
        if (!state.IsMltConnected)
            return (false, "MLT box must be connected before starting transmission.", "Connect MLT first.");
        if (!state.IsDeviceConnectedToBoard)
            return (false, "Device must be connected to board before starting MLT transmission.", "Connect device to board first.");
        state.IsMltTransmissionStarted = true;
        return (true, "MLT transmission started. Network ready.", null);
    }

    private static (bool, string, string?) MltEndTransmission(TestInstanceState state)
    {
        if (!state.IsMltTransmissionStarted)
            return (false, "No active MLT transmission to end.", "No active transmission.");
        state.IsMltTransmissionStarted = false;
        return (true, "MLT transmission ended.", null);
    }

    private static (bool, string, string?) MltReceiveSms(TestInstanceState state)
    {
        if (!state.IsSmsInFlight)
            return (false, "No SMS in transit — Device.SendSms must be called first.", "No SMS sent.");
        var elapsed = (long)(DateTime.UtcNow - state.SmsSentAt!.Value).TotalMilliseconds;
        state.IsSmsInFlight = false;
        state.SmsSentAt = null;
        return (true, $"SMS received in {elapsed} ms.", null);
    }

    private static (bool, string, string?) MltReceiveVoiceCall(TestInstanceState state)
    {
        if (!state.IsVoiceCallActive)
            return (false, "No active voice call — Device.MakeVoiceCall must be called first.", "No active call.");
        var elapsed = (long)(DateTime.UtcNow - state.VoiceCallStartedAt!.Value).TotalMilliseconds;
        return (true, $"Voice call received in {elapsed} ms.", null);
    }

    private static (bool, string, string?) MltReceiveYoutubeRequest(TestInstanceState state)
    {
        if (!state.IsYoutubeSessionActive)
            return (false, "No active YouTube session — Device.GoOnYoutube must be called first.", "No YouTube session.");
        var elapsed = (long)(DateTime.UtcNow - state.YoutubeStartedAt!.Value).TotalMilliseconds;
        return (true, $"YouTube request received in {elapsed} ms.", null);
    }

    private static (bool, string, string?) MltStopSpeedTest(TestInstanceState state)
    {
        if (!state.IsSpeedTestInFlight)
            return (false, "No speed test in progress — Device.StartSpeedTest must be called first.", "No speed test running.");
        var elapsed = (long)(DateTime.UtcNow - state.SpeedTestStartedAt!.Value).TotalMilliseconds;
        state.IsSpeedTestInFlight = false;
        state.SpeedTestStartedAt = null;
        return (true, $"Speed test complete: 8 bytes transferred in {elapsed} ms.", null);
    }

    // ── Power Supply ─────────────────────────────────────────────────────────

    private static (bool, string, string?) PowerSupplyPowerOn(TestInstanceState state)
    {
        state.IsPowerSupplyOn = true;
        return (true, "Power supply on.", null);
    }

    private static (bool, string, string?) PowerSupplyPowerOff(TestInstanceState state)
    {
        state.IsPowerSupplyOn = false;
        state.IsDevicePowered = false;
        return (true, "Power supply off. Device powered off.", null);
    }

    private static (bool, string, string?) SetSupplyPower(ExecutorStepDto step, TestInstanceState state)
    {
        if (!state.IsPowerSupplyOn)
            return (false, "Power supply must be on before setting voltage.", "Power supply not on.");
        if (step.Volts is not int v || v < 1 || v > 10)
            return (false, "Invalid voltage. Must be an integer from 1 to 10 V.", "Invalid voltage.");
        state.SupplyVolts = v;
        return (true, $"Supply voltage set to {v} V.", null);
    }

    private static (bool, string, string?) SetSupplyCurrent(ExecutorStepDto step, TestInstanceState state)
    {
        if (!state.IsPowerSupplyOn)
            return (false, "Power supply must be on before setting current.", "Power supply not on.");
        if (step.Amps is not int a || a < 1 || a > 10)
            return (false, "Invalid current. Must be an integer from 1 to 10 A.", "Invalid current.");
        state.SupplyCurrent = a;
        return (true, $"Supply current set to {a} A.", null);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? SerializeParameters(ExecutorStepDto step)
    {
        var dict = new Dictionary<string, object?>();
        if (step.TemperatureCelsius.HasValue) dict["temperatureCelsius"] = step.TemperatureCelsius;
        if (step.Millimeters.HasValue) dict["millimeters"] = step.Millimeters;
        if (step.RotationDegrees.HasValue) dict["rotationDegrees"] = step.RotationDegrees;
        if (step.Volts.HasValue) dict["volts"] = step.Volts;
        if (step.Amps.HasValue) dict["amps"] = step.Amps;
        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }
}
