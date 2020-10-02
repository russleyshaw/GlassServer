using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Text.Json;
using System.Text.Json.Serialization;
using GlassServer.Models;

namespace GlassServer
{
    public class SocketServer
    {
        WebSocketServer m_server;
        public void Start(string sBaseUrl)
        {
            m_server = new WebSocketServer(sBaseUrl);
            m_server.AddWebSocketService<SimBehavior>("/sim");
            m_server.Start();
        }

        public void Stop()
        {
            m_server.Stop();
        }
    }


    public class SimClientCommand
    {
        public string[]? subscribe { get; set; }
        public Dictionary<string, double>? setData { get; set; }
        public bool? getData { get; set; }
        public Dictionary<string, uint>? sendEvent { get; set; }
    }

    public class SimServerCommand
    {
        public SimDataModel[]? updateData { get; set; }
    }



    public class SimBehavior : WebSocketBehavior
    {
        HashSet<string> m_subscribedNames = new HashSet<string>();
        protected override void OnMessage(MessageEventArgs e)
        {
            var response = new SimServerCommand();
            var sendResponse = false;
            try
            {
                var cmd = JsonSerializer.Deserialize<SimClientCommand>(e.Data);
                if (cmd == null) return;

                if (cmd.subscribe != null && cmd.subscribe.Length > 0)
                {
                    foreach (var name in cmd.subscribe)
                    {
                        m_subscribedNames.Add(name);
                    }
                }

                if (cmd.setData != null && cmd.setData.Count > 0)
                {
                    foreach (var kv in cmd.setData)
                    {
                        try { SimManager.RequestDataSet(kv.Key, kv.Value); }
                        catch { }
                    }
                }


                if (cmd.getData == true && m_subscribedNames.Count > 0)
                {
                    var models = new List<SimDataModel>();
                    

                    foreach (var name in m_subscribedNames)
                    {
                        try
                        {
                            var def = SimManager.GetDefinition(name);
                            models.Add(new SimDataModel
                            {
                                name = def.name,
                                text = def.text,
                                units = def.units,
                                value = def.value,
                            });
                        }
                        catch { }
                    }

                    if(models.Count > 0)
                    {
                        sendResponse = true;
                        response.updateData = models.ToArray();
                    }
                }

                if (cmd.sendEvent != null && cmd.sendEvent.Count > 0)
                {
                    foreach (var kv in cmd.sendEvent)
                    {
                        try { SimManager.SendEvent(kv.Key, kv.Value); }
                        catch { }
                    }
                }
            }
            catch { }

            if (sendResponse)
            {
                try
                {
                    var sResponse = JsonSerializer.Serialize(response);
                    Send(sResponse);
                }
                catch (Exception _e)
                {
                    Console.WriteLine("Unable to send response: {0}", _e.ToString());
                }
            }
        }
    }

}
