using ArduinoController.Hardware.Memory;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v2.Messages {
    internal class LoadAddressRequest : Request {
        internal LoadAddressRequest(IMemory memory, int addr) {
            int modifier = memory.Type == MemoryType.Flash ? 0x80 : 0x00;
            Bytes = new[] {
                Constants.CmdLoadAddress,
                (byte) (((addr >> 24) & 0xff) | modifier),
                (byte) ((addr >> 16) & 0xff),
                (byte) ((addr >> 8) & 0xff),
                (byte) (addr & 0xff)
            };
        }
    }
}