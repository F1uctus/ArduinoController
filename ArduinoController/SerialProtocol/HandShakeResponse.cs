namespace ArduinoController.SerialProtocol {
    public class HandShakeResponse : ArduinoResponse {
        public int ProtocolMajorVersion { get; }
        public int ProtocolMinorVersion { get; }

        public HandShakeResponse(byte protocolMajorVersion, byte protocolMinorVersion) {
            ProtocolMajorVersion = protocolMajorVersion;
            ProtocolMinorVersion = protocolMinorVersion;
        }
    }
}