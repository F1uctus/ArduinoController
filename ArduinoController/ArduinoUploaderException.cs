using System;

namespace ArduinoController {
    public class ArduinoUploaderException : Exception {
        public ArduinoUploaderException(string message) : base(message) {
        }
    }
}