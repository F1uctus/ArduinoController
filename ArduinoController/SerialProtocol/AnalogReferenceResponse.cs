namespace ArduinoController.SerialProtocol {
    public class AnalogReferenceResponse : ArduinoResponse {
        public AnalogReferenceType ReferenceType { get; }

        public AnalogReferenceResponse(AnalogReferenceType referenceType) {
            ReferenceType = referenceType;
        }
    }
}