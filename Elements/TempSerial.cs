using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace PrintTool
{
	public class TempSerial
	{
		private SerialPortStream port;
		
		public string portName { get; set; }
		public string fileLoc = @"Data\Logs\Temp\";

		public TabItem tab;
		private TextBox box;
		private ScrollViewer scroller;

		public TempSerial(string portName)
		{
			box = new()
			{
				IsReadOnly = true,
				FontFamily = new System.Windows.Media.FontFamily("Consolas")
			};

			scroller = new()
			{
				Content = box
			};
			var commandGrid = new StackPanel();

			var sp1 = new StackPanel();
			
			GroupBox gp1 = new()
			{
				Content = sp1
			};

			commandGrid.Children.Add(gp1);


			var grid = new Grid();
			var Column1 = new ColumnDefinition();
			Column1.Width = new System.Windows.GridLength(.75, System.Windows.GridUnitType.Star);
			var Column2 = new ColumnDefinition();
			Column2.Width = new System.Windows.GridLength(.4, System.Windows.GridUnitType.Star);
			grid.ColumnDefinitions.Add(Column1);
			grid.ColumnDefinitions.Add(Column2);
			
			grid.Children.Add(box);
			Grid.SetColumn(box, 0);
			grid.Children.Add(commandGrid);

			tab = new()
			{
				Visibility = System.Windows.Visibility.Collapsed,
				Content = grid,
				Header = portName
			};
						

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
			tab.Visibility = System.Windows.Visibility.Visible;
			if (!log.Contains("\n") || !log.Contains("\r")) { log += "\n"; }
			//logging to textbox
			
			box.AppendText(log);
			scroller.ScrollToBottom();
			

			//Logging to file
			await File.AppendAllTextAsync(fileLoc + "Serial" + portName + ".txt", log);

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
