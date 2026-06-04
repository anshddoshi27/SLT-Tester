namespace SltVirtualTest.Shared.Models;

public enum TestMethod
{
    PowerOn,
    PowerOff,
    Connect,
    Disconnect,
    MoveRight,
    MoveLeft,
    MoveUp,
    MoveDown,
    Rotation,
    ReadTemperature,
    PickUpDevice,
    ReleaseDevice,
    MoveToBoard,
    MoveToDevice,
    MoveToOrigin,
    // PowerSupply
    SupplyPower,
    SupplyCurrent,
    // MLT
    StartTransmission,
    EndTransmission,
    ReceiveSms,
    ReceiveVoiceCall,
    ReceiveYoutubeRequest,
    StopSpeedTest,
    // Device (network)
    ConnectToNetwork,
    SendSms,
    MakeVoiceCall,
    EndVoiceCall,
    GoOnYoutube,
    GetOffYoutube,
    StartSpeedTest
}
