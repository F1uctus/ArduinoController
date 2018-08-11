namespace ArduinoController.SerialProtocol {
    public class HandShakeRequest : ArduinoRequest {
        public HandShakeRequest()
            : base(CommandConstants.HandshakeInitiate) {
        }
    }
}