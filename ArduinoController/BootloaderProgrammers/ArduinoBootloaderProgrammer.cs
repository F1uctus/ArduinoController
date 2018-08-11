﻿using System;
using System.Threading;
using ArduinoController.BootloaderProgrammers.Protocols;
using ArduinoController.BootloaderProgrammers.ResetBehavior;
using ArduinoController.Hardware;
using RJCP.IO.Ports;

namespace ArduinoController.BootloaderProgrammers {
    internal abstract class ArduinoBootloaderProgrammer : BootloaderProgrammer {
        protected readonly SerialPortConfig SerialPortConfig;

        protected ArduinoBootloaderProgrammer(SerialPortConfig serialPortConfig, IMcu mcu)
            : base(mcu) {
            SerialPortConfig = serialPortConfig;
        }

        protected SerialPortStream SerialPort { get; set; }

        public override void Open() {
            string portName = SerialPortConfig.PortName;
            int    baudRate = SerialPortConfig.BaudRate;
            Logger?.Info($"Opening serial port {portName} - baudrate {baudRate}");

            SerialPort = new SerialPortStream(portName, baudRate) {
                ReadTimeout  = SerialPortConfig.ReadTimeOut,
                WriteTimeout = SerialPortConfig.WriteTimeOut
            };

            IResetBehavior preOpen = SerialPortConfig.PreOpenResetBehavior;
            if (preOpen != null) {
                Logger?.Info($"Executing Pre Open behavior ({preOpen})...");
                SerialPort = preOpen.Reset(SerialPort, SerialPortConfig);
            }

            try {
                if (!SerialPort.IsOpen) {
                    SerialPort.Open();
                }
            }
            catch (ObjectDisposedException ex) {
                throw new ArduinoUploaderException($"Unable to open serial port {portName} - {ex.Message}.");
            }
            catch (InvalidOperationException ex) {
                throw new ArduinoUploaderException($"Unable to open serial port {portName} - {ex.Message}.");
            }
            Logger?.Trace($"Opened serial port {portName} with baud rate {baudRate}!");

            IResetBehavior postOpen = SerialPortConfig.PostOpenResetBehavior;
            if (postOpen != null) {
                Logger?.Info($"Executing Post Open behavior ({postOpen})...");
                SerialPort = postOpen.Reset(SerialPort, SerialPortConfig);
            }

            int sleepAfterOpen = SerialPortConfig.SleepAfterOpen;
            if (SerialPortConfig.SleepAfterOpen <= 0) {
                return;
            }

            Logger?.Trace($"Sleeping for {sleepAfterOpen} ms after open...");
            Thread.Sleep(sleepAfterOpen);
        }

        public override void EstablishSync() {
            // Do nothing.
        }

        public override void Close() {
            IResetBehavior preClose = SerialPortConfig.CloseResetAction;
            if (preClose != null) {
                Logger?.Info("Resetting...");
                SerialPort = preClose.Reset(SerialPort, SerialPortConfig);
            }

            Logger?.Info("Closing serial port...");
            SerialPort.DtrEnable = false;
            SerialPort.RtsEnable = false;
            try {
                SerialPort.Close();
            }
            catch (Exception) {
                // Ignore
            }
        }

        protected virtual void Send(IRequest request) {
            byte[] bytes  = request.Bytes;
            int    length = bytes.Length;
            Logger?.Trace(
                $"Sending {length} bytes: {Environment.NewLine}"
              + $"{BitConverter.ToString(bytes)}"
            );
            SerialPort.Write(bytes, 0, length);
        }

        protected TResponse Receive<TResponse>(int length = 1)
            where TResponse : Response, new() {
            byte[] bytes = ReceiveNext(length);
            if (bytes == null) {
                return null;
            }
            return new TResponse { Bytes = bytes };
        }

        protected int ReceiveNext() {
            var bytes = new byte[1];
            try {
                SerialPort.Read(bytes, 0, 1);
                Logger?.Trace($"Receiving byte: {BitConverter.ToString(bytes)}");
                return bytes[0];
            }
            catch (TimeoutException) {
                return -1;
            }
        }

        protected byte[] ReceiveNext(int length) {
            var bytes     = new byte[length];
            var retrieved = 0;
            try {
                while (retrieved < length) {
                    retrieved += SerialPort.Read(bytes, retrieved, length - retrieved);
                }

                Logger?.Trace($"Receiving bytes: {BitConverter.ToString(bytes)}");
                return bytes;
            }
            catch (TimeoutException) {
                return null;
            }
        }
    }
}