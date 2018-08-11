namespace ArduinoController.SerialProtocol {
    public class PinModeResponse : ArduinoResponse {
        public int     Pin  { get; }
        public PinMode Mode { get; }

        public PinModeResponse(byte pin, PinMode mode) {
            Pin  = pin;
            Mode = mode;
        }
    }
}