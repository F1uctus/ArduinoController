using System.Collections.Generic;
using System.Linq;
using ArduinoController.Hardware.Memory;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v2.Messages {
    internal class ExecuteProgramPageRequest : Request {
        internal ExecuteProgramPageRequest(byte writeCmd, IMemory memory, IReadOnlyCollection<byte> data) {
            int        len  = data.Count;
            const byte mode = 0xc1;
            byte[] headerBytes = {
                writeCmd,
                (byte) (len >> 8),
                (byte) (len & 0xff),
                mode,
                memory.Delay,
                memory.CmdBytesWrite[0],
                memory.CmdBytesWrite[1],
                memory.CmdBytesRead[0],
                memory.PollVal1,
                memory.PollVal2
            };
            Bytes = headerBytes.Concat(data).ToArray();
        }
    }
}