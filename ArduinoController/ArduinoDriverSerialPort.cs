﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArduinoController.SerialProtocol;
using RJCP.IO.Ports;

namespace ArduinoController {
    internal class ArduinoDriverSerialPort {
        private readonly        SerialPortStream serialPort;
        private const           int              maxSendRetries = 6;
        private const           int              maxSyncRetries = 3;

        internal ArduinoDriverSerialPort(SerialPortStream serialPortEngine) {
            serialPort = serialPortEngine;
        }

        public void Open() {
            serialPort.Open();
        }

        public void Close() {
            serialPort.Close();
        }

        public void Dispose() {
            serialPort.Dispose();
        }

        private bool GetSync() {
            try {
                byte[] bytes = CommandConstants.SyncBytes;
                serialPort.Write(bytes, 0, bytes.Length);
                byte[] responseBytes = ReadCurrentReceiveBuffer(4);
                return responseBytes.Length == 4
                    && responseBytes[0] == bytes[3]
                    && responseBytes[1] == bytes[2]
                    && responseBytes[2] == bytes[1]
                    && responseBytes[3] == bytes[0];
            }
            catch (Exception) {
                return false;
            }
        }

        private bool ExecuteCommandHandShake(byte command, byte length) {
            var bytes = new byte[] { 0xfb, command, length };
            serialPort.Write(bytes, 0, bytes.Length);
            byte[] responseBytes = ReadCurrentReceiveBuffer(3);
            return responseBytes.Length == 3
                && responseBytes[0] == bytes[2]
                && responseBytes[1] == bytes[1]
                && responseBytes[2] == bytes[0];
        }

        internal ArduinoResponse Send(ArduinoRequest request, int maximumSendRetries = maxSendRetries) {
            var sendRetries = 0;
            while (sendRetries++ < maximumSendRetries) {
                try {
                    // First try to get sync (send FF FE FD FC and require FC FD FE FF as a response).
                    bool hasSync;
                    var  syncRetries = 0;
                    while (!(hasSync = GetSync()) && syncRetries++ < maxSyncRetries) {
                    }
                    if (!hasSync) {
                        string errorMessage = $"Unable to get sync after {maxSyncRetries} tries!";
                        throw new IOException(errorMessage);
                    }

                    // Now send the command handshake (send FB as start of message marker + the command byte + length byte).
                    // Expect the inverse (length byte followed by command byte followed by FB) as a command ACK.
                    byte[] requestBytes       = request.Bytes.ToArray();
                    int    requestBytesLength = requestBytes.Length;

                    if (!ExecuteCommandHandShake(request.Command, (byte) requestBytesLength)) {
                        string errorMessage = $"Unable to configure command handshake for command {request}.";
                        throw new IOException(errorMessage);
                    }

                    // Write out all packet bytes, followed by a Fletcher 16 checksum!
                    // Packet bytes consist of:
                    // 1. Command byte repeated
                    // 2. Request length repeated
                    // 3. The actual request bytes
                    // 4. Two fletcher-16 checksum bytes calculated over (1 + 2 + 3)
                    var packetBytes = new byte[requestBytesLength + 4];
                    packetBytes[0] = request.Command;
                    packetBytes[1] = (byte) requestBytesLength;
                    Buffer.BlockCopy(requestBytes, 0, packetBytes, 2, requestBytesLength);
                    ushort fletcher16CheckSum = CalculateFletcher16Checksum(packetBytes, requestBytesLength + 2);
                    var    f0                 = (byte) (fletcher16CheckSum & 0xff);
                    var    f1                 = (byte) ((fletcher16CheckSum >> 8) & 0xff);
                    var    c0                 = (byte) (0xff - (f0 + f1) % 0xff);
                    var    c1                 = (byte) (0xff - (f0 + c0) % 0xff);
                    packetBytes[requestBytesLength + 2] = c0;
                    packetBytes[requestBytesLength + 3] = c1;

                    serialPort.Write(packetBytes, 0, requestBytesLength + 4);

                    // Write out all bytes written marker (FA)
                    serialPort.Write(new[] { CommandConstants.AllBytesWritten }, 0, 1);

                    // Expect response message to drop to be received in the following form:
                    // F9 (start of response marker) followed by response length
                    byte[] responseBytes         = ReadCurrentReceiveBuffer(2);
                    byte   startOfResponseMarker = responseBytes[0];
                    byte   responseLength        = responseBytes[1];
                    if (startOfResponseMarker != CommandConstants.StartOfResponseMarker) {
                        string errorMessage = $"Did not receive start of response marker but {startOfResponseMarker}!";
                        throw new IOException(errorMessage);
                    }

                    // Read x responsebytes
                    responseBytes = ReadCurrentReceiveBuffer(responseLength);
                    return ArduinoResponse.Create(responseBytes);
                }
                catch (Exception) {
                    // ignored
                }
            }
            return null;
        }

        private static ushort CalculateFletcher16Checksum(IReadOnlyList<byte> checkSumBytes, int count) {
            ushort sum1 = 0;
            ushort sum2 = 0;
            for (var index = 0; index < count; ++index) {
                sum1 = (ushort) ((sum1 + checkSumBytes[index]) % 255);
                sum2 = (ushort) ((sum2 + sum1) % 255);
            }
            return (ushort) ((sum2 << 8) | sum1);
        }

        private byte[] ReadCurrentReceiveBuffer(int numberOfBytes) {
            var result     = new byte[numberOfBytes];
            var retrieved  = 0;
            var retryCount = 0;

            while (retrieved < numberOfBytes && retryCount++ < 4) {
                retrieved += serialPort.Read(result, retrieved, numberOfBytes - retrieved);
            }

            return result;
        }
    }
}