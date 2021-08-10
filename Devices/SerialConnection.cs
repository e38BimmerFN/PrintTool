using RJCP.IO.Ports;
using System.Diagnostics;

namespace PrintTool
{
	public class SerialConnection
	{
		private SerialPortStream port;
		public Logger log { get; set; }
		public string portname {get;set;}
		public SerialConnection(Logger log)
		{
			port = new();
			this.log = log;
		}

		public void Connect(string portname)
		{
			this.portname = portname;
			port = new SerialPortStream();
			port.PortName = portname;
			port.BaudRate = 115200;
			port.DataBits = 8;
			port.StopBits = StopBits.One;
			port.Parity = Parity.None;
			port.DataReceived += Port_DataReceived;
			try
			{
				port.Open();
			}
			catch
			{
				System.Windows.MessageBox.Show(portname + " cannot be connected to.");
			}
			
		}

		private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			string indata = port.ReadLine();
			log.Log(indata);
		}

		public void Close()
		{
			if (port.IsOpen)
			{
				port.Close();
			}
		}

		
		public void SendData(string data)
		{
			port.WriteLine(data);
		}

		public static string[] GetPorts()
		{
			
			return SerialPortStream.GetPortNames();
		}
	}
}
