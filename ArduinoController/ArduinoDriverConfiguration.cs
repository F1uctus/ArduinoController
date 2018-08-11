using ArduinoController.Hardware;

namespace ArduinoController {
    internal class ArduinoDriverConfiguration {
        public ArduinoModel ArduinoModel  { get; set; }
        public string       PortName      { get; set; }
        public bool         AutoBootstrap { get; set; }
    }
}