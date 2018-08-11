﻿namespace ArduinoController.SerialProtocol {
    public class PinModeRequest : ArduinoRequest {
        public PinModeRequest(byte pin, PinMode mode)
            : base(CommandConstants.PinMode) {
            Bytes.Add(pin);
            Bytes.Add((byte) mode);
        }
    }
}