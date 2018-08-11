namespace ArduinoController.SerialProtocol {
    public class DigitalReadResponse : ArduinoResponse {
        public int          PinRead  { get; }
        public DigitalValue PinValue { get; }

        public DigitalReadResponse(byte pinRead, DigitalValue value) {
            PinRead  = pinRead;
            PinValue = value;
        }
    }
}