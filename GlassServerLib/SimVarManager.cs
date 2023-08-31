using Microsoft.FlightSimulator.SimConnect;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;

namespace GlassServerLib
{
    public class SimVarManager
    {
        public enum DEFINITION
        {
            Dummy = 0
        };

        public enum REQUEST
        {
            Dummy = 0,
            Struct1
        };


        public class SimvarRequest
        {
            public DEFINITION eDef = DEFINITION.Dummy;
            public REQUEST eRequest = REQUEST.Dummy;

            public string sName = "";
            public double dValue = 0.0;

            public string sUnits;

            public bool bPending = true;
            public bool bStillPending = false;


            public double dUpdateIntervalMs;
            public DateTime dtNextUpdate = DateTime.Now;


            public bool getShouldUpdate()
            {
                if (bPending)
                {
                    bStillPending = true;
                    return false;
                }
                if (bStillPending) return false;

                if (DateTime.Now < dtNextUpdate) return false;

                return true;
            }

            public void updateValue()
            {
                dtNextUpdate = DateTime.Now.AddMilliseconds(dUpdateIntervalMs);
                bPending = false;
                bStillPending = false;
            }


        };

        public delegate void UpdatedSimVar(SimvarRequest simVar);
        public UpdatedSimVar? OnSimVarUpdated;

        /// User-defined win32 event
        public const int WM_USER_SIMCONNECT = 0x0402;

        /// Window handle
        private IntPtr m_hWnd = new IntPtr(0);


        private uint m_iCurrentDefinition = 0;
        private uint m_iCurrentRequest = 0;

        /// SimConnect object
        private SimConnect m_oSimConnect = null;
        public List<SimvarRequest> lSimvarRequests = new List<SimvarRequest>();

        public void Connect()
        {
            Console.WriteLine("Connect");
            if (m_oSimConnect != null)
            {
                return;
            }


            try
            {
                /// The constructor is similar to SimConnect_Open in the native API
                m_oSimConnect = new SimConnect("GlassServer", m_hWnd, WM_USER_SIMCONNECT, null, 0);

                /// Listen to connect and quit msgs
                m_oSimConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                m_oSimConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);

                /// Listen to exceptions
                m_oSimConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);

                /// Catch a simobject data request
                m_oSimConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataBytype);

                StartPolling();
            }
            catch (COMException ex)
            {
                Console.WriteLine("Connection to KH failed: " + ex.Message);
            }

        }

        public async Task StartPolling()
        {
            Console.WriteLine("Start Polling...");

            while (true)
            {
                SendRequests();
                await Task.Delay(100);

                ReceiveSimConnectMessage();
                await Task.Delay(100);
            }
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnect");


            if (m_oSimConnect != null)
            {
                /// Dispose serves the same purpose as SimConnect_Close()
                m_oSimConnect.Dispose();
                m_oSimConnect = null;
            }


            // Set all requests as pending
            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                oSimvarRequest.bPending = true;
                oSimvarRequest.bStillPending = true;
            }
        }



        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect Opened");
            // Register pending requests
            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                if (oSimvarRequest.bPending)
                {
                    oSimvarRequest.bPending = !RegisterToSimConnect(oSimvarRequest);
                    oSimvarRequest.bStillPending = oSimvarRequest.bPending;
                }
            }
        }

        /// The case where the user closes game
        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("SimConnect_OnRecvQuit");
            Console.WriteLine("KH has exited");

            Disconnect();
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
            Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());
        }

        private void SimConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            uint iRequest = data.dwRequestID;
            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                if (iRequest == (uint)oSimvarRequest.eRequest)
                {
                    var prevValue = oSimvarRequest.dValue;
                    var dValue = (double)data.dwData[0];
                    oSimvarRequest.dValue = dValue;


                    if (prevValue != oSimvarRequest.dValue)
                    {
                        OnSimVarUpdated?.Invoke(oSimvarRequest);
                    }
                    oSimvarRequest.updateValue();
                }
            }
        }


        private bool RegisterToSimConnect(SimvarRequest _oSimvarRequest)
        {
            if (m_oSimConnect == null) return false;


            /// Define a data structure containing numerical value
            m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, _oSimvarRequest.sUnits, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
            /// If you skip this step, you will only receive a uint in the .dwData field.
            m_oSimConnect.RegisterDataDefineStruct<double>(_oSimvarRequest.eDef);


            return true;

        }

        public void AddRequest(string _sNewSimvarRequest, string _sNewUnitRequest, uint updateIntervalMs)
        {
            Console.WriteLine($"Add Request({_sNewSimvarRequest}, {_sNewUnitRequest}, {updateIntervalMs})");

            SimvarRequest oSimvarRequest = new SimvarRequest
            {
                eDef = (DEFINITION)m_iCurrentDefinition,
                eRequest = (REQUEST)m_iCurrentRequest,
                sName = _sNewSimvarRequest,
                sUnits = _sNewUnitRequest,
                dUpdateIntervalMs = updateIntervalMs,

            };

            oSimvarRequest.bPending = !RegisterToSimConnect(oSimvarRequest);
            oSimvarRequest.bStillPending = oSimvarRequest.bPending;

            lSimvarRequests.Add(oSimvarRequest);

            ++m_iCurrentDefinition;
            ++m_iCurrentRequest;
        }

        public void ReceiveSimConnectMessage()
        {
            m_oSimConnect?.ReceiveMessage();
        }

        public void SendRequests()
        {

            foreach (SimvarRequest oSimvarRequest in lSimvarRequests)
            {
                if (oSimvarRequest == null) continue;
                if (!oSimvarRequest.getShouldUpdate()) continue;

                m_oSimConnect?.RequestDataOnSimObjectType(oSimvarRequest.eRequest, oSimvarRequest.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                oSimvarRequest.bPending = true;
            }
        }

        public SimvarRequest? FindRequestByName(string sName)
        {
            return lSimvarRequests.Find(r => r.sName == sName);
        }

        public void SetValue(string sName, double dNewValue)
        {
            var oSimVarRequest = FindRequestByName(sName);
            if (oSimVarRequest == null) return;

            m_oSimConnect.SetDataOnSimObject(oSimVarRequest.eDef, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, dNewValue);
        }
    }
}