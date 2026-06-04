using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Shared.Dtos;

public record ExecutorStepDto(
    TestModule Module,
    TestMethod Method,
    int? TemperatureCelsius,
    int? Millimeters,
    int? RotationDegrees,
    int? Volts,
    int? Amps);

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
    bool IncludesPowerOn,
    bool RequiresVolts,
    bool RequiresAmps);

public static class StepCatalog
{
    public static readonly IReadOnlyList<StepDefinitionDto> All =
    [
        // Thermal
        new(TestModule.Thermal, TestMethod.Connect,               "Connect (Thermal)",      true,  false, false, false, false, false),
        new(TestModule.Thermal, TestMethod.Disconnect,             "Disconnect (Thermal)",   false, false, false, false, false, false),

        // Handler
        new(TestModule.Handler, TestMethod.Connect,               "Connect (Handler)",      false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.Disconnect,             "Disconnect (Handler)",   false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveRight,              "Move Right",             false, true,  false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveLeft,               "Move Left",              false, true,  false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveUp,                 "Move Up",                false, true,  false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveDown,               "Move Down",              false, true,  false, false, false, false),
        new(TestModule.Handler, TestMethod.Rotation,               "Rotation",               false, false, true,  false, false, false),
        new(TestModule.Handler, TestMethod.PickUpDevice,           "Pick Up Device",         false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.ReleaseDevice,          "Release Device",         false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveToBoard,            "Move to Board",          false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveToDevice,           "Move to Device",         false, false, false, false, false, false),
        new(TestModule.Handler, TestMethod.MoveToOrigin,           "Move to Origin",         false, false, false, false, false, false),

        // Device
        new(TestModule.Device,  TestMethod.Connect,               "Connect (Board)",        false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.Disconnect,             "Disconnect (Board)",     false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.ReadTemperature,        "Read Temperature",       false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.ConnectToNetwork,       "Connect to Network",     false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.SendSms,                "Send SMS",               false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.MakeVoiceCall,          "Make Voice Call",        false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.EndVoiceCall,           "End Voice Call",         false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.GoOnYoutube,            "Go on YouTube",          false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.GetOffYoutube,          "Get Off YouTube",        false, false, false, false, false, false),
        new(TestModule.Device,  TestMethod.StartSpeedTest,         "Start Speed Test",       false, false, false, false, false, false),

        // MLT
        new(TestModule.Mlt, TestMethod.Connect,                   "Connect (MLT)",          false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.Disconnect,                 "Disconnect (MLT)",       false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.PowerOn,                    "Power On",               false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.PowerOff,                   "Power Off",              false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.StartTransmission,          "Start Transmission",     false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.EndTransmission,            "End Transmission",       false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.ReceiveSms,                 "Receive SMS",            false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.ReceiveVoiceCall,           "Receive Voice Call",     false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.ReceiveYoutubeRequest,      "Receive YouTube Req",    false, false, false, false, false, false),
        new(TestModule.Mlt, TestMethod.StopSpeedTest,              "Stop Speed Test",        false, false, false, false, false, false),

        // PowerSupply
        new(TestModule.PowerSupply, TestMethod.PowerOn,            "Power On",               false, false, false, true,  false, false),
        new(TestModule.PowerSupply, TestMethod.PowerOff,           "Power Off",              false, false, false, false, false, false),
        new(TestModule.PowerSupply, TestMethod.SupplyPower,        "Supply Power",           false, false, false, false, true,  false),
        new(TestModule.PowerSupply, TestMethod.SupplyCurrent,      "Supply Current",         false, false, false, false, false, true),
    ];
}
