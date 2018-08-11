namespace ArduinoController.SerialProtocol {
    public class AnalogWriteResponse : ArduinoResponse {
        public int PinWritten   { get; }
        public int ValueWritten { get; }

        public AnalogWriteResponse(byte pinRead, byte value) {
            PinWritten   = pinRead;
            ValueWritten = value;
        }
    }
}