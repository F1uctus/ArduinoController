using ArduinoController.Hardware;

namespace ArduinoController {
    public class ArduinoSketchUploaderOptions {
        public string FileName { get; set; }

        public string PortName { get; set; }

        public ArduinoModel ArduinoModel { get; set; }
    }
}