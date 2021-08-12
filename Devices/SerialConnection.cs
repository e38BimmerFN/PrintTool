using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace PrintTool
{
	public class SerialConnection
	{
		private SerialPortStream port;
		TextBox box { get; set; }
		public string portName { get; set; }
		public string fileloc { get; set; }

		public SerialConnection(string portname, string fileloc, TextBox box)
		{
			this.box = box;
			this.portName = portName;
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
				Log(portName + " cannot be connected to.");
			}

		}


		private async void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try { await Log(port.ReadLine()); }
			catch { }
		}

		public void Close()
		{
			if (port.IsOpen)
			{
				port.Close();
			}
		}


		private async Task Log(string log)
		{
			log = log + "\n";
			//logging to textbox
			box.Dispatcher.Invoke(new Action(() =>
			{
				box.AppendText(log);
				box.ScrollToEnd();
			}));

			//Logging to file
			await File.AppendAllTextAsync(fileloc + port + ".txt", log);
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
