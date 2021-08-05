using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;

namespace PrintTool
{
    public class Dart
    {
        public bool isEnabled { get; set; }
        public IPAddress IP { get; set; }
        public bool usingPorts { get; set; }
        private List<NetConnection> connections { get; set; }

        public Dart()
        {
            isEnabled = false;
            usingPorts = false;
            //Ports.Add(8108);
            //Ports.Add(8109);
            //Ports.Add(8110);
        }

        public void ConnectToPorts()
        {
            connections.Add(new(IP.ToString(),8108));
            connections.Add(new(IP.ToString(), 8109));
            connections.Add(new(IP.ToString(), 8110));
        }

        public void Flush()
        {
            if (!isEnabled) { return; }
            if (IP == null) { return; }
            var dart = new Process();
            dart.StartInfo.FileName = "dart.exe";
            dart.StartInfo.Arguments = IP.ToString() + " flush";
        }
        
        public void OpenWebsite()
        {
            if(!isEnabled) { return; }
            if(IP == null) { return; }
            Process.Start(IP.ToString());
        }

        public async Task<string> DownloadLogs()
        {
            if (!isEnabled) { return "DartNotEnabled"; }
            if (IP == null) { return "DartHasNoIP"; }
            string filename = "Dart.bin";
            var dart = new Process();
            dart.StartInfo.FileName = "dart.exe";
            dart.StartInfo.Arguments = IP.ToString() +" dl "+ filename;
            dart.Start();
            await dart.WaitForExitAsync();
            return filename;
        }

    }
}
