using SltVirtualTest.Client.Models;
using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Client.Validation;

public static class StepValidation
{
    public const int MinTemperature = 20;
    public const int MaxTemperature = 100;
    public const int MinVolts = 1;
    public const int MaxVolts = 10;
    public const int MinAmps = 1;
    public const int MaxAmps = 10;

    public static bool TryParseTemperature(string input, out int value) =>
        int.TryParse(input, out value) && value >= MinTemperature && value <= MaxTemperature;

    public static bool TryParseInteger(string input, out int value) =>
        int.TryParse(input, out value);

    public static bool TryParseVolts(string input, out int value) =>
        int.TryParse(input, out value) && value >= MinVolts && value <= MaxVolts;

    public static bool TryParseAmps(string input, out int value) =>
        int.TryParse(input, out value) && value >= MinAmps && value <= MaxAmps;

    public static bool IsStepValid(ExecutorStepItem step)
    {
        if (step.RequiresTemperature && !TryParseTemperature(step.TemperatureInput, out _))
            return false;
        if (step.RequiresMillimeters && !TryParseInteger(step.MillimetersInput, out _))
            return false;
        if (step.RequiresRotation && !TryParseInteger(step.RotationInput, out _))
            return false;
        if (step.RequiresVolts && !TryParseVolts(step.VoltsInput, out _))
            return false;
        if (step.RequiresAmps && !TryParseAmps(step.AmpsInput, out _))
            return false;
        return true;
    }

    public static bool HasInvalidTemperature(ExecutorStepItem step) =>
        step.RequiresTemperature &&
        !string.IsNullOrWhiteSpace(step.TemperatureInput) &&
        !TryParseTemperature(step.TemperatureInput, out _);

    public static bool CanRun(IReadOnlyList<ExecutorStepItem> steps)
    {
        if (steps.Count == 0) return false;
        return steps.All(s =>
        {
            if (s.RequiresTemperature && string.IsNullOrWhiteSpace(s.TemperatureInput)) return false;
            if (s.RequiresMillimeters && string.IsNullOrWhiteSpace(s.MillimetersInput)) return false;
            if (s.RequiresRotation && string.IsNullOrWhiteSpace(s.RotationInput)) return false;
            if (s.RequiresVolts && string.IsNullOrWhiteSpace(s.VoltsInput)) return false;
            if (s.RequiresAmps && string.IsNullOrWhiteSpace(s.AmpsInput)) return false;
            return IsStepValid(s);
        });
    }

    public static bool IsUsbConnectStep(ExecutorStepItem step) =>
        step.Method == TestMethod.Connect &&
        (step.Module == TestModule.Thermal || step.Module == TestModule.Device);
}
