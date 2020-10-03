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
    class SimManager
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

        private static Dictionary<string, SimDataDef> m_dataDefinitions = new Dictionary<string, SimDataDef>();
        private static Dictionary<string, SimEventDef> m_eventDefinitions = new Dictionary<string, SimEventDef>();


        static void PopulateEventDefinitions()
        {
            Console.WriteLine("Populating event definitions...");
            m_eventDefinitions.Clear();

            var b = new SimEventDefBuilder();

            Add(b.Name("PARKING_BRAKES"));
            Add(b.Name("GEAR_TOGGLE"));
            Add(b.Name("SMOKE_TOGGLE"));
            Add(b.Name("PITOT_HEAT_TOGGLE"));

            Add(b.Name("TOGGLE_MASTER_BATTERY"));
            Add(b.Name("TOGGLE_MASTER_ALTERNATOR"));
            Add(b.Name("APU_GENERATOR_SWITCH_TOGGLE"));

            Add(b.Name("FLAPS_UP"));
            Add(b.Name("FLAPS_1"));
            Add(b.Name("FLAPS_2"));
            Add(b.Name("FLAPS_3"));
            Add(b.Name("FLAPS_DOWN"));

            Add(b.Name("TOGGLE_AVIONICS_MASTER"));
            Add(b.Name("AP_MASTER"));
            Add(b.Name("YAW_DAMPER_TOGGLE"));
            Add(b.Name("AP_PANEL_HEADING_HOLD"));
            Add(b.Name("AP_PANEL_ALTITUDE_HOLD"));
            Add(b.Name("TOGGLE_FLIGHT_DIRECTOR"));
            Add(b.Name("SYNC_FLIGHT_DIRECTOR_PITCH"));
            Add(b.Name("AP_HDG_HOLD"));
            Add(b.Name("AP_ALT_HOLD"));
            Add(b.Name("AP_ATT_HOLD"));
            Add(b.Name("AP_NAV1_HOLD"));
            Add(b.Name("AP_ALT_VAR_SET_ENGLISH"));
            Add(b.Name("AP_VS_VAR_SET_ENGLISH"));



            Add(b.Name("HEADING_BUG_SET")); // degrees
            Add(b.Name("REQUEST_FUEL_KEY"));



            Add(b.Name("DECREASE_DECISION_HEIGHT"));
            Add(b.Name("INCREASE_DECISION_HEIGHT"));

            Add(b.Name("TOGGLE_WING_FOLD"));
            Add(b.Name("TOGGLE_PUSHBACK"));

            Add(b.Name("AP_VS_VAR_INC"));
            Add(b.Name("AP_VS_VAR_DEC"));

            // LIGHTS
            Add(b.Name("STROBES_TOGGLE"));
            Add(b.Name("PANEL_LIGHTS_TOGGLE"));
            Add(b.Name("LANDING_LIGHTS_TOGGLE"));
            Add(b.Name("TOGGLE_BEACON_LIGHTS"));
            Add(b.Name("TOGGLE_TAXI_LIGHTS"));
            Add(b.Name("TOGGLE_LOGO_LIGHTS"));
            Add(b.Name("TOGGLE_WING_LIGHTS"));
            Add(b.Name("TOGGLE_NAV_LIGHTS"));
            Add(b.Name("TOGGLE_CABIN_LIGHTS"));

            Add(b.Name("BAROMETRIC"));

            Add(b.Name("COM_RECEIVE_ALL_TOGGLE"));
            Add(b.Name("COM_RADIO_WHOLE_DEC")); // Decrements COM by one MHz
            Add(b.Name("COM_RADIO_WHOLE_INC")); // Increments COM by one MHz
            Add(b.Name("COM_RADIO_FRACT_DEC")); // Decrements COM by 25 KHz
            Add(b.Name("COM_RADIO_FRACT_INC")); // Increments COM by 25 KHz

            foreach (var navId in new[] { 1, 2 })
            {
                foreach (var dir in new[] { "INC", "DEC " })
                {
                    foreach (var amount in new[] { "FRACT", "WHOLE" })
                    {
                        Add(b.Name(string.Format("NAV{0}_RADIO_{1}_{2}", navId, amount, dir)));
                    }

                    Add(b.Name(string.Format("VOR{0}_OBI_{1}", navId, dir)));
                }

                Add(b.Name(string.Format("NAV{0}_RADIO_SET", navId)));
                Add(b.Name(string.Format("NAV{0}_STBY_SET", navId)));
                Add(b.Name(string.Format("NAV{0}_RADIO_SWAP", navId)));

                Add(b.Name(string.Format("COM{0}_TRANSMIT_SELECT", navId)));

                Add(b.Name(string.Format("VOR{0}_SET", navId)));
                Add(b.Name(string.Format("DME{0}_TOGGLE", navId)));
            }


            Add(b.Name("THROTTLE_FULL"));
            Add(b.Name("THROTTLE_INCR"));
            Add(b.Name("THROTTLE_INCR_SMALL"));
            Add(b.Name("THROTTLE_DECR"));
            Add(b.Name("THROTTLE_DECR_SMALL"));
            Add(b.Name("THROTTLE_CUT"));
            Add(b.Name("THROTTLE_SET")); // 0 to 16383

            Add(b.Name("TOGGLE_ALL_STARTERS"));

            foreach (var engId in new[] { 1, 2, 3, 4 })
            {
                Add(b.Name(string.Format("TOGGLE_ALTERNATOR{0}", engId)));
                Add(b.Name(string.Format("TOGGLE_STARTER{0}", engId)));

                Add(b.Name(string.Format("MIXTURE{0}_SET", engId))); // 0 to 16383
                Add(b.Name(string.Format("MIXTURE{0}_RICH", engId)));
                Add(b.Name(string.Format("MIXTURE{0}_INCR", engId)));
                Add(b.Name(string.Format("MIXTURE{0}_INCR_SMALL", engId)));
                Add(b.Name(string.Format("MIXTURE{0}_DECR", engId)));
                Add(b.Name(string.Format("MIXTURE{0}_DECR_SMALL", engId)));
                Add(b.Name(string.Format("MIXTURE{0}_LEAN", engId)));

                Add(b.Name(string.Format("THROTTLE{0}_FULL", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_INCR", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_INCR_SMALL", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_DECR", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_DECR_SMALL", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_CUT", engId)));
                Add(b.Name(string.Format("THROTTLE{0}_SET", engId))); // 0 to 16383
            }

            Console.WriteLine("Populated event definitions!");

            static void Add(SimEventDefBuilder _builder)
            {
                var def = _builder.Build();
                try { m_eventDefinitions.Add(def.name, def); }
                catch { Console.WriteLine("Failed to add definition for event {0}", def.name); }
            }
        }

        static void PopulateDataDefinitions()
        {
            Console.WriteLine("Populating data definitions...");
            m_dataDefinitions.Clear();

            // https://docs.microsoft.com/en-us/previous-versions/microsoft-esp/cc526981(v=msdn.10)

            var b = new SimDataDefBuilder();

            string[] tanks = { "CENTER", "CENTER2", "CENTER3", "LEFT MAIN", "LEFT AUX", "LEFT TIP", "RIGHT MAIN", "RIGHT AUX", "RIGHT TIP", "EXTERNAL1", "EXTERNAL2" };

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

            for (var i = 1; i <= 4; i++)
            {
                Add(b.Name(string.Format("GENERAL ENG COMBUSTION:{0}", i)));
                Add(b.Name(string.Format("GENERAL ENG MASTER ALTERNATOR:{0}", i)));
                Add(b.Name(string.Format("GENERAL ENG FUEL PUMP SWITCH:{0}", i)));
            }



            Add(b.Name("COM TRANSMIT:1"));
            Add(b.Name("COM TRANSMIT:2"));
            Add(b.Name("COM RECIEVE ALL"));

            Add(b.Name("NAV AVAILABLE:1"));
            Add(b.Name("NAV AVAILABLE:2"));
            Add(b.Name("NAV HAS NAV:1"));
            Add(b.Name("NAV HAS NAV:2"));

            Add(b.Name("SIM ON GROUND").ReadOnly());

            // Percent
            b = b.Units("percent");
            Add(b.Name("AILERON TRIM PCT"));
            Add(b.Name("ELEVATOR TRIM PCT"));
            for (var i = 1; i <= 4; i++)
            {
                Add(b.Name(string.Format("GENERAL ENG THROTTLE LEVER POSITION:{0}", i)).ReadOnly());
                Add(b.Name(string.Format("GENERAL ENG MIXTURE LEVER POSITION:{0}", i)).ReadOnly());
                Add(b.Name(string.Format("GENERAL ENG PROPELLER LEVER POSITION:{0}", i)).ReadOnly());
                Add(b.Name(string.Format("GENERAL ENG PCT MAX RPM:{0}", i)).ReadOnly());
            }

            foreach (var tank in tanks)
            {
                Add(b.Name(string.Format("FUEL TANK {0} LEVEL", tank)).ReadOnly());
            }

            // Gallons
            b = b.Units("gallons");
            foreach (var tank in tanks)
            {

                Add(b.Name(string.Format("FUEL TANK {0} CAPACITY", tank)).ReadOnly());
                Add(b.Name(string.Format("FUEL TANK {0} QUANTITY", tank)).ReadOnly());
            }

            // Knots
            b = b.Units("knots");
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

            // psf
            b = b.Units("fsp");
            for (var i = 1; i <= 4; i++)
            {
                Add(b.Name(string.Format("GENERAL ENG OIL PRESSURE:{0}", i)));
            }

            // RPM
            b = b.Units("rpm");
            for (var i = 1; i <= 4; i++)
            {
                Add(b.Name(string.Format("GENERAL ENG RPM:{0}", i)).ReadOnly());
                Add(b.Name(string.Format("ENG MAX RPM:{0}", i)).ReadOnly());
            }

            for (var i = 0; i <= 3; i++)
            {
                Add(b.Name(string.Format("WHEEL RPM:{0}", i)).ReadOnly());
            }

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

            Add(b.Name("GPS WP NEXT LAT").ReadOnly());
            Add(b.Name("GPS WP NEXT LON").ReadOnly());
            Add(b.Name("GPS WP PREV LAT").ReadOnly());
            Add(b.Name("GPS WP PREV LON").ReadOnly());

            Add(b.Name("AMBIENT WIND DIRECTION").ReadOnly());
            Add(b.Name("AUTOPILOT HEADING LOCK DIR").ReadOnly());

            Add(b.Name("PLANE HEADING DEGREES MAGNETIC").ReadOnly());
            Add(b.Name("PLANE HEADING DEGREES TRUE").ReadOnly());

            Add(b.Name("NAV RADIAL:1"));
            Add(b.Name("NAV RADIAL:2"));
            Add(b.Name("NAV RADIAL ERROR:1"));
            Add(b.Name("NAV RADIAL ERROR:2"));

            Add(b.Name("AILERON TRIM"));
            Add(b.Name("RUDDER TRIM"));

            // Radians
            b = b.Units("radians");
            Add(b.Name("ELEVATOR TRIM POSITION"));


            // Temperature
            b = b.Units("celsius");
            Add(b.Name("AMBIENT TEMPERATURE").ReadOnly());
            for(var i = 1; i <= 4; i++)
            {
                Add(b.Name(string.Format("GENERAL ENG OIL TEMPERATURE:{0}", i)));
            }

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
            Add(b.Name("NAV TOFROM:1").ReadOnly().Enum((0, "Off"), (1, "To"), (2, "From")));
            Add(b.Name("NAV TOFROM:2").ReadOnly().Enum((0, "Off"), (1, "To"), (2, "From")));

            Add(b.Name("GPS APPROACH MODE").ReadOnly().Enum(
                (0, "None"),
                (1, "Transition"),
                (2, "Final"),
                (3, "Missed")
            ));

            Add(b.Name("GPS APPROACH WP TYPE").ReadOnly().Enum(
                (0, "None"),
                (1, "Fix"),
                (2, "Procedure Turn Left"),
                (3, "Procedure Turn Right"),
                (4, "DME ARC Left"),
                (5, "DME ARC Right"),
                (6, "Holding Left"),
                (7, "Holding Right"),
                (8, "Distance"),
                (9, "Altitude"),
                (10, "Manual Mequence"),
                (11, "Vector To Final")
            ));

            Add(b.Name("GPS APPROACH APPROACH TYPE").ReadOnly().Enum(
                (0, "None"),
                (1, "GPS"),
                (2, "VOR"),
                (3, "NDB"),
                (4, "ILS"),
                (5, "Localizer"),
                (6, "SDF"),
                (7, "LDA"),
                (8, "VOR/DME"),
                (9, "NDB/DME"),
                (10, "RNAV"),
                (11, "Backcourse")
            ));

            b = b.Units("Mask");
            for (var i = 0; i <= 10; i++)
            {
                Add(b.Name(string.Format("TURB ENG TANKS USED:{0}", i)).ReadOnly());
                Add(b.Name(string.Format("RECIP ENG FUEL TANKS USED:{0}", i)).ReadOnly());
            }


            Console.WriteLine("Populated data definitions!");

            static void Add(SimDataDefBuilder _builder)
            {
                var def = _builder.Build();
                try { m_dataDefinitions.Add(def.name, def); }
                catch { Console.WriteLine("Failed to add definition for variable {0}", def.name); }
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
            try
            {
                PopulateDataDefinitions();
                PopulateEventDefinitions();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to populate.");
            }

            foreach (var def in m_dataDefinitions.Values)
            {
                try
                {
                    RegisterDataDefinition(def);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to register sim data def \"{0}\" ({1})", def.name, def.units);
                }
            }

            foreach (var def in m_eventDefinitions.Values)
            {
                try
                {
                    RegisterEventDefinition(def);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to register sim event def \"{0}\"", def.name);
                }
            }

            m_oSimConnect.SetNotificationGroupPriority(EVENT_GROUP.Main, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);
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
            AssertConnected();

            // Try once
            SimDataDef def;
            if (m_dataDefinitions.TryGetValue(_sName, out def))
            {
                if (!def.registered) RegisterDataDefinition(def);

                return def;
            }
            else
            {
                throw new HttpResponseException(404, string.Format("{0} is not a defined simvar.", _sName));
            }
        }

        public static void RequestDataSet(string _sName, double _dValue)
        {
            AssertConnected();

            SimDataDef def;
            if (m_dataDefinitions.TryGetValue(_sName, out def))
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

        public static void SendEvent(string _sName, uint _dValue)
        {
            AssertConnected();

            SimEventDef def;
            if (m_eventDefinitions.TryGetValue(_sName, out def))
            {
                if (!def.registered) RegisterEventDefinition(def);

                m_oSimConnect.TransmitClientEvent(0, def.eDef, _dValue, EVENT_GROUP.Main, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            else
            {
                throw new HttpResponseException(404, string.Format("{0} is not a defined sim event.", _sName));
            }
        }

        /// <summary>
        /// Add a new SimData request to the polling.
        /// </summary>
        /// <param name="_sName"></param>
        /// <param name="_sUnits"></param>
        private static void RegisterDataDefinition(SimDataDef def)
        {
            if (def.registered) return;
            def.eDef = (DEFINITION)m_iCurrentDefinition;
            def.eRequest = (REQUEST)m_iCurrentRequest;
            m_iCurrentDefinition++;
            m_iCurrentRequest++;

            try
            {
                m_oSimConnect.AddToDataDefinition(def.eDef, def.name, def.units, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                m_oSimConnect.RegisterDataDefineStruct<double>(def.eRequest);
                def.registered = true;
            }
            catch
            {
                // Console.WriteLine("Failed to register \"{0}\" simvar definition.", def.name);
            }

            m_iCurrentDefinition++;
            m_iCurrentRequest++;
        }

        private static void RegisterEventDefinition(SimEventDef def)
        {
            if (def.registered) return;

            def.eDef = (EVENT_DEFINITION)m_iCurrentDefinition;
            m_iCurrentDefinition++;

            m_oSimConnect.MapClientEventToSimEvent(def.eDef, def.name);
            m_oSimConnect.AddClientEventToNotificationGroup(EVENT_GROUP.Main, def.eDef, false);
            def.registered = true;
        }


        /// <summary>
        /// Re-request the data to refresh the cache.
        /// </summary>
        private static void RequestProcessor()
        {
            while (!m_bConnected) Thread.Sleep(1000);

            while (true)
            {
                var defs = m_dataDefinitions.Values.Where(def => def.registered && !def.pending);
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

            foreach (var entry in m_dataDefinitions)
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

        private static void AssertConnected()
        {
            if (m_oSimConnect == null || !m_bConnected)
            {
                throw new HttpResponseException(503, "SimConnect service is not connected.");
            }
        }

        private static void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;

            Console.WriteLine("SimConnect_OnRecvException: ID {0}, SendID {1} " + eException.ToString(), data.dwID, data.dwSendID);
        }
    }
}
