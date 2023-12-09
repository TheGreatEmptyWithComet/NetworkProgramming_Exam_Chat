namespace BroadcastServerApp
{
    internal class Program
    {
        private static BroadcastServer broadcastServer = new BroadcastServer();

        static void Main(string[] args)
        {
            // Release socket resources at app exit
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            broadcastServer.Start();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            broadcastServer.AtExit();
        }
    }
}