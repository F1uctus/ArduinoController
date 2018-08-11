using ArduinoController.Hardware.Memory;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v2.Messages {
    internal class ExecuteReadPageRequest : Request {
        internal ExecuteReadPageRequest(byte readCmd, IMemory memory) {
            int  pageSize = memory.PageSize;
            byte cmdByte  = memory.CmdBytesRead[0];
            Bytes = new[] {
                readCmd,
                (byte) (pageSize >> 8),
                (byte) (pageSize & 0xff),
                cmdByte
            };
        }
    }
}