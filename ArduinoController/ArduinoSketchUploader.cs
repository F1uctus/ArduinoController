using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using ArduinoController.BootloaderProgrammers;
using ArduinoController.BootloaderProgrammers.Protocols.AVR109;
using ArduinoController.BootloaderProgrammers.Protocols.STK500v1;
using ArduinoController.BootloaderProgrammers.Protocols.STK500v2;
using ArduinoController.BootloaderProgrammers.ResetBehavior;
using ArduinoController.Config;
using ArduinoController.Hardware;
using IntelHexFormatReader;
using IntelHexFormatReader.Model;
using RJCP.IO.Ports;

namespace ArduinoController {
    public class ArduinoSketchUploader {
        internal static IArduinoUploaderLogger Logger { get; set; }

        private readonly ArduinoSketchUploaderOptions _options;
        private readonly IProgress<double>            _progress;

        public ArduinoSketchUploader(ArduinoSketchUploaderOptions options,
                                     IArduinoUploaderLogger       logger = null, IProgress<double> progress = null) {
            Logger = logger;
            Logger?.Info("Starting ArduinoSketchUploader...");
            _options  = options;
            _progress = progress;
        }

        public void UploadSketch() {
            string   hexFileName = _options.FileName;
            string[] hexFileContents;
            Logger?.Info($"Starting upload process for file '{hexFileName}'.");
            try {
                hexFileContents = File.ReadAllLines(hexFileName);
            }
            catch (Exception ex) {
                Logger?.Error(ex.Message, ex);
                throw;
            }
            UploadSketch(hexFileContents);
        }

        public void UploadSketch(IEnumerable<string> hexFileContents) {
            try {
                string       serialPortName = _options.PortName;
                string[]     allPortNames   = SerialPortStream.GetPortNames();
                List<string> distinctPorts  = allPortNames.Distinct().ToList();

                // If we don't specify a COM port, automagically select one if there is only a single match.
                if (string.IsNullOrWhiteSpace(serialPortName) && distinctPorts.SingleOrDefault() != null) {
                    Logger?.Info($"Port autoselected: {serialPortName}.");
                    serialPortName = distinctPorts.Single();
                }
                // Or else, check that we have an unambiguous match. Throw an exception otherwise.
                else if (!allPortNames.Any() ||
                         distinctPorts.SingleOrDefault(x => x.Equals(serialPortName, StringComparison.OrdinalIgnoreCase)) == null) {
                    throw new ArduinoUploaderException($"Specified COM port name '{serialPortName}' is not valid.");
                }

                Logger?.Trace($"Creating serial port '{serialPortName}'...");
                ArduinoBootloaderProgrammer programmer;
                IMcu                        mcu;

                string        model          = _options.ArduinoModel.ToString();
                Configuration hardwareConfig = ReadConfiguration();
                Arduino modelOptions =
                    hardwareConfig.Arduinos.SingleOrDefault(x => x.Model.Equals(model, StringComparison.OrdinalIgnoreCase));

                if (modelOptions == null) {
                    throw new ArduinoUploaderException($"Unable to find configuration for '{model}'!");
                }

                switch (modelOptions.Mcu) {
                    case McuIdentifier.AtMega1284:
                        mcu = new AtMega1284();
                        break;
                    case McuIdentifier.AtMega2560:
                        mcu = new AtMega2560();
                        break;
                    case McuIdentifier.AtMega32U4:
                        mcu = new AtMega32U4();
                        break;
                    case McuIdentifier.AtMega328P:
                        mcu = new AtMega328P();
                        break;
                    case McuIdentifier.AtMega168:
                        mcu = new AtMega168();
                        break;
                    default:
                        throw new ArduinoUploaderException($"Unrecognized MCU: '{modelOptions.Mcu}'!");
                }

                IResetBehavior preOpenResetBehavior  = ParseResetBehavior(modelOptions.PreOpenResetBehavior);
                IResetBehavior postOpenResetBehavior = ParseResetBehavior(modelOptions.PostOpenResetBehavior);
                IResetBehavior closeResetBehavior    = ParseResetBehavior(modelOptions.CloseResetBehavior);

                var serialPortConfig = new SerialPortConfig(
                    serialPortName,
                    modelOptions.BaudRate, preOpenResetBehavior, postOpenResetBehavior, closeResetBehavior,
                    modelOptions.SleepAfterOpen, modelOptions.ReadTimeout, modelOptions.WriteTimeout
                );

                switch (modelOptions.Protocol) {
                    case Protocol.Avr109:
                        programmer = new Avr109BootloaderProgrammer(serialPortConfig, mcu);
                        break;
                    case Protocol.Stk500v1:
                        programmer = new Stk500V1BootloaderProgrammer(serialPortConfig, mcu);
                        break;
                    case Protocol.Stk500v2:
                        programmer = new Stk500V2BootloaderProgrammer(serialPortConfig, mcu);
                        break;
                    default:
                        throw new ArduinoUploaderException($"Unrecognized protocol: '{modelOptions.Protocol}'!");
                }

                try {
                    Logger?.Info("Establishing memory block contents...");
                    MemoryBlock memoryBlockContents = ReadHexFile(hexFileContents, mcu.Flash.Size);

                    programmer.Open();

                    Logger?.Info("Establishing sync...");
                    programmer.EstablishSync();
                    Logger?.Info("Sync established.");

                    Logger?.Info("Checking device signature...");
                    programmer.CheckDeviceSignature();
                    Logger?.Info("Device signature checked.");

                    Logger?.Info("Initializing device...");
                    programmer.InitializeDevice();
                    Logger?.Info("Device initialized.");

                    Logger?.Info("Enabling programming mode on the device...");
                    programmer.EnableProgrammingMode();
                    Logger?.Info("Programming mode enabled.");

                    Logger?.Info("Programming device...");
                    programmer.ProgramDevice(memoryBlockContents, _progress);
                    Logger?.Info("Device programmed.");

                    Logger?.Info("Verifying program...");
                    programmer.VerifyProgram(memoryBlockContents, _progress);
                    Logger?.Info("Verified program!");

                    Logger?.Info("Leaving programming mode...");
                    programmer.LeaveProgrammingMode();
                    Logger?.Info("Left programming mode!");
                }
                finally {
                    programmer.Close();
                }
                Logger?.Info("All done, shutting down!");
            }
            catch (Exception ex) {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        #region Private Methods

        private static MemoryBlock ReadHexFile(IEnumerable<string> hexFileContents, int memorySize) {
            try {
                var reader = new HexFileReader(hexFileContents, memorySize);
                return reader.Parse();
            }
            catch (Exception ex) {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        private static Configuration ReadConfiguration() {
            Assembly      assembly     = Assembly.GetExecutingAssembly();
            const string  resourceName = "ArduinoController.ArduinoUploader.xml";
            Configuration hardwareConfig;
            var           deserializer = new XmlSerializer(typeof(Configuration));
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream == null) {
                    throw new ArduinoUploaderException($"Unable to extract embedded resource '{resourceName}'!");
                }

                try {
                    hardwareConfig = (Configuration) deserializer.Deserialize(stream);
                }
                catch (Exception ex) {
                    throw new ArduinoUploaderException($"Unable to deserialize configuration: '{ex.Message}'!{Environment.NewLine}{ex.StackTrace}");
                }
            }
            return hardwareConfig;
        }

        private static IResetBehavior ParseResetBehavior(string resetBehavior) {
            if (resetBehavior == null) {
                return null;
            }
            if (resetBehavior.Trim().Equals("1200bps", StringComparison.OrdinalIgnoreCase)) {
                return new ResetThrough1200BpsBehavior();
            }

            string[] parts         = resetBehavior.Split(';');
            int      numberOfParts = parts.Length;

            if (numberOfParts == 2 && parts[0].Trim().Equals("DTR", StringComparison.OrdinalIgnoreCase)) {
                bool flag = parts[1].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                return new ResetThroughTogglingDtrBehavior(flag);
            }

            if (numberOfParts < 3 || numberOfParts > 4) {
                throw new ArduinoUploaderException($"Unexpected format ({numberOfParts} parts to '{resetBehavior}')!");
            }

            // Only DTR-RTS supported at this point...
            string type = parts[0];
            if (!type.Equals("DTR-RTS", StringComparison.OrdinalIgnoreCase)) {
                throw new ArduinoUploaderException($"Unrecognized close reset behavior: '{resetBehavior}'!");
            }

            int wait1, wait2;
            try {
                wait1 = int.Parse(parts[1]);
            }
            catch (Exception) {
                throw new ArduinoUploaderException($"Unrecognized Wait (1) in DTR-RTS: '{parts[1]}'!");
            }

            try {
                wait2 = int.Parse(parts[2]);
            }
            catch (Exception) {
                throw new ArduinoUploaderException($"Unrecognized Wait (2) in DTR-RTS: '{parts[2]}'!");
            }

            bool inverted = numberOfParts == 4 && parts[3].Equals("true", StringComparison.OrdinalIgnoreCase);
            return new ResetThroughTogglingDtrRtsBehavior(wait1, wait2, inverted);
        }

        private static IResetBehavior ParseCloseResetBehavior(string closeResetBehavior) {
            if (closeResetBehavior == null) {
                return null;
            }
            string[] parts         = closeResetBehavior.Split(';');
            int      numberOfParts = parts.Length;
            if (numberOfParts < 3 || numberOfParts > 4) {
                throw new ArduinoUploaderException($"Unexpected format ({numberOfParts} parts to '{closeResetBehavior}')!");
            }

            // Only DTR-RTS supported at this point...
            string type = parts[0];
            if (!type.Equals("DTR-RTS", StringComparison.OrdinalIgnoreCase)) {
                throw new ArduinoUploaderException($"Unrecognized close reset behavior: '{closeResetBehavior}'!");
            }

            int wait1, wait2;
            try {
                wait1 = int.Parse(parts[1]);
            }
            catch (Exception) {
                throw new ArduinoUploaderException($"Unrecognized Wait (1) in DTR-RTS: '{parts[1]}'!");
            }

            try {
                wait2 = int.Parse(parts[2]);
            }
            catch (Exception) {
                throw new ArduinoUploaderException($"Unrecognized Wait (2) in DTR-RTS: '{parts[2]}'!");
            }

            bool inverted = numberOfParts == 4 && parts[3].Equals("true", StringComparison.OrdinalIgnoreCase);
            return new ResetThroughTogglingDtrRtsBehavior(wait1, wait2, inverted);
        }

        #endregion
    }
}