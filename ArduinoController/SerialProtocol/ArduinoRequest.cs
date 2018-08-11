using System;
using System.Collections.Generic;
using System.Linq;

namespace ArduinoController.SerialProtocol {
    public abstract class ArduinoRequest : ArduinoMessage {
        protected ArduinoRequest(byte command) {
            Command = command;
        }

        internal IList<byte> Bytes = new List<byte>();
        internal byte        Command { get; }

        public override string ToString() {
            return $"{GetType().FullName} ({BitConverter.ToString(Bytes.ToArray())})";
        }
    }
}