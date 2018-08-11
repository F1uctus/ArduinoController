namespace ArduinoController.SerialProtocol {
    public class ErrorResponse : ArduinoResponse {
        public byte Byte1 { get; }
        public byte Byte2 { get; }
        public byte Byte3 { get; }

        public ErrorResponse(byte byte1, byte byte2, byte byte3) {
            Byte1 = byte1;
            Byte2 = byte2;
            Byte3 = byte3;
        }
    }
}