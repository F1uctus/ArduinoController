﻿using System.Collections.Generic;
using ArduinoController.Hardware.Memory;

namespace ArduinoController.Hardware {
    internal interface IMcu {
        byte   DeviceCode      { get; }
        string DeviceSignature { get; }

        byte DeviceRevision { get; }
        byte ProgType       { get; }
        byte ParallelMode   { get; }
        byte Polling        { get; }
        byte SelfTimed      { get; }
        byte LockBytes      { get; }
        byte FuseBytes      { get; }

        byte Timeout     { get; }
        byte StabDelay   { get; }
        byte CmdExeDelay { get; }
        byte SynchLoops  { get; }
        byte ByteDelay   { get; }
        byte PollValue   { get; }
        byte PollIndex   { get; }

        IDictionary<Command, byte[]> CommandBytes { get; }

        IList<IMemory> Memory { get; }

        IMemory Flash  { get; }
        IMemory Eeprom { get; }
    }
}