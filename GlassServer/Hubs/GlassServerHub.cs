
using GlassServerLib;
using Microsoft.AspNetCore.SignalR;

public class GlassServerHub : Hub {
    public static SimVarManager manager;
    public static IClientProxy client;
    public async Task AddSimVar(string simvar, string units, uint updateIntervalMs, bool isString) {
        client = Clients.All;

        Console.WriteLine($"AddSimVar({simvar}, {units}, {updateIntervalMs}, {isString}");
        manager.AddRequest(simvar, units, updateIntervalMs, isString);
    }
        
}

