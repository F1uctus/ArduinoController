﻿namespace ArduinoController.SerialProtocol {
    public class AnalogReadRequest : ArduinoRequest {
        public AnalogReadRequest(byte pinToRead)
            : base(CommandConstants.AnalogRead) {
            Bytes.Add(pinToRead);
        }
    }
}