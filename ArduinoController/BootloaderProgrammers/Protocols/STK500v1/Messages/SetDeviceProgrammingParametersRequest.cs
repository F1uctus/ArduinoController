﻿using ArduinoController.Hardware;
using ArduinoController.Hardware.Memory;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v1.Messages {
    internal class SetDeviceProgrammingParametersRequest : Request {
        internal SetDeviceProgrammingParametersRequest(IMcu mcu) {
            IMemory flashMem      = mcu.Flash;
            IMemory eepromMem     = mcu.Eeprom;
            int     flashPageSize = flashMem.PageSize;
            int     flashSize     = flashMem.Size;
            int     epromSize     = eepromMem.Size;

            Bytes     = new byte[22];
            Bytes[0]  = Constants.CmdStkSetDevice;
            Bytes[1]  = mcu.DeviceCode;
            Bytes[2]  = mcu.DeviceRevision;
            Bytes[3]  = mcu.ProgType;
            Bytes[4]  = mcu.ParallelMode;
            Bytes[5]  = mcu.Polling;
            Bytes[6]  = mcu.SelfTimed;
            Bytes[7]  = mcu.LockBytes;
            Bytes[8]  = mcu.FuseBytes;
            Bytes[9]  = flashMem.PollVal1;
            Bytes[10] = flashMem.PollVal2;
            Bytes[11] = eepromMem.PollVal1;
            Bytes[12] = eepromMem.PollVal2;
            Bytes[13] = (byte) ((flashPageSize >> 8) & 0x00ff);
            Bytes[14] = (byte) (flashPageSize & 0x00ff);
            Bytes[15] = (byte) ((epromSize >> 8) & 0x00ff);
            Bytes[16] = (byte) (epromSize & 0x00ff);
            Bytes[17] = (byte) ((flashSize >> 24) & 0xff);
            Bytes[18] = (byte) ((flashSize >> 16) & 0xff);
            Bytes[19] = (byte) ((flashSize >> 8) & 0xff);
            Bytes[20] = (byte) (flashSize & 0xff);
            Bytes[21] = Constants.SyncCrcEop;
        }
    }
}