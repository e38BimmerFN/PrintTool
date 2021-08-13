using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RJCP.IO.Ports;

using System.IO;
using System.Text.RegularExpressions;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for SerialConnection.xaml
	/// </summary>
	public partial class SerialConnection : UserControl
	{
		Logger logger;
		private SerialPortStream port;

		public string portName { get; set; }
		public string fileLoc = @"Data\Logs\Temp\";



		public SerialConnection(string portName)
		{
			InitializeComponent();
			logger = new(portName);
			logLocation.Children.Add(logger);
			this.portName = portName;

			port = new();
			port.PortName = portName;
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
				logger.Log(portName + " cannot be connected to.");
			}
		}
		

		private async void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try { await logger.Log(port.ReadLine()); }
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

		public static string[] GetPorts()
		{

			return SerialPortStream.GetPortNames();
		}
	}
}
