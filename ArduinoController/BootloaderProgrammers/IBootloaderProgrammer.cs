﻿using System;
using ArduinoController.Hardware.Memory;
using IntelHexFormatReader.Model;

namespace ArduinoController.BootloaderProgrammers {
    internal interface IBootloaderProgrammer {
        void   Open();
        void   Close();
        void   EstablishSync();
        void   CheckDeviceSignature();
        void   InitializeDevice();
        void   EnableProgrammingMode();
        void   LeaveProgrammingMode();
        void   ProgramDevice(MemoryBlock memoryBlock, IProgress<double> progress = null);
        void   LoadAddress(IMemory       memory,      int               offset);
        void   ExecuteWritePage(IMemory  memory,      int               offset, byte[] bytes);
        byte[] ExecuteReadPage(IMemory   memory);
    }
}