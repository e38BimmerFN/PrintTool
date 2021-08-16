using RJCP.IO.Ports;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for SerialConnection.xaml
	/// </summary>
	public partial class SerialConnection : UserControl
	{
		Logger logger;
		private SerialPortStream port = new();
		public int refreshRate = 200;
		public string fileLoc = @"Data\Logs\Temp\";



		public SerialConnection(string portName)
		{
			InitializeComponent();
			logger = new(portName);
			logLocation.Children.Add(logger);
			port.PortName = portName;
			port.BaudRate = 115200;
			port.DataBits = 8;
			port.StopBits = StopBits.One;
			port.Parity = Parity.None;
			port.DtrEnable = true;
			port.RtsEnable = true;
			port.DataReceived += Port_DataReceived1;

			try
			{
				port.Open();
			}
			catch
			{
				logger.Log(portName + " cannot be connected to.");
			}

			Timer timer = new(50);

		}

		private async void Port_DataReceived1(object sender, SerialDataReceivedEventArgs e)
		{
			try { await logger.Log(port.ReadExisting()); }
			catch { }
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

		private void customCommandEntry_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Return)
			{
				SendData(customCommandEntry.Text);
				customCommandEntry.Text = "";
			}
		}

		public static string[] GetPorts()
		{
			return SerialPortStream.GetPortNames();
		}
	}
}
