using ArduinoController.Hardware;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v2.Messages {
    internal class EnableProgrammingModeRequest : Request {
        internal EnableProgrammingModeRequest(IMcu mcu) {
            byte[] cmdBytes = mcu.CommandBytes[Command.PgmEnable];
            Bytes = new[] {
                Constants.CmdEnterProgrmodeIsp,
                mcu.Timeout,
                mcu.StabDelay,
                mcu.CmdExeDelay,
                mcu.SynchLoops,
                mcu.ByteDelay,
                mcu.PollValue,
                mcu.PollIndex,
                cmdBytes[0],
                cmdBytes[1],
                cmdBytes[2],
                cmdBytes[3]
            };
        }
    }
}