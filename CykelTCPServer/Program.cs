using System;

namespace CykelTCPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TCPServer server = new TCPServer();
            server.Start();
        }
    }
}
