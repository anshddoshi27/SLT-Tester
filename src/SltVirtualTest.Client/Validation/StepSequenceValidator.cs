using SltVirtualTest.Client.Models;
using SltVirtualTest.Shared.Models;

namespace SltVirtualTest.Client.Validation;

public static class StepSequenceValidator
{
    private const int BoardX = 100, BoardY = 0, BoardZ = 0;
    private const int DeviceStartX = 300, DeviceStartY = 0, DeviceStartZ = 0;

    // Returns a list of (stepIndex, errorMessage) for every step that would fail.
    public static List<(int StepIndex, string Message)> Validate(IReadOnlyList<ExecutorStepItem> steps)
    {
        var errors = new List<(int, string)>();

        // Handler / Device / Thermal state
        bool isHandlerConnected = false;
        bool isDevicePickedUp = false;
        bool isDeviceConnectedToBoard = false;
        bool isDeviceConnectedToThermal = false;
        int handlerX = 0, handlerY = 0, handlerZ = 0;
        int deviceX = DeviceStartX, deviceY = DeviceStartY, deviceZ = DeviceStartZ;

        // Power Supply state
        bool isPowerSupplyOn = false;
        int? supplyVolts = null;
        int? supplyAmps = null;

        // MLT state
        bool isMltConnected = false;
        bool isMltTransmissionStarted = false;
        bool isDeviceConnectedToNetwork = false;

        // Session state (no timestamps needed client-side — presence only)
        bool isSmsInFlight = false;
        bool isVoiceCallActive = false;
        bool isYoutubeSessionActive = false;
        bool isSpeedTestInFlight = false;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            string? error = null;

            switch (step.Module, step.Method)
            {
                // ── Handler ────────────────────────────────────────────────────
                case (TestModule.Handler, TestMethod.Connect):
                    isHandlerConnected = true;
                    break;

                case (TestModule.Handler, TestMethod.Disconnect):
                    isHandlerConnected = false;
                    break;

                case (TestModule.Handler, TestMethod.MoveRight):
                case (TestModule.Handler, TestMethod.MoveLeft):
                case (TestModule.Handler, TestMethod.MoveUp):
                case (TestModule.Handler, TestMethod.MoveDown):
                    if (!isHandlerConnected)
                    {
                        error = "Handler must be connected before moving.";
                    }
                    else
                    {
                        int mm = int.TryParse(step.MillimetersInput, out var m) ? m : 0;
                        handlerX += step.Method == TestMethod.MoveRight ? mm : step.Method == TestMethod.MoveLeft ? -mm : 0;
                        handlerY += step.Method == TestMethod.MoveUp ? mm : step.Method == TestMethod.MoveDown ? -mm : 0;
                        if (isDevicePickedUp) { deviceX = handlerX; deviceY = handlerY; deviceZ = handlerZ; }
                    }
                    break;

                case (TestModule.Handler, TestMethod.Rotation):
                    if (!isHandlerConnected)
                        error = "Handler must be connected before rotating.";
                    break;

                case (TestModule.Handler, TestMethod.PickUpDevice):
                    if (!isHandlerConnected)
                        error = "Handler must be connected to pick up device.";
                    else if (handlerX != deviceX || handlerY != deviceY || handlerZ != deviceZ)
                        error = $"Handler is at ({handlerX},{handlerY},{handlerZ}) but device is at ({deviceX},{deviceY},{deviceZ}). Move handler to device location first.";
                    else
                        isDevicePickedUp = true;
                    break;

                case (TestModule.Handler, TestMethod.ReleaseDevice):
                    if (!isHandlerConnected)
                        error = "Handler must be connected to release device.";
                    else if (!isDevicePickedUp)
                        error = "No device is currently picked up by the handler.";
                    else
                    {
                        deviceX = handlerX; deviceY = handlerY; deviceZ = handlerZ;
                        isDevicePickedUp = false;
                    }
                    break;

                case (TestModule.Handler, TestMethod.MoveToBoard):
                    if (!isHandlerConnected)
                        error = "Handler must be connected to move.";
                    else
                    {
                        handlerX = BoardX; handlerY = BoardY; handlerZ = BoardZ;
                        if (isDevicePickedUp) { deviceX = handlerX; deviceY = handlerY; deviceZ = handlerZ; }
                    }
                    break;

                case (TestModule.Handler, TestMethod.MoveToDevice):
                    if (!isHandlerConnected)
                        error = "Handler must be connected to move.";
                    else
                    {
                        handlerX = deviceX; handlerY = deviceY; handlerZ = deviceZ;
                    }
                    break;

                case (TestModule.Handler, TestMethod.MoveToOrigin):
                    if (!isHandlerConnected)
                        error = "Handler must be connected to move.";
                    else
                    {
                        handlerX = 0; handlerY = 0; handlerZ = 0;
                        if (isDevicePickedUp) { deviceX = 0; deviceY = 0; deviceZ = 0; }
                    }
                    break;

                // ── Device ─────────────────────────────────────────────────────
                case (TestModule.Device, TestMethod.Connect):
                    if (isDevicePickedUp)
                        error = "Device must be released by the handler before connecting to board.";
                    else if (deviceX != BoardX || deviceY != BoardY || deviceZ != BoardZ)
                        error = $"Device is at ({deviceX},{deviceY},{deviceZ}) but must be at board location ({BoardX},{BoardY},{BoardZ}). Move handler to device, pick it up, move to board, then release it.";
                    else if (!isPowerSupplyOn || supplyVolts != 5 || supplyAmps != 3)
                        error = $"Power supply must be on at 5V and 3A before connecting device to board. Current: {(isPowerSupplyOn ? "" : "off, ")}{supplyVolts?.ToString() ?? "not set"} V, {supplyAmps?.ToString() ?? "not set"} A.";
                    else
                        isDeviceConnectedToBoard = true;
                    break;

                case (TestModule.Device, TestMethod.Disconnect):
                    if (isDeviceConnectedToThermal)
                        error = "Thermal must be disconnected before disconnecting device from board.";
                    else if (isDevicePickedUp)
                        error = "Device must be released before disconnecting from board.";
                    else if (deviceX != BoardX || deviceY != BoardY || deviceZ != BoardZ)
                        error = $"Device must be at board location ({BoardX},{BoardY},{BoardZ}) to disconnect. Device is at ({deviceX},{deviceY},{deviceZ}).";
                    else
                        isDeviceConnectedToBoard = false;
                    break;

                case (TestModule.Device, TestMethod.ReadTemperature):
                    if (!isDeviceConnectedToBoard)
                        error = "Device must be connected to board before reading temperature.";
                    else if (!isDeviceConnectedToThermal)
                        error = "Thermal cell must be connected before reading temperature.";
                    break;

                case (TestModule.Device, TestMethod.ConnectToNetwork):
                    if (!isMltTransmissionStarted)
                        error = "MLT transmission must be started before connecting device to network.";
                    else
                        isDeviceConnectedToNetwork = true;
                    break;

                case (TestModule.Device, TestMethod.SendSms):
                    if (!isDeviceConnectedToNetwork)
                        error = "Device must be connected to network before sending SMS.";
                    else if (isSmsInFlight)
                        error = "An SMS is already in transit. Wait for MLT to receive it first.";
                    else
                        isSmsInFlight = true;
                    break;

                case (TestModule.Device, TestMethod.MakeVoiceCall):
                    if (!isDeviceConnectedToNetwork)
                        error = "Device must be connected to network before making a voice call.";
                    else if (isVoiceCallActive)
                        error = "A voice call is already active.";
                    else
                        isVoiceCallActive = true;
                    break;

                case (TestModule.Device, TestMethod.EndVoiceCall):
                    if (!isVoiceCallActive)
                        error = "No active voice call to end.";
                    else
                        isVoiceCallActive = false;
                    break;

                case (TestModule.Device, TestMethod.GoOnYoutube):
                    if (!isDeviceConnectedToNetwork)
                        error = "Device must be connected to network before going on YouTube.";
                    else if (isYoutubeSessionActive)
                        error = "A YouTube session is already active.";
                    else
                        isYoutubeSessionActive = true;
                    break;

                case (TestModule.Device, TestMethod.GetOffYoutube):
                    if (!isYoutubeSessionActive)
                        error = "No active YouTube session to end.";
                    else
                        isYoutubeSessionActive = false;
                    break;

                case (TestModule.Device, TestMethod.StartSpeedTest):
                    if (!isDeviceConnectedToNetwork)
                        error = "Device must be connected to network before starting a speed test.";
                    else if (isSpeedTestInFlight)
                        error = "A speed test is already in progress.";
                    else
                        isSpeedTestInFlight = true;
                    break;

                // ── Thermal ────────────────────────────────────────────────────
                case (TestModule.Thermal, TestMethod.Connect):
                    if (!isDeviceConnectedToBoard)
                        error = "Device must be connected to board before connecting thermal cell.";
                    else
                        isDeviceConnectedToThermal = true;
                    break;

                case (TestModule.Thermal, TestMethod.Disconnect):
                    if (!isDeviceConnectedToThermal)
                        error = "Thermal cell is not connected — nothing to disconnect.";
                    else if (deviceX != BoardX || deviceY != BoardY || deviceZ != BoardZ)
                        error = $"Device must be at board location ({BoardX},{BoardY},{BoardZ}) to disconnect thermal.";
                    else
                        isDeviceConnectedToThermal = false;
                    break;

                // ── MLT ────────────────────────────────────────────────────────
                case (TestModule.Mlt, TestMethod.Connect):
                    if (!isDeviceConnectedToBoard)
                        error = "Device must be connected to board before connecting MLT box.";
                    else
                        isMltConnected = true;
                    break;

                case (TestModule.Mlt, TestMethod.Disconnect):
                    if (isMltTransmissionStarted)
                        error = "MLT transmission must be ended before disconnecting MLT box.";
                    else
                        isMltConnected = false;
                    break;

                case (TestModule.Mlt, TestMethod.StartTransmission):
                    if (!isMltConnected)
                        error = "MLT box must be connected before starting transmission.";
                    else if (!isDeviceConnectedToBoard)
                        error = "Device must be connected to board before starting MLT transmission.";
                    else
                        isMltTransmissionStarted = true;
                    break;

                case (TestModule.Mlt, TestMethod.EndTransmission):
                    if (!isMltTransmissionStarted)
                        error = "No active MLT transmission to end.";
                    else
                        isMltTransmissionStarted = false;
                    break;

                case (TestModule.Mlt, TestMethod.ReceiveSms):
                    if (!isSmsInFlight)
                        error = "No SMS in transit — Device.Send SMS must be called first.";
                    else
                        isSmsInFlight = false;
                    break;

                case (TestModule.Mlt, TestMethod.ReceiveVoiceCall):
                    if (!isVoiceCallActive)
                        error = "No active voice call — Device.Make Voice Call must be called first.";
                    break;

                case (TestModule.Mlt, TestMethod.ReceiveYoutubeRequest):
                    if (!isYoutubeSessionActive)
                        error = "No active YouTube session — Device.Go on YouTube must be called first.";
                    break;

                case (TestModule.Mlt, TestMethod.StopSpeedTest):
                    if (!isSpeedTestInFlight)
                        error = "No speed test in progress — Device.Start Speed Test must be called first.";
                    else
                        isSpeedTestInFlight = false;
                    break;

                // ── Power Supply ────────────────────────────────────────────────
                case (TestModule.PowerSupply, TestMethod.PowerOn):
                    isPowerSupplyOn = true;
                    break;

                case (TestModule.PowerSupply, TestMethod.PowerOff):
                    isPowerSupplyOn = false;
                    break;

                case (TestModule.PowerSupply, TestMethod.SupplyPower):
                    if (!isPowerSupplyOn)
                        error = "Power supply must be on before setting voltage.";
                    else if (int.TryParse(step.VoltsInput, out var v) && v >= 1 && v <= 10)
                        supplyVolts = v;
                    break;

                case (TestModule.PowerSupply, TestMethod.SupplyCurrent):
                    if (!isPowerSupplyOn)
                        error = "Power supply must be on before setting current.";
                    else if (int.TryParse(step.AmpsInput, out var a) && a >= 1 && a <= 10)
                        supplyAmps = a;
                    break;

                // PowerOn/PowerOff for Mlt always succeed — no preconditions
                default:
                    break;
            }

            if (error != null)
                errors.Add((i, error));
        }

        return errors;
    }
}
