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
		readonly private System.Threading.CancellationTokenSource tokenSource = new();
		readonly private Client cli;
		private const int refreshRate = 200;
		readonly private Logger logger;
		public bool paused = false;

		public TelnetConnection(string ip, int port)
		{
			InitializeComponent();
			logger = new(port.ToString());
			logLocation.Children.Add(logger);
			var token = tokenSource.Token;            
			cli = new Client(ip, port, token);
			try
			{
				cli.TryLoginAsync("root", "", 1000);
			}
            catch
            {
				MessageBox.Show("Unable to connect to" + ip + port);
            }
			Timer timer = new(refreshRate);
			timer.Elapsed += GetData;
			timer.Start();
		}



		private async void GetData(object sender, ElapsedEventArgs e)
		{
			string result;
			try { result = await cli.ReadAsync(); }
			catch { return; }
			if (!paused)
            {
				await logger.Log(result);
			}		
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

		private async void printEngineModel_Click(object sender, RoutedEventArgs e)
		{
			await Write("udws \"pelican_dox_wrapper.td /ei/printEngineModel\"");
		}

		private async void getCanonFWRev_Click(object sender, RoutedEventArgs e)
		{
			await Write("udws \"pelican_dox_wrapper.td /ei/getCanonFWRev\"");
		}

		private async void getEngineVerient_Click(object sender, RoutedEventArgs e)
		{
			await Write("udws \"pelican_dox_wrapper.td /ei/getEngineVariant\"");
		}

		private async void tryEngineUpdate_Click(object sender, RoutedEventArgs e)
		{
			await Write("udws \"pelican_dox_wrapper.try_engine_update\"");
		}

		private async void getUpdateStatus_Click(object sender, RoutedEventArgs e)
		{
			await Write("udws \"pelican_dox_wrapper.get_update_status\"");
		}

		private async void reboot_Click(object sender, RoutedEventArgs e)
		{
			await Write("reboot");
		}
	}
}
