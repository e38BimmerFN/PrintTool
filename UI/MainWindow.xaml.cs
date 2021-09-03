using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;


namespace PrintTool
{
	public partial class MainWindow : Window
	{

		const string SIRUSSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/";
		const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
		const string JOLTPATH = @"\\jedibdlbroker.boi.rd.hpicorp.net\JediSystems\Published\DailyBuilds\25s\";
		System.Threading.CancellationTokenSource cancelSource = new();

		Logger ptlog = new("PrintTool");
		Printer currPrinter = new();
		IPPHandler ippCli = new();
		List<SerialConnection> serialConnections = new();
		List<TelnetConnection> telnetConnections = new();


		public MainWindow()
		{
			InitializeComponent();
			ptLoggerHere.Content = ptlog;

		}



		#region Startup
		private async void LoadTrigger(object sender, EventArgs e)
		{
			Directory.CreateDirectory(@"Data\Jobs\");
			Directory.CreateDirectory(@"Data\Printers\");
			Directory.CreateDirectory(@"Data\Logs\Temp\");

			if (File.Exists(@"Data\Logs\Temp\PrintToolLog.txt")) { File.Delete(@"Data\Logs\Temp\PrintToolLog.txt"); }
			Helper.InstallOrUpdate();

			await ptlog.Log("The time is " + DateTime.Now);
			await ptlog.Log("You have used this program : " + Settings.Default.TimesLaunched++ + " times");
			await ptlog.Log("If you have any issues, please direct them to derek.hearst@hp.com");
			await ptlog.Log("Logs for this session will be located at : \\Data\\Logs\\Temp\\");
			await ptlog.Log("Have a good day");

			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
			if (!Helper.HPStatus())
			{
				MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
				firmwareTab.IsEnabled = false;
			}
			else
			{
				await Helper.PopulateComboBox(joltYearSelect, JOLTPATH, "", true);
				await Helper.PopulateComboBox(duneVersionSelect, DUNESITE + "?C=M;O=D");
				sirusSGPSelect.Items.Add("yolo_sgp/");
				sirusSGPSelect.Items.Add("avengers_sgp/");
				sirusSGPSelect.SelectedIndex = 0;
			}

			try
			{
				if (Settings.Default.LastLoaded is "" || !File.Exists(@"Data\Printers\" + Settings.Default.LastLoaded)) { }
				else
				{
					savedPrinters.SelectedItem = Settings.Default.LastLoaded;
					ConnectionsLoadDefaults(sender, e);
				}

			}
			catch
			{
				await ptlog.Log("Couldn't load last used printer.");
			}

		}

		#endregion Startup

		#region Connections Tab


		//Printer Details
		private async void PrinterModel_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			currPrinter.Model = printerModelEntry.Text;
		}
		private async void PrinterEngine_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			currPrinter.Engine = printerEngineEntry.Text;
		}
		private async void PrinterID_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			currPrinter.Id = printerIdEntry.Text;
		}
		private async void PrinterTypeEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			currPrinter.Type = printerTypeEntry.Text;
			switch (printerTypeEntry.Text)
			{
				case "Jedi":
					joltTab.IsSelected = true;
					break;
				case "Dune":
					duneTab.IsSelected = true;
					break;
				case "Sirus":
					sirusTab.IsSelected = true;
					break;
				default:
					break;
			}
		}

		//Connections
		private async void PrinterIpEntry_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(500);
			currPrinter.PrinterIp = printerIpEntry.Text;
			if (await Helper.CheckIP(printerIpEntry.Text))
			{
				printerIpEntry.Background = System.Windows.Media.Brushes.LightGreen;
				openEWSButton.IsEnabled = true;
				ippCli.Ip = printerIpEntry.Text;
			}

			else
			{
				printerIpEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
				openEWSButton.IsEnabled = false;
			}


		}
		private async void DartIpEntry_TextedChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(500);
			currPrinter.DartIp = dartIpEntry.Text;
			if (await Helper.CheckIP(dartIpEntry.Text))
			{
				dartIpEntry.Background = System.Windows.Media.Brushes.LightGreen;
				connectTelnetButton.IsEnabled = true;
				ConnectSnoopyButton.IsEnabled = true;
				openDartButton.IsEnabled = true;
			}

			else
			{
				dartIpEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
				connectTelnetButton.IsEnabled = false;
				ConnectSnoopyButton.IsEnabled = false;
				openDartButton.IsEnabled = false;
			}

		}


		private void OpenDartButton_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", "http://" + dartIpEntry.Text);
		}
		private void OpenEWSButton_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", "http://" + printerIpEntry.Text);
		}



		bool serialConnected = false;
		private async void ConnectSerial_Click(object sender, RoutedEventArgs e)
		{
			if (!serialConnected)
			{
				await ptlog.Log("Connecting to serial connections...");
				serialConnections.Clear();
				serialConnectionsTabControl.Items.Clear();
				foreach (string portname in SerialConnection.GetPorts())
				{
					await ptlog.Log("Connecting to " + portname);
					SerialConnection conection = new(portname);
					TabItem tab = new() { Content = conection, Header = portname };
					serialConnectionsTabControl.Items.Add(tab);
					serialConnections.Add(conection);

				}
				connectSerialButton.Content = "Disconnect from Serial";
				connectSerialButton.Background = System.Windows.Media.Brushes.PaleVioletRed;
				serialConnected = true;
				await ptlog.Log("Finished");
			}
			else
			{
				await ptlog.Log("Disconnecting from serial connections....");
				foreach (SerialConnection sc in serialConnections) { sc.Close(); }
				connectSerialButton.Content = "Connect to Serial";
				connectSerialButton.Background = System.Windows.Media.Brushes.LightGreen;
				serialConnected = false;
				await ptlog.Log("Finished");
			}

		}
		bool snoopyConnected = false;
		private async void ConnectSnoopy_Click(object sender, RoutedEventArgs e)
		{
			//TODO
		}
		bool telnetConnected = false;
		private async void ConnectTelnet_Click(object sender, RoutedEventArgs e)
		{
			if (!telnetConnected)
			{
				await ptlog.Log("Connecting to telnet connections...");
				telnetConnections.Clear();
				telnetConnectionsTabControl.Items.Clear();
				foreach (int port in TelnetConnection.GetPorts())
				{
					await ptlog.Log("Connecting to " + port);
					TelnetConnection connection = new(currPrinter.DartIp, port);
					TabItem tab = new() { Content = connection, Header = port.ToString() };
					telnetConnectionsTabControl.Items.Add(tab);
					telnetConnections.Add(connection);
				}
				connectTelnetButton.Content = "Disconnect from Telnet";
				connectTelnetButton.Background = System.Windows.Media.Brushes.PaleVioletRed;
				telnetConnected = true;
				await ptlog.Log("Finished");

			}
			else
			{
				await ptlog.Log("Disconnecting from Telnet connections....");
				foreach (TelnetConnection tc in telnetConnections) { tc.Close(); }
				connectTelnetButton.Content = "Connect to Telnet";
				connectTelnetButton.Background = System.Windows.Media.Brushes.LightGreen;
				telnetConnected = false;
				await ptlog.Log("Finished");
			}

		}

		private async void FlushLogs_Click(object sender, RoutedEventArgs e)
		{

			foreach (string file in Directory.GetFiles(@"Data\Logs\Temp\"))
			{
				if (file.Contains("LogPrintTool.txt")) { continue; }
				File.Delete(file);
			}
			await ptlog.Log("Flushed Logs");
		}





		private void OpenLogs_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", Directory.GetCurrentDirectory().ToString() + @"\Data\Logs\Temp\");
		}


		private async void refreshPrinterAttributes_Click(object sender, RoutedEventArgs e)
		{
			if (!await Helper.CheckIP(currPrinter.PrinterIp)) { return; }
			ippCli.Ip = currPrinter.PrinterIp;

			var response = await ippCli.GetPrinterDetails();
			if (response is null) { return; }
			List<SharpIpp.Model.IppAttribute> atl = response.Sections[1].Attributes;
			ippMedia.Items.Clear();
			ippMediaSource.Items.Clear();
			ippPaperAttributes.Items.Clear();
			ippOutputBin.Items.Clear();
			ippDuplex.Items.Clear();
			ippFinishings.Items.Clear();
			ippCollate.Items.Clear();
			foreach (SharpIpp.Model.IppAttribute at in atl)
			{
				if (true)
				{
					switch (at.Name)
					{
						case "media-supported":
							ippMedia.Items.Add(at.Value);
							break;
						case "media-source-supported":
							ippMediaSource.Items.Add(at.Value);
							break;
						case "media-type-supported":
							ippPaperAttributes.Items.Add(at.Value);
							break;
						case "output-bin-supported":
							ippOutputBin.Items.Add(at.Value);
							break;
						case "sides-supported":
							ippDuplex.Items.Add(at.Value);
							break;
						case "finishings-supported":
							ippFinishings.Items.Add((SharpIpp.Model.Finishings)at.Value);
							break;
						case "multiple-document-handling-supported":
							ippCollate.Items.Add(at.Value);
							break;
						case "printer-make-and-model":
							ippName.Text = at.Value.ToString();
							break;
						case "printer-firmware-version":
							ippFirmwareInstalled.Text = at.Value.ToString();
							break;
						case "pages-per-minute":
							ippPPM.Text = at.Value.ToString();
							break;
						case "color-supported":
							ippColorSupport.Text = at.Value.ToString();
							break;
						case "printer-state":
							ippPrinterState.Text = ((SharpIpp.Model.PrinterState)at.Value).ToString();
							break;
						case "printer-state-reasons":
							ippPrinterStateMessage.Text = at.Value.ToString();
							break;
						case "printer-supply":
							ippTonerLevel.Text = at.Value.ToString();
							break;
						default:
							break;
					}
				}
			}
			try
			{
				ippMedia.SelectedItem = "na_letter_8.5x11in";
				ippMediaSource.SelectedIndex = 0;
				ippPaperAttributes.SelectedItem = "stationery";
				ippOutputBin.SelectedIndex = 0;
				ippDuplex.SelectedIndex = 0;
				ippFinishings.SelectedIndex = 0;
				ippCollate.SelectedIndex = 0;
			}
			catch { }

		}


		//Saving
		public void ConnectionsSaveDefaults(object sender, EventArgs e)
		{
			File.WriteAllText($"Data\\Printers\\{currPrinter.Model}", JsonSerializer.Serialize(currPrinter));
			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
		}
		public async void ConnectionsLoadDefaults(object sender, EventArgs e)
		{
			if (savedPrinters.SelectedItem is null or "Nothing Found") { await ptlog.Log("Select something first"); return; }
			Settings.Default.LastLoaded = savedPrinters.SelectedItem.ToString();
			Settings.Default.Save();
			string json = File.ReadAllText($"Data\\Printers\\{savedPrinters.SelectedItem}");
			currPrinter = JsonSerializer.Deserialize<Printer>(json);
			printerModelEntry.Text = currPrinter.Model;
			printerIdEntry.Text = currPrinter.Id;
			printerEngineEntry.Text = currPrinter.Engine;
			printerTypeEntry.Text = currPrinter.Type;
			printerIpEntry.Text = currPrinter.PrinterIp;
			dartIpEntry.Text = currPrinter.DartIp;

		}
		public async void ConnectionsDeleteDefaults(object sender, EventArgs e)
		{
			if (savedPrinters.SelectedItem is null or "Nothing Found") { await ptlog.Log("Select something first"); return; }
			File.Delete(@"Data\Printers\" + savedPrinters.SelectedItem);
			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
		}




		#endregion Connections

		#region Firmware Tab
		#region Jolt


		private async void JoltYearSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltMonthSelect, JOLTPATH + joltYearSelect.Text + "\\", "", true);
		}

		private async void JoltMonthSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltDaySelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\", "", true);

		}
		private async void JoltDaySelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltProductSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\");
		}

		private async void JoltProductSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltVersionSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\", "", true);
		}

		private async void JoltVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltBuildSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "bdl");
			await Helper.PopulateComboBox(joltCSVSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "csv");
			await Helper.PopulateComboBox(joltFIMSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "exe");
		}

		private async void JoltCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(joltBuildSelect, joltCustomLink.Text, "bdl");
			await Helper.PopulateComboBox(joltCSVSelect, joltCustomLink.Text, "csv");
			await Helper.PopulateComboBox(joltFIMSelect, joltCustomLink.Text, "exe");
		}

		private async void JoltFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (joltFwTab.SelectedIndex == 0)
			{
				await Helper.PopulateComboBox(joltBuildSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "bdl");
				await Helper.PopulateComboBox(joltCSVSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "csv");
				await Helper.PopulateComboBox(joltFIMSelect, JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\", "exe");
			}
			else
			{
				await Helper.PopulateComboBox(joltBuildSelect, joltCustomLink.Text, "bdl");
				await Helper.PopulateComboBox(joltCSVSelect, joltCustomLink.Text, "csv");
				await Helper.PopulateComboBox(joltFIMSelect, joltCustomLink.Text, "exe");
			}
		}


		private async void JoltStart_Click(object sender, RoutedEventArgs e)
		{
			joltStart.IsEnabled = false;

			string sourceLink;
			if (joltFwTab.SelectedIndex == 0)
			{
				sourceLink = JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\";
			}
			else
			{
				sourceLink = joltCustomLink.Text + "\\";
			}
			await ptlog.Log($"Now starting download of {sourceLink}");

			await Helper.DownloadOrCopyFile(joltBuildSelect.Text, sourceLink);
			await Helper.DownloadOrCopyFile(joltCSVSelect.Text, sourceLink);
			await Helper.DownloadOrCopyFile(joltFIMSelect.Text, sourceLink);
			await ptlog.Log($"Finished downloading of {joltBuildSelect.Text}, {joltCSVSelect.Text}, and{joltFIMSelect.Text}");
			if (joltEnableCSV.IsChecked ?? false)
			{
				await ptlog.Log($"Running fimClient with these commands : {joltFIMSelect.Text} -x {currPrinter.PrinterIp} -t bios -n arm64 -c {joltCSVSelect.Text} {joltBuildSelect.Text}");
				ProcessHandler handler = new(joltFIMSelect.Text, $"-x {currPrinter.PrinterIp} -t bios -n arm64 -c {joltCSVSelect.Text} {joltBuildSelect.Text}", ptlog);
				await handler.Start(cancelSource.Token);
			}
			else
			{
				await ptlog.Log($"Running fimClient with these commands : {joltFIMSelect.Text} -x {currPrinter.PrinterIp} -t bios -n arm64 {joltBuildSelect.Text}");
				ProcessHandler handler = new(joltFIMSelect.Text, $"-x {currPrinter.PrinterIp} -t bios -n arm64 {joltBuildSelect.Text}", ptlog);
				await handler.Start(cancelSource.Token);
			}

			File.Delete(joltBuildSelect.Text);
			File.Delete(joltCSVSelect.Text);
			File.Delete(joltFIMSelect.Text);

			await ptlog.Log("FIM Process done!");
			MessageBox.Show("Installation done!");
			joltStart.IsEnabled = true;
		}

		private void JoltOpenFW_Click(object sender, RoutedEventArgs e)
		{
			if (joltFwTab.SelectedIndex == 0)
			{
				Helper.OpenPath(JOLTPATH + joltYearSelect.Text + "\\" + joltMonthSelect.Text + "\\" + joltDaySelect.Text + "\\Products\\" + joltProductSelect.Text + "\\" + joltVersionSelect.Text + "\\");
			}
			else
			{
				Helper.OpenPath(joltCustomLink.Text);
			}

		}

		private void JoltInventorsBell_Click(object sender, RoutedEventArgs e)
		{
			if (joltProductSelect.Items.Contains("Bell"))
			{
				joltProductSelect.SelectedItem = "Bell";
			}
		}

		private void JoltInventorsCurie_Click(object sender, RoutedEventArgs e)
		{
			if (joltProductSelect.Items.Contains("Curie"))
			{
				joltProductSelect.SelectedItem = "Curie";
			}
		}

		private void JoltInventorsEdison_Click(object sender, RoutedEventArgs e)
		{
			if (joltProductSelect.Items.Contains("Edison"))
			{
				joltProductSelect.SelectedItem = "Edison";
			}
		}

		private void JoltInventorsHopper_Click(object sender, RoutedEventArgs e)
		{
			if (joltProductSelect.Items.Contains("Hopper"))
			{
				joltProductSelect.SelectedItem = "Hopper";
			}
		}


		#endregion Jolt
		#region Yolo

		//Main UI
		private async void SirusSGPSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusDistSelect, SIRUSSITE + sirusSGPSelect.Text, "dist/");
		}

		private async void SirusDistSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusFWVersionSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + "?C=M;O=D");
		}

		private async void SirusFWVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusBranchSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text);
		}

		private async void SirusBranchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusPackageSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text + "?C=S;O=D", "fhx");
		}

		private async void SiriusCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusPackageSelect, sirusCustomLink.Text + "?C=S;O=D", "fhx");
		}

		private async void SirusFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			if (sirusFwTab.SelectedIndex == 0)
			{
				await Helper.PopulateComboBox(sirusPackageSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text + "?C=S;O=D", "fhx");
			}
			else
			{
				await Helper.PopulateComboBox(sirusPackageSelect, sirusCustomLink.Text + "?C=S;O=D", "fhx");
			}
			sirusPackageSelect.SelectedIndex = 0;
		}

		//Quick Links
		private void YoloSecureConvert_Click(object sender, RoutedEventArgs e)
		{
			sirusCustomLink.Text = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
			sirusFwTab.SelectedIndex = 1;
		}

		private void YoloUnsecureConvert_Click(object sender, RoutedEventArgs e)
		{
			sirusCustomLink.Text = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
			sirusFwTab.SelectedIndex = 1;
		}


		private async void SirusSendFW_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			if (sirusFwTab.SelectedIndex == 0)
			{
				await Firmware.DLAndSend(sirusPackageSelect.Text, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text, ptlog, sirusSendFW, cancelToken);
			}
			else
			{
				await Firmware.DLAndSend(sirusPackageSelect.Text, sirusCustomLink.Text, ptlog, sirusSendFW, cancelToken);
			}
		}

		private void SirusCancelFW_Click(object sender, RoutedEventArgs e)
		{
			cancelSource.Cancel();
			cancelSource = new();
		}

		private void SirusOpenFW_Click(object sender, RoutedEventArgs e)
		{
			if (sirusFwTab.SelectedIndex == 0)
			{
				Helper.OpenPath(SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text);
			}
			else
			{
				Helper.OpenPath(sirusCustomLink.Text);
			}
		}

		#endregion Yolo
		#region Dune

		private async void DuneFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (duneFwTab.SelectedIndex == 0)
			{
				await Helper.PopulateComboBox(dunePackageSelect, DUNESITE + duneVersionSelect.Text + duneModelSelect.Text + "?C=S;O=D", "fhx");
			}
			else
			{
				await Helper.PopulateComboBox(dunePackageSelect, duneCustomLink.Text, "fhx");
			}


		}


		private async void DuneVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(duneModelSelect, DUNESITE + duneVersionSelect.Text);
			if (duneModelSelect.Items[0].ToString().Contains("defaultProductGroup"))
			{
				duneModelSelect.Items.RemoveAt(0);
			}
			duneModelSelect.SelectedIndex = 0;
		}

		private async void DuneModelSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(dunePackageSelect, DUNESITE + duneVersionSelect.Text + duneModelSelect.Text + "?C=S;O=D", "fhx");
		}

		private async void DuneCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(dunePackageSelect, duneCustomLink.Text, "fhx");
		}

		//Special Links
		private void DuneUtilityFolder_Click(object sender, RoutedEventArgs e)
		{
			duneCustomLink.Text = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";
			duneFwTab.SelectedIndex = 1;
		}
		//Sending
		private async void DuneSendFW_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			if (duneFwTab.SelectedIndex == 0)
			{
				await Firmware.DLAndSend(dunePackageSelect.Text, DUNESITE + duneVersionSelect.Text + duneModelSelect.Text, ptlog, duneSendFW, cancelToken);
			}
			else
			{
				await Firmware.DLAndSend(dunePackageSelect.Text, duneCustomLink.Text, ptlog, duneSendFW, cancelToken);
			}
		}

		private void DuneCancelFW_Click(object sender, RoutedEventArgs e)
		{
			cancelSource.Cancel();
			cancelSource = new();

		}

		private void DuneOpenFw_Click(object sender, RoutedEventArgs e)
		{
			if (duneFwTab.SelectedIndex == 0)
			{
				Helper.OpenPath(DUNESITE + duneVersionSelect.Text + duneModelSelect.Text);
			}
			else
			{
				Helper.OpenPath(duneCustomLink.Text);
			}

		}

		#endregion Dune

		#endregion Firmware


		#region IPP Tab


		private async void IppSendJob_Click(object sender, RoutedEventArgs e)
		{
			if (!await Helper.CheckIP(currPrinter.PrinterIp)) { MessageBox.Show("Invalid IP"); return; }
			string stringToWrite = "";
			string template = File.ReadAllText(@"Data\Jobs\template.ps");
			template = template.Replace("RVER", Settings.Default.Version.ToString());
			template = template.Replace("RGENAT", DateTime.Now.ToString());
			//Currpage
			template = template.Replace("RMEDIA", ippMedia.Text);
			template = template.Replace("RPAPERAT", ippPaperAttributes.Text);
			template = template.Replace("RFINISH", ippFinishings.Text);
			template = template.Replace("RCOLLATE", ippCollate.Text);
			template = template.Replace("RDUPLEX", ippDuplex.Text);
			template = template.Replace("RSOURCE", ippMediaSource.Text);
			template = template.Replace("ROUTBIN", ippOutputBin.Text);
			template = template.Replace("RCOPIES", ippCopies.Text);
			template = template.Replace("RUSERNAME", Environment.UserName);
			

			foreach (int i in Enumerable.Range(0, int.Parse(ippPages.Text)))
			{
				string tempstr = template;
				tempstr = tempstr.Replace("RCURPAGE", i.ToString());
				stringToWrite += tempstr;
			}
			
			await File.WriteAllTextAsync(@"Data\Jobs\temp.ps", stringToWrite);
			string media = ippMedia.Text;
			string duplex = ippDuplex.Text;
			string msource = ippMediaSource.Text;
			string binout = ippOutputBin.Text;
			int copies = int.Parse(ippCopies.Text);
			string collate = ippCollate.Text;
			SharpIpp.Model.Finishings finish = (SharpIpp.Model.Finishings)Enum.Parse(typeof(SharpIpp.Model.Finishings), ippFinishings.Text);
			string mediatype = ippPaperAttributes.Text;

			var res = await ippCli.SendJob(@"Data\Jobs\temp.ps", media, duplex, msource, binout, copies, finish, collate,mediatype);
			if(res is null) { return; }
			ippJobStatusList.Items.Insert(0,new JobStatus(res.JobUri, ippCli));

		}

		private async void ippCancelJobs_Click(object sender, RoutedEventArgs e)
		{
			await ippCli.CancelJob();
		}


		#endregion IPP




		#region misc
		int time;
		Timer clockTime;
		private void StartClock_Click(object sender, RoutedEventArgs e)
		{
			time = 0;
			clockTimer.Content = time;
			clockTime = new(1000);
			clockTime.Elapsed += ClockUpdate;
			clockTime.Start();
			startClock.IsEnabled = false;
		}

		private void ClockUpdate(object sender, ElapsedEventArgs e)
		{
			time++;
			clockTimer.Dispatcher.Invoke(() =>
			{
				clockTimer.Content = time;
			});

		}

		private void StopClock_Click(object sender, RoutedEventArgs e)
		{
			startClock.IsEnabled = true;
			clockTime.Stop();
		}




		#endregion

		
	}
}
