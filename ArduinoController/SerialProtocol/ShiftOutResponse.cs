namespace ArduinoController.SerialProtocol {
    public class ShiftOutResponse : ArduinoResponse {
        public int      DigitalPin { get; }
        public int      ClockPin   { get; }
        public BitOrder BitOrder   { get; }
        public byte     Value      { get; }

        public ShiftOutResponse(byte digitalPin, byte clockPin, BitOrder bitOrder, byte value) {
            DigitalPin = digitalPin;
            ClockPin   = clockPin;
            BitOrder   = bitOrder;
            Value      = value;
        }
    }
}