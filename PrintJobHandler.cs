using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace PitCrewUltimateByDerekHearst
{
    class PrintJobHandler
    {

        public async Task sendPrint9100(string ip)
        {
            string filename = "C:\\Users\\HearstDe\\Desktop\\USBSend\\1pg_Default_AnyType.ps";

            byte[] data = File.ReadAllBytes(filename);

            TcpClient client = new TcpClient();
            try
            {
                client.Connect(ip, 9100);
            }
            catch
            {
                System.Windows.MessageBox.Show("Incorrect IP");
                return;
            }
            NetworkStream stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);

            client.Close();
        }


    }
}
