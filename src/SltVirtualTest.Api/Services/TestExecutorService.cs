using System.Text.Json;
using SltVirtualTest.Api.Data;
using SltVirtualTest.Api.Data.Entities;
using SltVirtualTest.Shared.Dtos;
using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Api.Services;

public class TestExecutorService(AppDbContext db)
{
    public const string ReadBeforeConnectMessage = "read temperature before any connect";

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
            (TestModule.Thermal, TestMethod.PowerOn) => PowerOn(state),
            (TestModule.Thermal, TestMethod.PowerOff) => PowerOff(state),
            (TestModule.Thermal, TestMethod.Connect) => ThermalConnect(step, state),
            (TestModule.Thermal, TestMethod.Disconnect) => ThermalDisconnect(state),

            (TestModule.Handler, TestMethod.PowerOn) => PowerOn(state),
            (TestModule.Handler, TestMethod.PowerOff) => PowerOff(state),
            (TestModule.Handler, TestMethod.Connect) => HandlerConnect(state),
            (TestModule.Handler, TestMethod.Disconnect) => HandlerDisconnect(state),
            (TestModule.Handler, TestMethod.MoveRight) => Move(state, step.Millimeters ?? 0, dx: 1),
            (TestModule.Handler, TestMethod.MoveLeft) => Move(state, step.Millimeters ?? 0, dx: -1),
            (TestModule.Handler, TestMethod.MoveUp) => Move(state, step.Millimeters ?? 0, dy: 1),
            (TestModule.Handler, TestMethod.MoveDown) => Move(state, step.Millimeters ?? 0, dy: -1),
            (TestModule.Handler, TestMethod.Rotation) => Rotate(state, step.RotationDegrees ?? 0),

            (TestModule.Device, TestMethod.PowerOn) => PowerOn(state),
            (TestModule.Device, TestMethod.PowerOff) => PowerOff(state),
            (TestModule.Device, TestMethod.Connect) => DeviceConnect(state),
            (TestModule.Device, TestMethod.Disconnect) => DeviceDisconnect(state),
            (TestModule.Device, TestMethod.ReadTemperature) => ReadTemperature(state),

            _ => (false, $"Unknown step: {step.Module}.{step.Method}", null)
        };
    }

    private static (bool, string, string?) PowerOn(TestInstanceState state)
    {
        state.IsDevicePowered = true;
        return (true, "Device powered on.", null);
    }

    private static (bool, string, string?) PowerOff(TestInstanceState state)
    {
        state.IsDevicePowered = false;
        return (true, "Device powered off.", null);
    }

    private static (bool, string, string?) ThermalConnect(ExecutorStepDto step, TestInstanceState state)
    {
        PowerOn(state);

        if (step.TemperatureCelsius is not int temp || temp < 20 || temp > 100)
            return (false, "Invalid temperature. Must be an integer from 20 to 100 °C.", "Invalid temperature");

        state.LastValidatedTemperatureCelsius = temp;
        state.IsUsbConnected = true;
        return (true, $"USB connected to SLT. Temperature set and validated at {temp} °C.", null);
    }

    private static (bool, string, string?) ThermalDisconnect(TestInstanceState state)
    {
        PowerOff(state);
        state.IsUsbConnected = false;
        return (true, "USB disconnected from device. Device powered off.", null);
    }

    private static (bool, string, string?) HandlerConnect(TestInstanceState state)
    {
        PowerOn(state);
        state.IsHandlerConnected = true;
        return (true, "Handler connected to device. Device powered on.", null);
    }

    private static (bool, string, string?) HandlerDisconnect(TestInstanceState state)
    {
        PowerOff(state);
        state.IsHandlerConnected = false;
        return (true, "Handler disconnected. Device powered off.", null);
    }

    private static (bool, string, string?) DeviceConnect(TestInstanceState state)
    {
        PowerOn(state);
        state.IsUsbConnected = true;
        return (true, "USB connected to SLT. Device powered on.", null);
    }

    private static (bool, string, string?) DeviceDisconnect(TestInstanceState state)
    {
        PowerOff(state);
        state.IsUsbConnected = false;
        return (true, "USB disconnected from device. Device powered off.", null);
    }

    private static (bool, string, string?) ReadTemperature(TestInstanceState state)
    {
        if (!state.IsUsbConnected)
            return (false, ReadBeforeConnectMessage, ReadBeforeConnectMessage);

        if (state.LastValidatedTemperatureCelsius is not int temp)
            return (true, "No temperature.", null);

        return (true, $"Temperature: {temp} °C.", null);
    }

    private static (bool, string, string?) Move(TestInstanceState state, int mm, int dx = 0, int dy = 0)
    {
        state.HandlerX += dx * mm;
        state.HandlerY += dy * mm;
        var direction = dx switch
        {
            1 => "right",
            -1 => "left",
            _ => dy switch
            {
                1 => "up",
                -1 => "down",
                _ => "unknown"
            }
        };
        return (true, $"Moved {direction} {Math.Abs(mm)} mm. Position: ({state.HandlerX}, {state.HandlerY}, {state.HandlerZ}).", null);
    }

    private static (bool, string, string?) Rotate(TestInstanceState state, int degrees)
    {
        state.HandlerRotationDegrees += degrees;
        var direction = degrees >= 0 ? "clockwise (right)" : "counterclockwise (left)";
        return (true, $"Rotated {Math.Abs(degrees)}° {direction}. Total rotation: {state.HandlerRotationDegrees}°.", null);
    }

    private static string? SerializeParameters(ExecutorStepDto step)
    {
        var dict = new Dictionary<string, object?>();
        if (step.TemperatureCelsius.HasValue) dict["temperatureCelsius"] = step.TemperatureCelsius;
        if (step.Millimeters.HasValue) dict["millimeters"] = step.Millimeters;
        if (step.RotationDegrees.HasValue) dict["rotationDegrees"] = step.RotationDegrees;
        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }
}
