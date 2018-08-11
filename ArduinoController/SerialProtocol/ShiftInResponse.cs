namespace ArduinoController.SerialProtocol {
    public class ShiftInResponse : ArduinoResponse {
        public int      DigitalPin { get; }
        public int      ClockPin   { get; }
        public BitOrder BitOrder   { get; }
        public byte     Incoming   { get; }

        public ShiftInResponse(byte digitalPin, byte clockPin, BitOrder bitOrder, byte incoming) {
            DigitalPin = digitalPin;
            ClockPin   = clockPin;
            BitOrder   = bitOrder;
            Incoming   = incoming;
        }
    }
}