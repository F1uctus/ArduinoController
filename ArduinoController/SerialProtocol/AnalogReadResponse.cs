namespace ArduinoController.SerialProtocol {
    public class AnalogReadResponse : ArduinoResponse {
        public int PinRead  { get; }
        public int PinValue { get; }

        public AnalogReadResponse(byte pinRead, byte byte1, byte byte2) {
            PinRead  = pinRead;
            PinValue = (byte1 << 8) + byte2;
        }
    }
}