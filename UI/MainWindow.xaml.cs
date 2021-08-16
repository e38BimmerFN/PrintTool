using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;




namespace PrintTool
{
	public partial class MainWindow : Window
	{
		Printer printer = new();
		const string SIRUSSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/";
		const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
		const string JOLTPATH = @"\\jedibdlserver.boi.rd.hpicorp.net\JediSystems\Published\DailyBuilds";
		System.Threading.CancellationTokenSource cancelSource = new();


		public MainWindow()
		{
			InitializeComponent();
		}




		#region Startup
		private async void LoadTrigger(object sender, EventArgs e)
		{
			if (File.Exists(@"Data\Logs\Temp\PrintToolLog.txt")) { File.Delete(@"Data\Logs\Temp\PrintToolLog.txt"); }
			Helper.InstallOrUpdate();

			printer.box = PrintToolLogs;
			await printer.Log("The time is " + DateTime.Now);
			await printer.Log("You have used this program : " + Settings.Default.TimesLaunched++ + " times");
			await printer.Log("If you have any issues, please direct them to derek.hearst@hp.com");
			await printer.Log("Logs for this session will be located at :" + printer.loggingLocation);
			await printer.Log("Have a good day");

			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
			Helper.PopulateListBox(savedPrintJobs, "Data\\Jobs\\");
			if (!Helper.HPStatus())
			{
				MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
				firmwareTab.IsEnabled = false;
			}
			else
			{
				//await Helper.PopulateComboBox(duneVersions, DUNESITE + "?C=M;O=D");
				sirusSGPSelect.Items.Add("yolo_sgp/");
				sirusSGPSelect.Items.Add("avengers_sgp/");
				sirusSGPSelect.SelectedIndex = 0;
			}
			
			try
			{
				savedPrinters.SelectedItem = Settings.Default.LastLoaded;
				ConnectionsLoadDefaults(sender, e);
			}
			catch
			{
				await printer.Log("Couldn't load last used printer.");
			}

		}

		#endregion Startup

		#region Connections Tab

		//Printer Details
		private async void printerModel_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.model = printerModelEntry.Text;
		}
		private async void printerEngine_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.engine = printerEngineEntry.Text;
		}
		private async void printerID_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.id = printerIdEntry.Text;
		}
		private async void printerTypeEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.type = printerTypeEntry.Text;
		}

		//Connections
		private async void printerIpEntry_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.printerIp = printerIpEntry.Text;
			if (await Helper.CheckIP(printerIpEntry.Text))
			{
				printerIpEntry.Background = System.Windows.Media.Brushes.LightGreen;
				connectButton.IsEnabled = true;
				openEWSButton.IsEnabled = true;
			}

			else
			{
				printerIpEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
				connectButton.IsEnabled = false;
				openEWSButton.IsEnabled = false;
			}

		}
		private async void dartIpEntry_TextedChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			printer.dartIp = dartIpEntry.Text;
			if (await Helper.CheckIP(dartIpEntry.Text))
			{
				dartIpEntry.Background = System.Windows.Media.Brushes.LightGreen;
				enableDartCheckBox.IsEnabled = true;
				enableTelnetCheckBox.IsEnabled = true;
				openDartButton.IsEnabled = true;
			}

			else
			{
				dartIpEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
				enableDartCheckBox.IsEnabled = false;
				enableTelnetCheckBox.IsEnabled = false;
				openDartButton.IsEnabled = false;
			}
		}
		private void openDartButton_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", "http://" + dartIpEntry.Text);
		}
		private void openEWSButton_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", "http://" + printerIpEntry.Text);
		}

		//Log Settings
		private async void enableSerialCheckBox_Click(object sender, RoutedEventArgs e)
		{
			await Task.Delay(10);
			printer.enableSerial = enableSerialCheckBox.IsChecked ?? false;
		}
		private async void enableDart_Click(object sender, RoutedEventArgs e)
		{
			await Task.Delay(10);
			printer.enableDart = enableDartCheckBox.IsChecked ?? false;
		}
		private async void enableTelnet_Click(object sender, RoutedEventArgs e)
		{
			await Task.Delay(10);
			printer.enableTelnet = enableTelnetCheckBox.IsChecked ?? false;
		}
		private async void enablePrinterStatus_Click(object sender, RoutedEventArgs e)
		{
			await Task.Delay(10);
			printer.enablePrinterStatus = enablePrinterStatus.IsChecked ?? false;
		}


		//Start logging
		private async void connectButton_Click(object sender, RoutedEventArgs e)
		{

			if (printer.connected) //stop
			{
				printer.connected = false;
				await printer.Log("Disconnecting.");
				foreach (SerialConnection serial in printer.serialConnections) { serial.Close(); }
				foreach (TelnetConnection telnet in printer.telnetConnections) { telnet.Close(); }
				connectButton.Background = System.Windows.Media.Brushes.LightGreen;
				connectButton.Content = "Conect and Flush";
			}
			else //start
			{
				printer.telnetConnections.Clear();
				printer.serialConnections.Clear();
				serialConnectionsTabControl.Items.Clear();
				telnetConnectionsTabControl.Items.Clear();
				foreach (string file in Directory.GetFiles(@"Data\Logs\Temp\"))
				{
					if (file.Contains("PrintToolLog.txt")) { continue; }
					File.Delete(file);
				}
				if (enableSerialCheckBox.IsChecked ?? false)
				{
					await printer.Log("Connecting to serial connections...");
					foreach (string portname in SerialConnection.GetPorts())
					{
						await printer.Log("Connecting to " + portname);
						SerialConnection conection = new(portname);
						TabItem tab = new() { Content = conection, Header = portname };
						serialConnectionsTabControl.Items.Add(tab);
						printer.serialConnections.Add(conection);
					}
				}
				if (enableTelnetCheckBox.IsChecked ?? false)
				{
					if (dartIpEntry.Text is null or "0.0.0.0") { MessageBox.Show("Dart IP is invalid"); }
					else
					{
						foreach (int port in TelnetConnection.GetPorts())
						{
							await printer.Log("Connecting to " + port);
							TelnetConnection connection = new(printer.dartIp, port);
							TabItem tab = new() { Content = connection, Header = port.ToString() };
							telnetConnectionsTabControl.Items.Add(tab);
							printer.telnetConnections.Add(connection);
						}
					}
				}

				if (enablePrinterStatus.IsChecked ?? false)
				{
					//Todo
				}
				printer.connected = true;
				connectButton.Background = System.Windows.Media.Brushes.PaleVioletRed;
				connectButton.Content = "Disconnect";
				await printer.Log("Finished connecting.");

			}
		}
		private void openLogs_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", Directory.GetCurrentDirectory().ToString()+@"\Data\Logs\Temp\");
		}
		private void captureData_Click(object sender, RoutedEventArgs e)
		{

		}

		//Saving
		public void ConnectionsSaveDefaults(object sender, EventArgs e)
		{
			printer.SaveConfig();
			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
		}
		public async void ConnectionsLoadDefaults(object sender, EventArgs e)
		{
			if (savedPrinters.SelectedItem is null or "Nothing Found") { await printer.Log("Select something first"); return; }
			Settings.Default.LastLoaded = savedPrinters.SelectedItem.ToString();
			Settings.Default.Save();
			printer.LoadConfig(@"Data\Printers\" + savedPrinters.SelectedItem.ToString());
			printerModelEntry.Text = printer.model;
			printerIdEntry.Text = printer.id;
			printerEngineEntry.Text = printer.engine;
			printerIpEntry.Text = printer.printerIp.ToString();
			enableDartCheckBox.IsChecked = printer.enableDart;
			enableTelnetCheckBox.IsChecked = printer.enableTelnet;
			dartIpEntry.Text = printer.dartIp;
			enableSerialCheckBox.IsChecked = printer.enableSerial;
		}
		public async void ConnectionsDeleteDefaults(object sender, EventArgs e)
		{
			if (savedPrinters.SelectedItem is null or "Nothing Found") { await printer.Log("Select something first"); return; }
			File.Delete(@"Data\Printers\" + savedPrinters.SelectedItem);
			Helper.PopulateListBox(savedPrinters, "Data\\Printers\\");
		}




		#endregion Connections

		#region Firmware Tab
		#region Jolt




		#endregion Jolt
		#region Yolo

		private async void sirusSGPSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusDistSelect, SIRUSSITE + sirusSGPSelect.Text, "dist/");
			sirusDistSelect.SelectedIndex = 0;
		}

		private async void sirusDistSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusFWVersionSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + "?C=M;O=D");
			sirusFWVersionSelect.SelectedIndex = 0;
		}

		private async void sirusFWVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusBranchSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text);
			sirusBranchSelect.SelectedIndex = 0;
		}

		private async void sirusBranchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusPackageSelect, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text + "?C=S;O=D", "fhx");
			sirusPackageSelect.SelectedIndex = 0;
		}

		private async void siriusCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			await Task.Delay(10);
			await Helper.PopulateComboBox(sirusPackageSelect, sirusCustomLink.Text + "?C=S;O=D", "fhx");
			sirusPackageSelect.SelectedIndex = 0;
		}

		private async void sirusFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
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


		#endregion Yolo
		#region Dune
		//private async void duneVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		//{
		//	await Task.Delay(100);
		//	if (duneVersions.Text == "") { return; }
		//	await Helper.PopulateComboBox(duneProducts, DUNESITE + duneVersions.Text);
		//}
		//private async void duneProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		//{
		//	await Task.Delay(100);
		//	if (duneVersions.Text == "" || duneProducts.Text == "") { return; }
		//	await Helper.PopulateComboBox(dunePackages, DUNESITE + duneVersions.Text + duneProducts.Text + "/?C=S;O=D", "fhx");
		//}
		//private async void firmwareDuneCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		//{
		//	await Task.Delay(100);
		//	if (duneCustomEntry.Text == "" || !duneCustomEntry.Text.EndsWith("/")) { return; }
		//	await Helper.PopulateComboBox(duneCustomPackages, duneCustomEntry.Text, "fhx");
		//}


		#endregion Dune
		#region SharedFirmware
		//private async void firmwareUSBSend(object sender, RoutedEventArgs e)
		//{
		//	System.Threading.CancellationToken cancelToken = cancelSource.Token;
		//	if (yoloTab.IsSelected)
		//	{
		//		if (yoloDailyTab.IsSelected)
		//		{
		//			await Firmware.DLAndSend(yoloPackages.Text, YOLOSITE + yoloProducts.Text + yoloVersions.Text + yoloDistros.Text, printer, yoloInstallButton, cancelToken);
		//		}
		//		else
		//		{
		//			await Firmware.DLAndSend(yoloCustomPackages.Text, yoloCustomEntry.Text, printer, yoloInstallButton, cancelToken);
		//		}
		//	}
		//	else
		//	{
		//		if (duneDailyTab.IsSelected)
		//		{
		//			await Firmware.DLAndSend(dunePackages.Text, DUNESITE + duneVersions.Text + duneProducts.Text, printer, duneInstallButton, cancelToken);
		//		}
		//		else
		//		{
		//			await Firmware.DLAndSend(duneCustomPackages.Text, yoloCustomEntry.Text, printer, duneInstallButton, cancelToken);
		//		}
		//	}
		//}
		private void firmwareCancel(object sender, RoutedEventArgs e)
		{
			cancelSource.Cancel();
		}
		#endregion SharedFirmware
		#endregion Firmware

		#region Printing tab 
		private List<string> generateArgs()
		{
			string sendType = "";
			if (psButton.IsChecked == true) { sendType = "1"; }
			if (pclButton.IsChecked == true) { sendType = "2"; }
			if (escpButton.IsChecked == true) { sendType = "3"; }

			string duplex = "OFF";
			string duplexMode = "";
			if (simplexButton.IsChecked == true) { duplex = "OFF"; }
			if (duplexLEButton.IsChecked == true) { duplex = "ON"; duplexMode = "LONGEDGE"; }
			if (duplexSEButton.IsChecked == true) { duplex = "ON"; duplexMode = "SHORTEDGE"; }

			List<string> args = new();
			args.Add("temp.ps"); //filename
			args.Add("PrintTool Selection Send"); //jobname
			args.Add(sendType); //what language
			args.Add(printPages.Text); // copies of pages
			args.Add(duplex); // duplexing on or off
			args.Add(duplexMode); //duplexing selection
			args.Add(paperTypeSelection.Text);
			args.Add(printSourceTray.Text);
			args.Add(printOutputTray.Text);
			args.Add(printCopies.Text);

			return args;
		}

		private async void printSend9100Button(object sender, RoutedEventArgs e)
		{
			string filename = PrintQueue.PrintGenerator(generateArgs());
			await PrintQueue.SendIP(printerIpEntry.Text, filename);
		}
		private async void printSendUSBButton(object sender, RoutedEventArgs e)
		{
			string filename = PrintQueue.PrintGenerator(generateArgs());
			await PrintQueue.SendUSB(filename);

		}
		private void printSaveJob_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists(@"Data\Jobs\" + printNameJob.Text)) { File.Delete(@"Data\Jobs\" + printNameJob.Text); }
			File.Copy(PrintQueue.PrintGenerator(generateArgs()), @"Data\Jobs\" + printNameJob.Text);
			Helper.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
		}
		private void printDeteleJob_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(@"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString())) { MessageBox.Show(savedPrintJobs.SelectedItem.ToString() + "Doesnt exist"); }
			File.Delete(@"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString());
			Helper.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
		}
		private async void printSendJob_Click(object sender, RoutedEventArgs e)
		{
			if (savedPrintJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
			await PrintQueue.SendIP(printerIpEntry.Text, @"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString());
		}



















		#endregion

		#region Log UI



		#endregion Log ui

		
	}
}
