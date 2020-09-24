using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace GlassServer
{
    public enum DEFINITION
    {
        Dummy = 0
    };

    public enum REQUEST
    {
        Dummy = 0
    };


    /// <summary>
    /// SimData information to use for future requests.
    /// </summary>
    class SimDataRequest
    {
        public DEFINITION eDef = DEFINITION.Dummy;
        public REQUEST eRequest = REQUEST.Dummy;

        public string sName;
        public double dValue;
        public string sUnits;
        public bool bPending = false;
    }

    /// <summary>
    /// Provides managed access to SimConnect values. 
    /// The intent is to have this manager keep a cache of requested values, adding more values to the polling loop as they're requested.
    /// </summary>
    class SimDataManager
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private static SimConnect m_oSimConnect = null;
        private static bool m_bConnected = false;

        private static EventWaitHandle m_eMessagePump;
        private static Thread m_tMessageThread;
        private static Thread m_tRequestThread;

        private static uint m_iCurrentDefinition = 0;
        private static uint m_iCurrentRequest = 0;

        public  static Dictionary<string, SimDataRequest> m_mSimDataRequests = new Dictionary<string, SimDataRequest>();

        public static void Connect()
        {
            Console.WriteLine("Connecting...");

            try
            {
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

                // TODO: Wait until proper connection
                Thread.Sleep(1000);
            }
            catch (COMException ex)
            {
                Console.WriteLine("Connection failed: " + ex.Message);
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
            }
        }


        /// <summary>
        /// Request a SimData from the manager.
        /// If there is no existing request, create one and wait for the result.
        /// </summary>
        /// <param name="_sName"></param>
        /// <returns></returns>
        public static async Task<double?> RequestData(string _sName)
        {
            // Try once
            SimDataRequest result;
            if (m_mSimDataRequests.TryGetValue(_sName, out result))
            {
                return result.dValue;
            }

            // Not found, try adding the request
            AddRequest(_sName, SimDataUnitMapper.FindUnits(_sName) ?? "number");

            // TODO: Surely there's a better way to do this with observables?
            // Try to get the value from store after waiting
            for(int tries = 5; tries >= 0; tries--)
            {
                await Task.Delay(200);
                if (m_mSimDataRequests.TryGetValue(_sName, out result))
                {
                    return result.dValue;
                }
            }

            // Not found
            return null;
        }

        /// <summary>
        /// Add a new SimData request to the polling.
        /// </summary>
        /// <param name="_sName"></param>
        /// <param name="_sUnits"></param>
        private static void AddRequest(string _sName, string _sUnits)
        {
            SimDataRequest oSimDataRequest = new SimDataRequest
            {
                eDef = (DEFINITION)m_iCurrentDefinition,
                eRequest = (REQUEST)m_iCurrentRequest,
                sName = _sName,
                sUnits = _sUnits
            };

            m_oSimConnect.AddToDataDefinition(oSimDataRequest.eDef, oSimDataRequest.sName, oSimDataRequest.sUnits, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            m_oSimConnect.RegisterDataDefineStruct<double>(oSimDataRequest.eRequest);

            oSimDataRequest.bPending = false;
            m_mSimDataRequests.Add(oSimDataRequest.sName, oSimDataRequest);

            ++m_iCurrentDefinition;
            ++m_iCurrentRequest;
        }



        /// <summary>
        /// Re-request the data to refresh the cache.
        /// </summary>
        private static void RequestProcessor()
        {
            while (true)
            {
                foreach (var entry in m_mSimDataRequests)
                {
                    var oSimDataRequest = entry.Value;

                    if (!oSimDataRequest.bPending)
                    {
                        m_oSimConnect?.RequestDataOnSimObjectType(oSimDataRequest.eRequest, oSimDataRequest.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                        oSimDataRequest.bPending = true;
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

            foreach (var entry in m_mSimDataRequests)
            {
                var oSimDataRequest = entry.Value;

                if (iRequest == (uint)oSimDataRequest.eRequest)
                {
                    double dValue = (double)data.dwData[0];
                    oSimDataRequest.dValue = dValue;
                    oSimDataRequest.bPending = false;
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
