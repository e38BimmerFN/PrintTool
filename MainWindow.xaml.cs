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
		const string YOLOSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
		const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
		System.Threading.CancellationTokenSource cancelSource = new();


		public MainWindow()
		{
			InitializeComponent();
		}




		#region Startup
		private async void LoadTrigger(object sender, EventArgs e)
		{
			if(File.Exists(@"Data\Logs\Temp\PrintToolLog.txt")) { File.Delete(@"Data\Logs\Temp\PrintToolLog.txt"); }
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
			await Helper.PopulateComboBox(duneVersions, DUNESITE + "?C=M;O=D");
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


		

		private async void connectButton_Click(object sender, RoutedEventArgs e)
		{

			if (printer.connected) //stop
			{
				printer.connected = false;
				await printer.Log("Disconnecting.");				
				serialConnectionsTabControl.Items.Clear();
				telnetConnectionsTabControl.Items.Clear();
				connectButton.Background = System.Windows.Media.Brushes.LightGreen;
				connectButton.Content = "Connect and Log";
			}
			else //start
			{
				foreach (string file in Directory.GetFiles(@"Data\Logs\Temp\")) 
				{ 
					if(file.Contains( "PrintToolLog.txt")) { continue; }
					File.Delete(file); 
				}
				if (enableSerialCheckBox.IsChecked ?? false)
				{
					await printer.Log("Connecting to serial connections...");
					foreach (string portname in SerialConnection.GetPorts())
					{
						await printer.Log("Connecting to " + portname);
						TabItem tempTab = new();
						tempTab.Header = portname;
						TextBox tempBox = new();
						tempTab.Content = tempBox;
						SerialConnection tempConnection = new(portname, tempBox);
						printer.serialConnections.Add(tempConnection);
						serialConnectionsTabControl.Items.Add(tempTab);
					}
				}

				if (enableTelnetCheckBox.IsChecked ?? false)
				{
					if (dartIpEntry.Text is null or "0.0.0.0") { MessageBox.Show("Dart IP is invalid"); }
					else
					{
						foreach (int port in TelnetConnection.getAvaliable())
						{
							await printer.Log("Connecting to " + port);
							TabItem tempTab = new();
							tempTab.Header = port;
							TextBox tempBox = new();
							tempTab.Content = tempBox;
							TelnetConnection tempConnection = new(printer.dartIp, port, printer.loggingLocation, tempBox);
							printer.telnetConnections.Add(tempConnection);
							serialConnectionsTabControl.Items.Add(tempTab);

						}
					}
				}
				printer.connected = true;
				connectButton.Background = System.Windows.Media.Brushes.PaleVioletRed;
				connectButton.Content = "Disconnect and Flush Logs";
				await printer.Log("Finished connecting.");

			}
		}



		private void openLogs_Click(object sender, RoutedEventArgs e)
		{

		}


		private void captureData_Click(object sender, RoutedEventArgs e)
		{

		}


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
		#region Yolo
		private async void yoloProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			await Task.Delay(100);
			await Helper.PopulateComboBox(yoloVersions, YOLOSITE + yoloProducts.Text + "/?C=M;O=D");
		}
		private async void yoloVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			await Task.Delay(100);
			await Helper.PopulateComboBox(yoloDistros, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text);
		}
		private async void yoloDistros_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			await Task.Delay(100);
			await Helper.PopulateComboBox(yoloPackages, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/?C=S;O=D", "fhx");

		}
		private async void firmwareYoloCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			await Task.Delay(100);
			await Helper.PopulateComboBox(yoloCustomPackages, yoloCustomEntry.Text, "fhx");
		}
		public async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
			List<string> results = await Helper.getListings(website);
			string filename = "";
			if (yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
			else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
			await Firmware.DLAndSend(filename, website, printer, firmwareYoloSecureB, cancelToken);
			firmwareYoloUnsecureB.Content = "Convert to unsecure";
		}
		public async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
			List<string> results = await Helper.getListings(website);
			string filename = "";
			if (yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
			else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
			await Firmware.DLAndSend(filename, website, printer, firmwareYoloSecureB, cancelToken);
			firmwareYoloSecureB.Content = "Convert to secured";
		}
		private void firmwareYoloResetB_Click(object sender, RoutedEventArgs e)
		{

		}
		#endregion Yolo
		#region Dune
		private async void duneVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			await Task.Delay(100);
			if (duneVersions.Text == "") { return; }
			await Helper.PopulateComboBox(duneProducts, DUNESITE + duneVersions.Text);
		}
		private async void duneProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			await Task.Delay(100);
			if (duneVersions.Text == "" || duneProducts.Text == "") { return; }
			await Helper.PopulateComboBox(dunePackages, DUNESITE + duneVersions.Text + duneProducts.Text + "/?C=S;O=D", "fhx");
		}
		private async void firmwareDuneCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			await Task.Delay(100);
			if (duneCustomEntry.Text == "" || !duneCustomEntry.Text.EndsWith("/")) { return; }
			await Helper.PopulateComboBox(duneCustomPackages, duneCustomEntry.Text, "fhx");
		}
		private void firmewareDuneUnsecure_Click(object sender, RoutedEventArgs e)
		{
			// Not enabled yet
		}

		private void firmewareDuneSecure_Click(object sender, RoutedEventArgs e)
		{
			// Not enabled yet
		}

		private async void firmewareDuneReset_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			string path = @"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\@Shared\BlankFiles\";
			string file = "";
			if (duneSpecialProductSelect.Text == "Selene")
			{
				file = "dune_selene_blank_rwfs.fhx";
			}
			else
			{
				file = "dune_ulysses_blank_rwfs.fhx";
			}
			await Firmware.DLAndSend(file, path, printer, firmewareDuneReset, cancelToken);
		}

		#endregion Dune
		#region SharedFirmware
		private async void firmwareUSBSend(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			if (yoloTab.IsSelected)
			{
				if (yoloDailyTab.IsSelected)
				{
					await Firmware.DLAndSend(yoloPackages.Text, YOLOSITE + yoloProducts.Text+ yoloVersions.Text + yoloDistros.Text, printer, yoloInstallButton, cancelToken);
				}
				else
				{
					await Firmware.DLAndSend(yoloCustomPackages.Text, yoloCustomEntry.Text, printer, yoloInstallButton, cancelToken);
				}
			}
			else
			{
				if (duneDailyTab.IsSelected)
				{
					await Firmware.DLAndSend(dunePackages.Text, DUNESITE + duneVersions.Text + duneProducts.Text, printer, duneInstallButton, cancelToken);
				}
				else
				{
					await Firmware.DLAndSend(duneCustomPackages.Text, yoloCustomEntry.Text, printer, duneInstallButton, cancelToken);
				}
			}
		}
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

		#region Log Capture Tab

		#endregion Log Capture


	}
}
