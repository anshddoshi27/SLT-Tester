using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Shared.Dtos;

public record ExecutorStepDto(
    TestModule Module,
    TestMethod Method,
    int? TemperatureCelsius,
    int? Millimeters,
    int? RotationDegrees);

public record ExecuteTestRequest(Guid UserId, IReadOnlyList<ExecutorStepDto> Steps);

public record LogEntryDto(
    int Index,
    string StepLabel,
    bool Success,
    string Message,
    DateTime Timestamp);

public record ExecuteTestResponse(
    Guid TestRunId,
    bool Success,
    string? FailurePopupMessage,
    IReadOnlyList<LogEntryDto> LogEntries);

public record StepDefinitionDto(
    TestModule Module,
    TestMethod Method,
    string DisplayName,
    bool RequiresTemperature,
    bool RequiresMillimeters,
    bool RequiresRotation,
    bool IncludesPowerOn);

public static class StepCatalog
{
    public static readonly IReadOnlyList<StepDefinitionDto> All =
    [
        new(TestModule.Thermal, TestMethod.PowerOn, "Power On", false, false, false, false),
        new(TestModule.Thermal, TestMethod.PowerOff, "Power Off", false, false, false, false),
        new(TestModule.Thermal, TestMethod.Connect, "Connect (USB)", true, false, false, true),
        new(TestModule.Thermal, TestMethod.Disconnect, "Disconnect (USB)", false, false, false, false),
        new(TestModule.Handler, TestMethod.Connect, "Connect (Handler)", false, false, false, true),
        new(TestModule.Handler, TestMethod.Disconnect, "Disconnect (Handler)", false, false, false, false),
        new(TestModule.Handler, TestMethod.PowerOn, "Power On", false, false, false, false),
        new(TestModule.Handler, TestMethod.PowerOff, "Power Off", false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveRight, "Move Right", false, true, false, false),
        new(TestModule.Handler, TestMethod.MoveLeft, "Move Left", false, true, false, false),
        new(TestModule.Handler, TestMethod.MoveUp, "Move Up", false, true, false, false),
        new(TestModule.Handler, TestMethod.MoveDown, "Move Down", false, true, false, false),
        new(TestModule.Handler, TestMethod.Rotation, "Rotation", false, false, true, false),
        new(TestModule.Device, TestMethod.PowerOn, "Power On", false, false, false, false),
        new(TestModule.Device, TestMethod.PowerOff, "Power Off", false, false, false, false),
        new(TestModule.Device, TestMethod.Connect, "Connect (USB)", false, false, false, true),
        new(TestModule.Device, TestMethod.Disconnect, "Disconnect (USB)", false, false, false, false),
        new(TestModule.Device, TestMethod.ReadTemperature, "Read Temperature", false, false, false, false)
    ];
}
