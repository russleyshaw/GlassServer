using GlassServerLib;

namespace GlassServer
{
    public class Sandbox
    {
        static public void Main()
        {
            Console.WriteLine("Hello, World!");

            var manager = new SimVarManager();
            manager.Connect();

            manager.AddRequest("PLANE ALTITUDE", "feet", false);

            while(true)
            {

                manager.ReceiveSimConnectMessage();
                manager.OnTick();
                Thread.Sleep(100);
            }
        }
    }
}