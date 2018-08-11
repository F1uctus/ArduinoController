namespace ArduinoController.BootloaderProgrammers.Protocols {
    internal interface IMessage {
        byte[] Bytes { get; set; }
    }
}