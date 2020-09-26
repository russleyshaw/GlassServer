using System;
using System.Collections.Generic;
using System.Linq;
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

        public  static Dictionary<string, SimDataDef> definitions = new Dictionary<string, SimDataDef>();

        static void PopulateDefinitions()
        {
            Console.WriteLine("Populating definitions...");

            definitions.Clear();

            // https://docs.microsoft.com/en-us/previous-versions/microsoft-esp/cc526981(v=msdn.10)

            // Booleans
            var units = "bool";
            AddDef("LIGHT STROBE", units);
            AddDef("LIGHT PANEL", units);
            AddDef("LIGHT LANDING", units);
            AddDef("LIGHT TAXI", units);
            AddDef("LIGHT BEACON", units);
            AddDef("LIGHT NAV", units);
            AddDef("LIGHT LOGO", units);
            AddDef("LIGHT WING", units);
            AddDef("LIGHT CABIN", units);

            AddDef("AUTOPILOT AVAILABLE", units);
            AddDef("AUTOPILOT MASTER", units);
            AddDef("AUTOPILOT NAV1 LOCK", units);
            AddDef("AUTOPILOT HEADING LOCK", units);
            AddDef("AUTOPILOT ALTITUDE LOCK", units);
            AddDef("AUTOPILOT ATTITUDE HOLD", units);
            AddDef("AUTOPILOT VERTICAL HOLD", units);

            AddDef("IS GEAR RETRACTABLE", units);
            AddDef("GEAR HANDLE POSITION", units);

            AddDef("GENERAL ENG COMBUSTION:1", units);
            AddDef("GENERAL ENG COMBUSTION:2", units);
            AddDef("GENERAL ENG COMBUSTION:3", units);
            AddDef("GENERAL ENG COMBUSTION:4", units);

            AddDef("GENERAL ENG MASTER ALTERNATOR:1", units);
            AddDef("GENERAL ENG MASTER ALTERNATOR:2", units);
            AddDef("GENERAL ENG MASTER ALTERNATOR:3", units);
            AddDef("GENERAL ENG MASTER ALTERNATOR:4", units);

            AddDef("COM TRANSMIT:1", units);
            AddDef("COM TRANSMIT:2", units);
            AddDef("COM RECIEVE ALL", units);

            AddDef("NAV AVAILABLE:1", units);
            AddDef("NAV AVAILABLE:2", units);
            AddDef("NAV HAS NAV:1", units);
            AddDef("NAV HAS NAV:2", units);
            

            // Knots
            units = "knots";
            AddDef("AMBIENT WIND VELOCITY", units);

            // feet/sec
            units = "feet per second";
            AddDef("VERTICAL SPEED", units);
            AddDef("AUTOPILOT VERTICAL HOLD VAR", units);

            // feet
            units = "feet";
            AddDef("RADIO HEIGHT", units);
            AddDef("AUTOPILOT ALTITUDE LOCK VAR", units);

            // RPM
            units = "rpm";
            AddDef("GENERAL ENG RPM:1", units);
            AddDef("GENERAL ENG RPM:2", units);
            AddDef("GENERAL ENG RPM:3", units);
            AddDef("GENERAL ENG RPM:4", units);

            // Frequency BCD16
            units = "frequency bdc16";
            AddDef("COM ACTIVE FREQUENCY:1", units);
            AddDef("COM ACTIVE FREQUENCY:2", units);
            AddDef("COM STANDBY FREQUENCY:1", units);
            AddDef("COM STANDBY FREQUENCY:2", units);

            // MHz
            units = "MHz";
            AddDef("NAV ACTIVE FREQUENCY:1", units);
            AddDef("NAV ACTIVE FREQUENCY:2", units);
            AddDef("NAV STANDBY FREQUENCY:1", units);
            AddDef("NAV STANDBY FREQUENCY:2", units);

            // Degrees
            units = "degrees";
            AddDef("PLANE LATITUDE", units);
            AddDef("PLANE LONGITUDE", units);

            AddDef("AMBIENT WIND DIRECTION", units);
            AddDef("AUTOPILOT HEADING LOCK DIR", units);

            AddDef("PLANE HEADING DEGREES MAGNETIC", units);
            AddDef("PLANE HEADING DEGREES TRUE", units);

            AddDef("NAV RADIAL:1", units);
            AddDef("NAV RADIAL:2", units);
            AddDef("NAV RADIAL ERROR:1", units);
            AddDef("NAV RADIAL ERROR:2", units);


            // Temperature
            units = "celsius";
            AddDef("AMBIENT TEMPERATURE", units);

            // Number/Count
            units = "number";
            AddDef("NUMBER OF ENGINES", units);
            AddDef("AUTOPILOT NAV SELECTED", units);
            AddDef("NAV SIGNAL:1", units);
            AddDef("NAV SIGNAL:2", units);
            AddDef("NAV CDI:1", units);
            AddDef("NAV CDI:2", units);

            // Enum
            units = "Enum";
            AddDef("ENGINE TYPE", units);
            AddDef("SURFACE TYPE", units);
            AddDef("COM STATUS:1", units);
            AddDef("COM STATUS:2", units);

            //Nav TO/ FROM flag:
            //0 = Off
            //1 = TO
            //2 = FROM
            AddDef("NAV TOFROM:1", units);
            AddDef("NAV TOFROM:2", units);


            Console.WriteLine("Populated definitions!");

            static void AddDef(string _name, string _units)
            {
                var def = new SimDataDef(_name, _units);
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

            while(!m_bConnected)
            {
                Console.WriteLine("Waiting to connect...");
                await Task.Delay(1000);
            }

            PopulateDefinitions();

            foreach(var def in definitions.Values)
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

            // Not found
            return null;
        }

        public static void RequestDataSet(string _sName, double _dValue)
        {
            SimDataDef def;
            if (definitions.TryGetValue(_sName, out def) && def.registered)
            {
                m_oSimConnect.SetDataOnSimObject(def.eDef, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, _dValue);
            }
        }

        /// <summary>
        /// Add a new SimData request to the polling.
        /// </summary>
        /// <param name="_sName"></param>
        /// <param name="_sUnits"></param>
        private static void RegisterDefinition(SimDataDef def)
        {
            if (def.registered) return;
            def.eDef = (DEFINITION)m_iCurrentDefinition;
            def.eRequest = (REQUEST)m_iCurrentRequest;

            m_oSimConnect.AddToDataDefinition(def.eDef, def.name, def.units, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            m_oSimConnect.RegisterDataDefineStruct<double>(def.eRequest);
            def.registered = true;

            m_iCurrentDefinition++;
            m_iCurrentRequest++;
        }



        /// <summary>
        /// Re-request the data to refresh the cache.
        /// </summary>
        private static void RequestProcessor()
        {
            while(!m_bConnected) Thread.Sleep(1000);

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
                    catch(Exception e)
                    {
                        Console.WriteLine("Error requesting sim object type: " + e.Message);
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
                    oSimDataRequest.value = dValue;
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
