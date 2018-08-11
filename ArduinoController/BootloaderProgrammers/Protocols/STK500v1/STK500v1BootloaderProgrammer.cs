using System;
using System.Threading;
using ArduinoController.BootloaderProgrammers.Protocols.STK500v1.Messages;
using ArduinoController.Hardware;
using ArduinoController.Hardware.Memory;

namespace ArduinoController.BootloaderProgrammers.Protocols.STK500v1 {
    internal class Stk500V1BootloaderProgrammer : ArduinoBootloaderProgrammer {
        internal Stk500V1BootloaderProgrammer(SerialPortConfig serialPortConfig, IMcu mcu)
            : base(serialPortConfig, mcu) {
        }

        public override void EstablishSync() {
            const int maxRetries   = 256;
            var       retryCounter = 0;
            while (retryCounter++ < maxRetries) {
                SerialPort.DiscardInBuffer();
                Send(new GetSyncRequest());
                var result = Receive<GetSyncResponse>();
                if (result == null) {
                    continue;
                }
                if (result.IsInSync) {
                    break;
                }
                Thread.Sleep(20);
            }
            if (retryCounter == maxRetries) {
                throw new ArduinoUploaderException($"Unable to establish sync after {maxRetries} retries.");
            }

            retryCounter = 0;
            while (retryCounter++ < maxRetries) {
                int nextByte = ReceiveNext();
                if (nextByte == Constants.RespStkOk) {
                    break;
                }
            }
            if (retryCounter == maxRetries) {
                throw new ArduinoUploaderException("Unable to establish sync.");
            }
        }

        protected void SendWithSyncRetry(IRequest request) {
            byte nextByte;
            while (true) {
                Send(request);
                nextByte = (byte) ReceiveNext();
                if (nextByte == Constants.RespStkNosync) {
                    EstablishSync();
                    continue;
                }
                break;
            }
            if (nextByte != Constants.RespStkInsync) {
                throw new ArduinoUploaderException($"Unable to aqcuire sync in SendWithSyncRetry for request of type {request.GetType()}!");
            }
        }

        public override void CheckDeviceSignature() {
            Logger?.Debug($"Expecting to find '{Mcu.DeviceSignature}'...");
            SendWithSyncRetry(new ReadSignatureRequest());
            var response = Receive<ReadSignatureResponse>(4);
            if (response == null || !response.IsCorrectResponse) {
                throw new ArduinoUploaderException("Unable to check device signature!");
            }

            byte[] signature = response.Signature;
            if (BitConverter.ToString(signature) != Mcu.DeviceSignature) {
                throw new ArduinoUploaderException(
                    $"Unexpected device signature - found '{BitConverter.ToString(signature)}'- expected '{Mcu.DeviceSignature}'."
                );
            }
        }

        public override void InitializeDevice() {
            uint majorVersion = GetParameterValue(Constants.ParmStkSwMajor);
            uint minorVersion = GetParameterValue(Constants.ParmStkSwMinor);
            Logger?.Info($"Retrieved software version: {majorVersion}.{minorVersion}.");

            Logger?.Info("Setting device programming parameters...");
            SendWithSyncRetry(new SetDeviceProgrammingParametersRequest((Mcu) Mcu));
            int nextByte = ReceiveNext();

            if (nextByte != Constants.RespStkOk) {
                throw new ArduinoUploaderException("Unable to set device programming parameters!");
            }
        }

        public override void EnableProgrammingMode() {
            SendWithSyncRetry(new EnableProgrammingModeRequest());
            int nextByte = ReceiveNext();
            if (nextByte == Constants.RespStkOk) {
                return;
            }
            if (nextByte == Constants.RespStkNodevice || nextByte == Constants.RespStkFailed) {
                throw new ArduinoUploaderException("Unable to enable programming mode on the device!");
            }
        }

        public override void LeaveProgrammingMode() {
            SendWithSyncRetry(new LeaveProgrammingModeRequest());
            int nextByte = ReceiveNext();
            if (nextByte == Constants.RespStkOk) {
                return;
            }
            if (nextByte == Constants.RespStkNodevice || nextByte == Constants.RespStkFailed) {
                throw new ArduinoUploaderException("Unable to leave programming mode on the device!");
            }
        }

        private uint GetParameterValue(byte param) {
            Logger?.Trace($"Retrieving parameter '{param}'...");
            SendWithSyncRetry(new GetParameterRequest(param));
            int nextByte   = ReceiveNext();
            var paramValue = (uint) nextByte;
            nextByte = ReceiveNext();

            if (nextByte == Constants.RespStkFailed) {
                throw new ArduinoUploaderException($"Retrieving parameter '{param}' failed!");
            }

            if (nextByte != Constants.RespStkOk) {
                throw new ArduinoUploaderException($"General protocol error while retrieving parameter '{param}'.");
            }

            return paramValue;
        }

        public override void ExecuteWritePage(IMemory memory, int offset, byte[] bytes) {
            SendWithSyncRetry(new ExecuteProgramPageRequest(memory, bytes));
            int nextByte = ReceiveNext();
            if (nextByte == Constants.RespStkOk) {
                return;
            }
            throw new ArduinoUploaderException($"Write at offset {offset} failed!");
        }

        public override byte[] ExecuteReadPage(IMemory memory) {
            int pageSize = memory.PageSize;
            SendWithSyncRetry(new ExecuteReadPageRequest(memory.Type, pageSize));
            byte[] bytes = ReceiveNext(pageSize);
            if (bytes == null) {
                throw new ArduinoUploaderException("Execute read page failed!");
            }

            int nextByte = ReceiveNext();
            if (nextByte == Constants.RespStkOk) {
                return bytes;
            }
            throw new ArduinoUploaderException("Execute read page failed!");
        }

        public override void LoadAddress(IMemory memory, int addr) {
            Logger?.Trace($"Sending load address request: {addr}.");
            addr = addr >> 1;
            SendWithSyncRetry(new LoadAddressRequest(addr));
            int result = ReceiveNext();
            if (result == Constants.RespStkOk) {
                return;
            }
            throw new ArduinoUploaderException($"LoadAddress failed with result {result}!");
        }
    }
}