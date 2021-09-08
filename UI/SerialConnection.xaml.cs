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
		private Logger logger;
		private SerialPortStream port = new();
		public int refreshRate = 200;




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
			try
			{
				port.WriteLine(data);
				port.WriteLine("\r\n");
			}
			catch
			{
				System.Windows.MessageBox.Show("Serial connection not accepting commands");
			}

		}

		private void CustomCommandEntry_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				SendData(customCommandEntry.Text);
				customCommandEntry.Text = "";
			}
		}

		public static string[] GetPorts()
		{
			return SerialPortStream.GetPortNames();
		}

		private void EscapeButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("\u001B");
		}

		private void CtrF_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("\u0006");
		}

		private void Reboot_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("reboot");
		}

		private void Reset_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("reset");

		}

		private void PartClean_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("disktest partclean 0");
		}

		private void NetExec_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("netexec");
		}

		private void PristineDisk_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("disktest pristine");
		}

		private void CleanNVRam_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SendData("nvmgmt vs clean -r");
		}
	}
}
