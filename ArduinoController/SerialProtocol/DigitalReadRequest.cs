namespace ArduinoController.SerialProtocol {
    public class DigitalReadRequest : ArduinoRequest {
        public DigitalReadRequest(byte pinToRead)
            : base(CommandConstants.DigitalRead) {
            Bytes.Add(pinToRead);
        }
    }
}