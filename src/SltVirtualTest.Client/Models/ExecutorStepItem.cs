using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Client.Models;

public class ExecutorStepItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TestModule Module { get; set; }
    public TestMethod Method { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool RequiresTemperature { get; set; }
    public bool RequiresMillimeters { get; set; }
    public bool RequiresRotation { get; set; }
    public bool IncludesPowerOn { get; set; }

    public string TemperatureInput { get; set; } = string.Empty;
    public string MillimetersInput { get; set; } = string.Empty;
    public string RotationInput { get; set; } = string.Empty;
}
