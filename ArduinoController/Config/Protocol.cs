using System.Diagnostics.CodeAnalysis;

namespace ArduinoController.Config {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum Protocol {
        Stk500v1,
        Stk500v2,
        Avr109
    }
}