using System;

namespace Wraith.Server
{
    class Program
    {
        public static Server server;

        public static void Main()
        {
            server = new Server();
            server.Run();
        }
    }
}
