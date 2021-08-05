using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace PrintTool
{
    public class NetConnection
    {
        List<string> data;
        public NetConnection(string IP, int port)
        {
            TcpClient client = new TcpClient();
            client.Connect(IP, port);
            NetworkStream stream  = client.GetStream();

            
            
        }


        public async void Listener(NetworkStream stream)
        {
           
        }
    }
}
