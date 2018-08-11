using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ArduinoController.Hardware;
using ArduinoController.SerialProtocol;
using RJCP.IO.Ports;

namespace ArduinoController {
    /// <summary>
    ///     An ArduinoDriver can be used to communicate with an attached Arduino by way of sending requests and receiving
    ///     responses.
    ///     These messages are sent over a live serial connection (via a serial protocol) to the Arduino. The required listener
    ///     can be
    ///     automatically deployed to the Arduino.
    /// </summary>
    public class ArduinoDriver : IDisposable {

        private readonly IList<ArduinoModel> alwaysRedeployListeners =
            new List<ArduinoModel> { ArduinoModel.NanoR3 };

        private const int defaultRebootGraceTime = 2000;

        private readonly IDictionary<ArduinoModel, int> rebootGraceTimes =
            new Dictionary<ArduinoModel, int> {
                { ArduinoModel.Micro, 8000 },
                { ArduinoModel.Mega2560, 4000 }
            };

        private const int                        currentProtocolMajorVersion = 1;
        private const int                        currentProtocolMinorVersion = 2;
        private const int                        driverBaudRate              = 115200;
        private       ArduinoDriverSerialPort    port;
        private       ArduinoDriverConfiguration config;
        private       Func<SerialPortStream>     serialFunc;

        private const string arduinoListenerHexResourceFileName =
            "ArduinoController.ArduinoListener.ArduinoListener.ino.{0}.hex";

        /// <summary>
        ///     Creates a new ArduinoDriver instance. The relevant port name will be auto-detected if possible.
        /// </summary>
        /// <param name="arduinoModel"></param>
        /// <param name="autoBootstrap"></param>
        public ArduinoDriver(ArduinoModel arduinoModel, bool autoBootstrap = false) {
            IEnumerable<string> possiblePortNames   = SerialPortStream.GetPortNames().Distinct();
            string              unambiguousPortName = null;
            try {
                unambiguousPortName = possiblePortNames.SingleOrDefault();
            }
            catch (InvalidOperationException) {
                // More than one possible hit.
            }
            if (unambiguousPortName == null) {
                throw new IOException(
                    "Unable to autoconfigure ArduinoDriver port name, since there is not exactly a single "
                  + "COM port available. Please use the ArduinoDriver with the named port constructor!"
                );
            }

            Initialize(
                new ArduinoDriverConfiguration {
                    ArduinoModel  = arduinoModel,
                    PortName      = unambiguousPortName,
                    AutoBootstrap = autoBootstrap
                }
            );
        }

        /// <summary>
        ///     Creates a new ArduinoDriver instance for a specified portName.
        /// </summary>
        /// <param name="arduinoModel"></param>
        /// <param name="portName">The COM port name to create the ArduinoDriver instance for.</param>
        /// <param name="autoBootstrap">Determines if an listener is automatically deployed to the Arduino if required.</param>
        public ArduinoDriver(ArduinoModel arduinoModel, string portName, bool autoBootstrap = false) {
            Initialize(
                new ArduinoDriverConfiguration {
                    ArduinoModel  = arduinoModel,
                    PortName      = portName,
                    AutoBootstrap = autoBootstrap
                }
            );
        }

        /// <summary>
        ///     Sends a Analog Read Request to the Arduino.
        /// </summary>
        /// <param name="request">Analog Read Request</param>
        /// <returns>The Analog Read Response</returns>
        public AnalogReadResponse Send(AnalogReadRequest request) {
            return (AnalogReadResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a Analog Write Request to the Arduino.
        /// </summary>
        /// <param name="request">Analog Write Request</param>
        /// <returns>The Analog Write Response</returns>
        public AnalogWriteResponse Send(AnalogWriteRequest request) {
            return (AnalogWriteResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a Digital Read Request to the Arduino.
        /// </summary>
        /// <param name="request">Digital Read Request</param>
        /// <returns>The Digital Read Response</returns>
        public DigitalReadResponse Send(DigitalReadRequest request) {
            return (DigitalReadResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a Digital Write Request to the Arduino.
        /// </summary>
        /// <param name="request">Digital Write Request</param>
        /// <returns>The Digital Write Response</returns>
        public DigitalWriteReponse Send(DigitalWriteRequest request) {
            return (DigitalWriteReponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a PinMode Request to the Arduino.
        /// </summary>
        /// <param name="request">PinMode Request</param>
        /// <returns>The PinMode Response</returns>
        public PinModeResponse Send(PinModeRequest request) {
            return (PinModeResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a Tone Request to the Arduino.
        /// </summary>
        /// <param name="request">Tone Request</param>
        /// <returns>The Tone Response</returns>
        public ToneResponse Send(ToneRequest request) {
            return (ToneResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a NoTone Request to the Arduino.
        /// </summary>
        /// <param name="request">NoTone Request</param>
        /// <returns>The NoTone Response</returns>
        public NoToneResponse Send(NoToneRequest request) {
            return (NoToneResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a AnalogReference Request to the Arduino.
        /// </summary>
        /// <param name="request">AnalogReference Request</param>
        /// <returns>AnalogReference Response</returns>
        public AnalogReferenceResponse Send(AnalogReferenceRequest request) {
            return (AnalogReferenceResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a ShiftOut Request to the Arduino.
        /// </summary>
        /// <param name="request">ShiftOut Request</param>
        /// <returns>ShiftOut Response</returns>
        public ShiftOutResponse Send(ShiftOutRequest request) {
            return (ShiftOutResponse) InternalSend(request);
        }

        /// <summary>
        ///     Sends a ShiftIn Request to the Arduino;
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ShiftInResponse Send(ShiftInRequest request) {
            return (ShiftInResponse) InternalSend(request);
        }

        /// <summary>
        ///     Disposes the ArduinoDriver instance.
        /// </summary>
        public void Dispose() {
            try {
                port.Close();
                port.Dispose();
            }
            catch (Exception) {
                // Ignore
            }
        }

        #region Private Methods

        private void Initialize(ArduinoDriverConfiguration arduinoConfig) {
            config     = arduinoConfig;
            serialFunc = () => new SerialPortStream { PortName = config.PortName, BaudRate = driverBaudRate };

            if (!config.AutoBootstrap) {
                InitializeWithoutAutoBootstrap();
            }
            else {
                InitializeWithAutoBootstrap();
            }
        }

        private void InitializeWithoutAutoBootstrap() {
            // Without auto bootstrap, we just try to send a handshake request (the listener should already be
            // deployed). If that fails, we try nothing else.
            InitializePort();
            HandShakeResponse handshakeResponse = ExecuteHandshake();
            if (handshakeResponse == null) {
                port.Close();
                port.Dispose();
                throw new IOException(
                    $"Unable to get a handshake ACK when sending a handshake request to the Arduino on port {config.PortName}. " +
                    "Pass 'true' for optional parameter autoBootStrap in one of the ArduinoDriver constructors to " +
                    "automatically configure the Arduino (please note: this will overwrite the existing sketch " + "on the Arduino)."
                );
            }
        }

        private void InitializeWithAutoBootstrap() {
            bool              alwaysReDeployListener             = alwaysRedeployListeners.Count > 500;
            HandShakeResponse handshakeResponse;
            var               handShakeAckReceived               = false;
            var               handShakeIndicatesOutdatedProtocol = false;
            if (!alwaysReDeployListener) {
                InitializePort();
                handshakeResponse    = ExecuteHandshake();
                handShakeAckReceived = handshakeResponse != null;
                if (handShakeAckReceived) {
                    const int currentVersion  = currentProtocolMajorVersion * 10 + currentProtocolMinorVersion;
                    int       listenerVersion = handshakeResponse.ProtocolMajorVersion * 10 + handshakeResponse.ProtocolMinorVersion;
                    
                    handShakeIndicatesOutdatedProtocol = currentVersion > listenerVersion;
                    if (handShakeIndicatesOutdatedProtocol) {
                        port.Close();
                        port.Dispose();
                    }
                }
                else {
                    port.Close();
                    port.Dispose();
                }
            }

            // If we have received a handshake ack, and we have no need to upgrade, simply return.
            if (handShakeAckReceived && !handShakeIndicatesOutdatedProtocol) {
                return;
            }

            ArduinoModel arduinoModel = config.ArduinoModel;
            // At this point we will have to redeploy our listener
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            ExecuteAutoBootStrap(arduinoModel, config.PortName);
            stopwatch.Stop();

            // Now wait a bit, since the bootstrapped Arduino might still be restarting !
            int graceTime =
                rebootGraceTimes.ContainsKey(arduinoModel) ? rebootGraceTimes[arduinoModel] : defaultRebootGraceTime;
            Thread.Sleep(graceTime);

            // Listener should now (always) be deployed, handshake should yield success.
            InitializePort();
            handshakeResponse = ExecuteHandshake();
            if (handshakeResponse == null) {
                throw new IOException("Unable to get a handshake ACK after executing auto bootstrap on the Arduino!");
            }
        }

        private void InitializePort() {
            SerialPortStream serialEngine = serialFunc();
            serialEngine.WriteTimeout = 200;
            serialEngine.ReadTimeout  = 500;
            port                      = new ArduinoDriverSerialPort(serialEngine);
            port.Open();
        }

        private ArduinoResponse InternalSend(ArduinoRequest request) {
            return port.Send(request);
        }

        private HandShakeResponse ExecuteHandshake() {
            ArduinoResponse response = port.Send(new HandShakeRequest(), 1);
            return response as HandShakeResponse;
        }

        private static void ExecuteAutoBootStrap(ArduinoModel arduinoModel, string portName) {
            Assembly assembly   = Assembly.GetExecutingAssembly();
            Stream   textStream = assembly.GetManifestResourceStream(string.Format(arduinoListenerHexResourceFileName, arduinoModel));
            if (textStream == null) {
                throw new IOException("Unable to configure auto bootstrap, embedded resource missing!");
            }

            var hexFileContents = new List<string>();
            using (var reader = new StreamReader(textStream)) {
                while (reader.Peek() >= 0) {
                    hexFileContents.Add(reader.ReadLine());
                }
            }

            var uploader = new ArduinoSketchUploader(
                new ArduinoSketchUploaderOptions {
                    PortName     = portName,
                    ArduinoModel = arduinoModel
                }
            );
            uploader.UploadSketch(hexFileContents);
        }

        #endregion
    }
}