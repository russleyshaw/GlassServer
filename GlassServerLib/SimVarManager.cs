using Microsoft.FlightSimulator.SimConnect;
using System.Collections.ObjectModel;
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

        public enum COPY_ITEM
        {
            Name = 0,
            Value,
            Unit
        }

        public class SimvarRequest
        {
            public DEFINITION eDef = DEFINITION.Dummy;
            public REQUEST eRequest = REQUEST.Dummy;

            public string sName = "";
            public bool bIsString = false;
            public double dValue = 0.0;
            public string sValue = null;

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


        // String properties must be packed inside of a struct
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            // this is how you declare a fixed size string
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String sValue;

            // other definitions can be added to this struct
            // ...
        };

        /// User-defined win32 event
        public const int WM_USER_SIMCONNECT = 0x0402;

        /// Window handle
        private IntPtr m_hWnd = new IntPtr(0);


        private uint m_iCurrentDefinition = 0;
        private uint m_iCurrentRequest = 0;

        /// SimConnect object
        private SimConnect m_oSimConnect = null;
        public ObservableCollection<SimvarRequest> lSimvarRequests = new ObservableCollection<SimvarRequest>();

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
            }
            catch (COMException ex)
            {
                Console.WriteLine("Connection to KH failed: " + ex.Message);
            }

            StartPolling();
        }

        public async Task StartPolling()
        {
            while(true)
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
                    if (oSimvarRequest.bIsString)
                    {
                        Struct1 result = (Struct1)data.dwData[0];
                        oSimvarRequest.dValue = 0;
                        oSimvarRequest.sValue = result.sValue;
                    }
                    else
                    {
                        double dValue = (double)data.dwData[0];
                        oSimvarRequest.dValue = dValue;
                        oSimvarRequest.sValue = dValue.ToString("F9");
                    }
                    Console.WriteLine(oSimvarRequest.sName + ": " + oSimvarRequest.sValue);

                    OnSimVarUpdated?.Invoke(oSimvarRequest);
                    oSimvarRequest.updateValue();
                }
            }
        }


        private bool RegisterToSimConnect(SimvarRequest _oSimvarRequest)
        {
            if (m_oSimConnect != null)
            {
                if (_oSimvarRequest.bIsString)
                {
                    /// Define a data structure containing string value
                    m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, "", SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
                    /// If you skip this step, you will only receive a uint in the .dwData field.
                    m_oSimConnect.RegisterDataDefineStruct<Struct1>(_oSimvarRequest.eDef);
                }
                else
                {
                    /// Define a data structure containing numerical value
                    m_oSimConnect.AddToDataDefinition(_oSimvarRequest.eDef, _oSimvarRequest.sName, _oSimvarRequest.sUnits, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
                    /// If you skip this step, you will only receive a uint in the .dwData field.
                    m_oSimConnect.RegisterDataDefineStruct<double>(_oSimvarRequest.eDef);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddRequest(string _sNewSimvarRequest, string _sNewUnitRequest, uint updateIntervalMs, bool _bIsString)
        {
            SimvarRequest oSimvarRequest = new SimvarRequest
            {
                eDef = (DEFINITION)m_iCurrentDefinition,
                eRequest = (REQUEST)m_iCurrentRequest,
                sName = _sNewSimvarRequest,
                bIsString = _bIsString,
                sUnits = _bIsString ? null : _sNewUnitRequest,
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
    }
}