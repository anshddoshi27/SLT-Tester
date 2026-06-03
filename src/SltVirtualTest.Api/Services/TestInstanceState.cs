namespace SltVirtualTest.Api.Services;

public class TestInstanceState
{
    public bool IsDevicePowered { get; set; }
    public bool IsUsbConnected { get; set; }
    public bool IsHandlerConnected { get; set; }
    public int? LastValidatedTemperatureCelsius { get; set; }
    public int HandlerX { get; set; }
    public int HandlerY { get; set; }
    public int HandlerZ { get; set; }
    public int HandlerRotationDegrees { get; set; }
}
