using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace GlassServer
{


    /// <summary>
    /// Provides managed access to SimConnect values. 
    /// The intent is to have this manager keep a cache of requested values, adding more values to the polling loop as they're requested.
    /// </summary>
    class SimDataManager
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private static SimConnect m_oSimConnect = null;

        private static bool m_bConnected = false;
        private static Task m_tConnectTask;

        private static EventWaitHandle m_eMessagePump;
        private static Thread m_tMessageThread;
        private static Thread m_tRequestThread;

        private static uint m_iCurrentDefinition = 0;
        private static uint m_iCurrentRequest = 0;

        public static Dictionary<string, SimDataDef> definitions = new Dictionary<string, SimDataDef>();

        static void PopulateDefinitions()
        {
            Console.WriteLine("Populating definitions...");

            definitions.Clear();

            // https://docs.microsoft.com/en-us/previous-versions/microsoft-esp/cc526981(v=msdn.10)

            var b = new SimDataDefBuilder();

            // Booleans
            b = b.Units("boolean");
            Add(b.Name("LIGHT STROBE"));
            Add(b.Name("LIGHT PANEL"));
            Add(b.Name("LIGHT LANDING"));
            Add(b.Name("LIGHT TAXI"));
            Add(b.Name("LIGHT BEACON"));
            Add(b.Name("LIGHT NAV"));
            Add(b.Name("LIGHT LOGO"));
            Add(b.Name("LIGHT WING"));
            Add(b.Name("LIGHT CABIN"));

            Add(b.Name("AUTOPILOT AVAILABLE"));
            Add(b.Name("AUTOPILOT MASTER"));
            Add(b.Name("AUTOPILOT NAV1 LOCK"));
            Add(b.Name("AUTOPILOT HEADING LOCK"));
            Add(b.Name("AUTOPILOT ALTITUDE LOCK"));
            Add(b.Name("AUTOPILOT ATTITUDE HOLD"));
            Add(b.Name("AUTOPILOT VERTICAL HOLD"));
            Add(b.Name("AUTOPILOT FLIGHT DIRECTOR ACTIVE"));

            Add(b.Name("IS GEAR RETRACTABLE"));
            Add(b.Name("GEAR HANDLE POSITION"));
            Add(b.Name("BRAKE PARKING POSITION").ReadOnly()); // Actually wants to be a position 32k but bool is easier to understand.

            Add(b.Name("GENERAL ENG COMBUSTION:1"));
            Add(b.Name("GENERAL ENG COMBUSTION:2"));
            Add(b.Name("GENERAL ENG COMBUSTION:3"));
            Add(b.Name("GENERAL ENG COMBUSTION:4"));

            Add(b.Name("GENERAL ENG MASTER ALTERNATOR:1"));
            Add(b.Name("GENERAL ENG MASTER ALTERNATOR:2"));
            Add(b.Name("GENERAL ENG MASTER ALTERNATOR:3"));
            Add(b.Name("GENERAL ENG MASTER ALTERNATOR:4"));

            Add(b.Name("COM TRANSMIT:1"));
            Add(b.Name("COM TRANSMIT:2"));
            Add(b.Name("COM RECIEVE ALL"));

            Add(b.Name("NAV AVAILABLE:1"));
            Add(b.Name("NAV AVAILABLE:2"));
            Add(b.Name("NAV HAS NAV:1"));
            Add(b.Name("NAV HAS NAV:2"));

            Add(b.Name("SIM ON GROUND").ReadOnly());


            // Knots
            b = b.Units("boolean");
            Add(b.Name("AMBIENT WIND VELOCITY").ReadOnly());

            // feet/sec
            b = b.Units("feet per second");
            Add(b.Name("VERTICAL SPEED").ReadOnly());

            // feet per minute
            b = b.Units("feet per minute");
            Add(b.Name("AUTOPILOT VERTICAL HOLD VAR").ReadOnly());

            // feet
            b = b.Units("feet");
            Add(b.Name("RADIO HEIGHT").ReadOnly());
            Add(b.Name("AUTOPILOT ALTITUDE LOCK VAR"));

            // RPM
            b = b.Units("rpm");
            Add(b.Name("GENERAL ENG RPM:1"));
            Add(b.Name("GENERAL ENG RPM:2"));
            Add(b.Name("GENERAL ENG RPM:3"));
            Add(b.Name("GENERAL ENG RPM:4"));

            // Frequency BCD16
            b = b.Units("frequency bcd16");
            Add(b.Name("COM ACTIVE FREQUENCY:1"));
            Add(b.Name("COM ACTIVE FREQUENCY:2"));
            Add(b.Name("COM STANDBY FREQUENCY:1"));
            Add(b.Name("COM STANDBY FREQUENCY:2"));

            // MHz
            b = b.Units("MHz");
            Add(b.Name("NAV ACTIVE FREQUENCY:1"));
            Add(b.Name("NAV ACTIVE FREQUENCY:2"));
            Add(b.Name("NAV STANDBY FREQUENCY:1"));
            Add(b.Name("NAV STANDBY FREQUENCY:2"));

            // Degrees
            b = b.Units("degrees");
            Add(b.Name("PLANE LATITUDE").ReadOnly());
            Add(b.Name("PLANE LONGITUDE").ReadOnly());

            Add(b.Name("AMBIENT WIND DIRECTION").ReadOnly());
            Add(b.Name("AUTOPILOT HEADING LOCK DIR").ReadOnly());

            Add(b.Name("PLANE HEADING DEGREES MAGNETIC").ReadOnly());
            Add(b.Name("PLANE HEADING DEGREES TRUE").ReadOnly());

            Add(b.Name("NAV RADIAL:1"));
            Add(b.Name("NAV RADIAL:2"));
            Add(b.Name("NAV RADIAL ERROR:1"));
            Add(b.Name("NAV RADIAL ERROR:2"));


            // Temperature
            b = b.Units("celsius");
            Add(b.Name("AMBIENT TEMPERATURE").ReadOnly());

            // Number/Count
            b = b.Units("number");
            Add(b.Name("NUMBER OF ENGINES").ReadOnly());
            Add(b.Name("AUTOPILOT NAV SELECTED"));
            Add(b.Name("NAV SIGNAL:1"));
            Add(b.Name("NAV SIGNAL:2"));
            Add(b.Name("NAV CDI:1"));
            Add(b.Name("NAV CDI:2"));

            // Enum
            b = b.Units("Enum");
            Add(b.Name("ENGINE TYPE").ReadOnly().Enum((0, "Piston"), (1, "Jet"), (2, "None")));
            Add(b.Name("SURFACE TYPE").ReadOnly().Enum(
                (0, "Concrete"),
                (1, "Grass"),
                (2, "Water"),
                (3, "Grass Bumpy"),
                (4, "Asphalt"),
                (5, "Short Grass"),
                (6, "Long Grass"),
                (7, "Hard Turf"),
                (8, "Snow"),
                (9, "Ice"),
                (10, "Urban"),
                (11, "Forest"),
                (12, "Dirt"),
                (13, "Coral"),
                (14, "Gravel"),
                (15, "Oil Treated"),
                (16, "Steel Mats"),
                (17, "Bituminus"),
                (18, "Brick"),
                (19, "Macadam"),
                (20, "Planks"),
                (21, "Sand"),
                (22, "Shale"),
                (23, "Tarmac"),
                (24, "Wright Flyer Track")
            ));

            Add(b.Name("COM STATUS:1").ReadOnly());
            Add(b.Name("COM STATUS:2").ReadOnly());

            //Nav TO/ FROM flag:
            //0 = Off
            //1 = TO
            //2 = FROM
            Add(b.Name("NAV TOFROM:1"));
            Add(b.Name("NAV TOFROM:2"));


            Console.WriteLine("Populated definitions!");

            static void Add(SimDataDefBuilder _builder)
            {
                var def = _builder.Build();
                definitions.Add(def.name, def);
            }

        }

        public static async Task Connect()
        {
            if (m_tConnectTask != null)
            {
                await m_tConnectTask;
                return;
            }

            m_tConnectTask = BaseConnect();

            await m_tConnectTask;
        }

        private static async Task BaseConnect()
        {
            // Exit if already connected
            if (m_oSimConnect != null && m_bConnected) return;

            Console.WriteLine("Connecting...");
            m_eMessagePump = new EventWaitHandle(false, EventResetMode.AutoReset);
            m_oSimConnect = new SimConnect("Glass Server", IntPtr.Zero, WM_USER_SIMCONNECT, m_eMessagePump, 0);

            m_oSimConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
            m_oSimConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
            m_oSimConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);
            m_oSimConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataByType);

            // TODO: A new thread might not be the modern way to do this.
            m_tMessageThread = new Thread(new ThreadStart(MessageProcessor));
            m_tMessageThread.IsBackground = true;
            m_tMessageThread.Start();

            // TODO: A new thread might not be the modern way to do this.
            m_tRequestThread = new Thread(new ThreadStart(RequestProcessor));
            m_tRequestThread.IsBackground = true;
            m_tRequestThread.Start();

            while (!m_bConnected)
            {
                Console.WriteLine("Waiting to connect...");
                await Task.Delay(1000);
            }

            PopulateDefinitions();

            foreach (var def in definitions.Values)
            {
                RegisterDefinition(def);
            }

        }

        public static void Disconnect()
        {
            if (m_oSimConnect != null)
            {
                /// Dispose serves the same purpose as SimConnect_Close()
                m_oSimConnect.Dispose();
                m_oSimConnect = null;
                m_bConnected = false;
                m_tRequestThread.Abort();
                m_tMessageThread.Abort();
            }
        }


        /// <summary>
        /// Request a SimData from the manager.
        /// If there is no existing request, create one and wait for the result.
        /// </summary>
        /// <param name="_sName"></param>
        /// <returns></returns>
        public static SimDataDef GetDefinition(string _sName)
        {
            // Try once
            SimDataDef def;
            if (definitions.TryGetValue(_sName, out def))
            {
                if (!def.registered) RegisterDefinition(def);

                return def;
            }
            else
            {
                throw new HttpResponseException(404, string.Format("{0} is not a defined simvar.", _sName));
            }
        }

        public static void RequestDataSet(string _sName, double _dValue)
        {
            SimDataDef def;
            if (definitions.TryGetValue(_sName, out def))
            {
                if (!def.registered)
                {
                    throw new HttpResponseException(403, string.Format("{0} is not registered yet.", _sName));
                }

                if (def.readOnly)
                {
                    throw new HttpResponseException(403, string.Format("{0} is read-only and cannot be set.", _sName));
                }
                m_oSimConnect.SetDataOnSimObject(def.eDef, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, _dValue);
            }
            else
            {
                throw new HttpResponseException(404, string.Format("{0} is not a defined simvar.", _sName));
            }
        }

        /// <summary>
        /// Add a new SimData request to the polling.
        /// </summary>
        /// <param name="_sName"></param>
        /// <param name="_sUnits"></param>
        private static void RegisterDefinition(SimDataDef def)
        {
            if (m_oSimConnect == null || !m_bConnected)
            {
                throw new HttpResponseException(503, "SimConnect service is not connected yet");
            }

            if (def.registered) return;
            def.eDef = (DEFINITION)m_iCurrentDefinition;
            def.eRequest = (REQUEST)m_iCurrentRequest;

            try
            {
                m_oSimConnect.AddToDataDefinition(def.eDef, def.name, def.units, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                m_oSimConnect.RegisterDataDefineStruct<double>(def.eRequest);
                def.registered = true;
            }
            catch
            {
                Console.WriteLine("Failed to register \"{0}\" simvar definition.", def.name);
            }

            m_iCurrentDefinition++;
            m_iCurrentRequest++;
        }



        /// <summary>
        /// Re-request the data to refresh the cache.
        /// </summary>
        private static void RequestProcessor()
        {
            while (!m_bConnected) Thread.Sleep(1000);

            while (true)
            {
                var defs = definitions.Values.Where(def => def.registered && !def.pending);
                foreach (var def in defs)
                {
                    try
                    {
                        m_oSimConnect?.RequestDataOnSimObjectType(def.eRequest, def.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                        def.pending = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error requesting sim object type {0} : " + e.ToString(), def.name);
                    }
                }

                // TODO: Create seperate data priorities so some less active values (such as NUM OF ENGINES) is requested less often.
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Our handle to SimConnect will alert us when there is a message to receive.
        /// </summary>
        private static void MessageProcessor()
        {
            while (true)
            {
                m_eMessagePump.WaitOne();
                if (m_oSimConnect != null)
                {
                    try
                    {
                        m_oSimConnect.ReceiveMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error Processing Message" + ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }


        private static void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("Connected to SimConnect!");
            m_bConnected = true;
        }

        private static void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("Quitting SimConnect...");
            Disconnect();
        }

        private static void SimConnect_OnRecvSimobjectDataByType(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            var iRequest = data.dwRequestID;

            foreach (var entry in definitions)
            {
                var oSimDataRequest = entry.Value;

                if (iRequest == (uint)oSimDataRequest.eRequest)
                {
                    double dValue = (double)data.dwData[0];
                    oSimDataRequest.SetValue(dValue);
                    oSimDataRequest.pending = false;
                }
            }
        }

        private static void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
            Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());
        }
    }
}
