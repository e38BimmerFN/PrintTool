using System.IO.Ports;

namespace PrintTool
{
	public class SerialConnection
	{
		private SerialPort port { get; set; }
		public Logger log { get; set; }
		public SerialConnection(Logger log)
		{
			port = new();
			this.log = log;
		}

		public void Connect(string portname)
		{
			port = new SerialPort();
			port.PortName = portname;
			port.BaudRate = 115200;
			port.DataBits = 8;
			port.StopBits = StopBits.One;
			port.Parity = Parity.None;
			port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
			port.Open();
		}

		public void Close()
		{
			port.Close();
		}

		private void DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			string indata = port.ReadLine();
			log.Log(indata);
		}

		public void SendData(string data)
		{
			port.WriteLine(data);
		}

		public static string[] GetPorts()
		{
			return SerialPort.GetPortNames();
		}


	}
}
