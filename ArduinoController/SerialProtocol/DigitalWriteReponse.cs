namespace ArduinoController.SerialProtocol {
    public class DigitalWriteReponse : ArduinoResponse {
        public int          PinWritten { get; }
        public DigitalValue PinValue   { get; }

        public DigitalWriteReponse(byte pinRead, DigitalValue value) {
            PinWritten = pinRead;
            PinValue   = value;
        }
    }
}