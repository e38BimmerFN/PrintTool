using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PrintTool
{
	public class Dart
	{
		public bool isEnabled { get; set; }
		public string ip { get; set; }
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
			connections.Add(new(ip.ToString(), 8108));
			connections.Add(new(ip.ToString(), 8109));
			connections.Add(new(ip.ToString(), 8110));
		}

		public void Disconnect()
		{
			connections.Clear();
		}


		public void Flush()
		{
			if (!isEnabled) { return; }
			if (ip == null) { return; }
			var dart = new Process();
			dart.StartInfo.FileName = "dart.exe";
			dart.StartInfo.Arguments = ip.ToString() + " flush";
		}

		public void OpenWebsite()
		{
			if (!isEnabled) { return; }
			if (ip == null) { return; }
			Process.Start(ip.ToString());
		}

		public async Task<string> DownloadLogs()
		{
			if (!isEnabled) { return "DartNotEnabled"; }
			if (ip == null) { return "DartHasNoIP"; }
			string filename = "Dart.bin";
			var dart = new Process();
			dart.StartInfo.FileName = "dart.exe";
			dart.StartInfo.Arguments = ip.ToString() + " dl " + filename;
			dart.Start();
			await dart.WaitForExitAsync();
			return filename;
		}

	}
}
