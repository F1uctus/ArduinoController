﻿namespace ArduinoController.BootloaderProgrammers.Protocols {
    public abstract class Request : IRequest {
        public byte[] Bytes { get; set; }
    }
}