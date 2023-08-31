
using GlassServerLib;
using Microsoft.AspNetCore.SignalR;

public class GlassServerHub : Hub
{
    public static SimVarManager manager;
    public static IClientProxy client;
    public async Task AddSimVar(string simvar, string units, uint updateIntervalMs)
    {
        client = Clients.All;
        manager.AddRequest(simvar, units, updateIntervalMs);
    }

    public async Task SetSimVar(string simvar, double value)
    {
        client = Clients.All;
        manager.SetValue(simvar, value);
    }

}

