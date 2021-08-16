using PrimS.Telnet;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;


namespace PrintTool
{
	/// <summary>
	/// Interaction logic for TelnetConnection.xaml
	/// </summary>
	public partial class TelnetConnection : UserControl
	{
		private System.Threading.CancellationTokenSource tokenSource = new();
		private Client cli;
		public int refreshRate = 200;
		private Logger logger;

		public TelnetConnection(string ip, int port)
		{
			InitializeComponent();
			logger = new(port.ToString());
			logLocation.Children.Add(logger);
			var token = tokenSource.Token;
			cli = new Client(ip, port, token);
			cli.TryLoginAsync("root", "", 1000);
			Timer timer = new(refreshRate);
			timer.Elapsed += GetData;
			timer.Start();
		}



		private async void GetData(object sender, ElapsedEventArgs e)
		{
			string result;
			try { result = await cli.ReadAsync(); }
			catch { return; }

			await logger.Log(result);
		}

		public async Task Write(string command)
		{
			await cli.WriteLine(command);

		}

		public void Close()
		{
			cli.Dispose();
		}

		public static List<int> GetPorts()
		{
			List<int> connections = new();
			connections.Add(8108);
			connections.Add(8109);
			connections.Add(8110);


			return connections;
		}

		private async void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Return)
			{
				await Write(customCommandEntry.Text);
				customCommandEntry.Text = "";
			}
		}
	}
}
