using System;
using System.Linq;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v2.Messages {
    internal class ExecuteSpiCommandRequest : Request {
        internal ExecuteSpiCommandRequest(byte numTx, byte numRx, byte rxStartAddr, byte[] txData) {
            var data = new byte[numTx];
            Buffer.BlockCopy(txData, 0, data, 0, numTx);
            byte[] header = {
                Constants.CmdSpiMulti,
                numTx,
                numRx,
                rxStartAddr
            };
            Bytes = header.Concat(data).ToArray();
        }
    }
}