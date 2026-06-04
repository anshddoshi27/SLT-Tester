namespace SltVirtualTest.Api.Services;

public class TestInstanceState
{
    public const int BoardX = 100;
    public const int BoardY = 0;
    public const int BoardZ = 0;

    public bool IsDevicePowered { get; set; }

    public bool IsHandlerConnected { get; set; }
    public int HandlerX { get; set; }
    public int HandlerY { get; set; }
    public int HandlerZ { get; set; }
    public int HandlerRotationDegrees { get; set; }

    public int DeviceX { get; set; } = 300;
    public int DeviceY { get; set; }
    public int DeviceZ { get; set; }
    public bool IsDevicePickedUp { get; set; }

    public bool IsDeviceConnectedToBoard { get; set; }
    public bool IsDeviceConnectedToThermal { get; set; }
    public int? LastValidatedTemperatureCelsius { get; set; }

    // Power Supply
    public int? SupplyVolts { get; set; }
    public int? SupplyCurrent { get; set; }
    public bool IsPowerSupplyOn { get; set; }

    // MLT
    public bool IsMltPowered { get; set; }
    public bool IsMltConnected { get; set; }
    public bool IsMltTransmissionStarted { get; set; }
    public bool IsDeviceConnectedToNetwork { get; set; }

    // Active sessions / timers
    public bool IsSmsInFlight { get; set; }
    public DateTime? SmsSentAt { get; set; }
    public bool IsVoiceCallActive { get; set; }
    public DateTime? VoiceCallStartedAt { get; set; }
    public bool IsYoutubeSessionActive { get; set; }
    public DateTime? YoutubeStartedAt { get; set; }
    public bool IsSpeedTestInFlight { get; set; }
    public DateTime? SpeedTestStartedAt { get; set; }
}
