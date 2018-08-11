using RJCP.IO.Ports;

namespace ArduinoController.BootloaderProgrammers.ResetBehavior {
    internal interface IResetBehavior {
        SerialPortStream Reset(SerialPortStream serialPort, SerialPortConfig config);
    }
}