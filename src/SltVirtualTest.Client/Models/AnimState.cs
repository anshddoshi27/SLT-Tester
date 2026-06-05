namespace SltVirtualTest.Client.Models;

public class AnimState
{
    public bool IsPowerSupplyOn { get; set; }
    public int? SupplyVolts { get; set; }
    public int? SupplyCurrent { get; set; }

    public bool IsHandlerConnected { get; set; }
    public double HandlerX { get; set; } = 0;
    public double HandlerY { get; set; } = 0;
    public double HandlerRotation { get; set; } = 0;
    public bool IsDevicePickedUp { get; set; }

    public double DeviceX { get; set; } = 300;
    public double DeviceY { get; set; } = 0;
    public bool IsDeviceConnectedToBoard { get; set; }
    public bool IsDeviceConnectedToThermal { get; set; }
    public int? LastTemperature { get; set; }

    public bool IsMltPowered { get; set; }
    public bool IsMltConnected { get; set; }
    public bool IsMltTransmissionStarted { get; set; }
    public bool IsDeviceConnectedToNetwork { get; set; }

    public bool IsSmsInFlight { get; set; }
    public bool IsVoiceCallActive { get; set; }
    public bool IsYoutubeSessionActive { get; set; }
    public bool IsSpeedTestInFlight { get; set; }
}
