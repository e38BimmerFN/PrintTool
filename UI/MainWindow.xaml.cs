using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		//Paths
		const string SIRUSSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/";
		const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
		const string JOLTPATH = @"\\jedibdlbroker.boi.rd.hpicorp.net\JediSystems\Published\DailyBuilds\25s\";
		DirectoryInfo appdataPath = new(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
		DirectoryInfo pathToSavePrintersTo = null;
		DirectoryInfo pathToSaveJobsTo = null;
		DirectoryInfo pathToSaveLogsTo = null;


		Printer currentlyDisplayedPrinter;
		ObservableCollection<string> currentDisplayingJobs = new();
		List<PrintJob> printedJobs = new();
		Timer RefreshJobListTimer = new(1000);
		Helper ptHelper = null;
		readonly Logger ptlog = new("PrintTool");
		System.Threading.CancellationTokenSource cancelSource = new();

		readonly List<SerialConnection> serialConnections = new();
		readonly List<TelnetConnection> telnetConnections = new();


		public MainWindow()
		{
			InitializeComponent();
			ptLoggerHere.Content = ptlog;
			ptHelper = new(ptProgressBar);

			pathToSavePrintersTo = appdataPath.CreateSubdirectory("DerekTools").CreateSubdirectory("PrintTool").CreateSubdirectory("Printers");
			pathToSaveJobsTo = appdataPath.CreateSubdirectory("DerekTools").CreateSubdirectory("PrintTool").CreateSubdirectory("Jobs");
			pathToSaveLogsTo = appdataPath.CreateSubdirectory("DerekTools").CreateSubdirectory("PrintTool").CreateSubdirectory("Logs");
			printingListActiveJobs.ItemsSource = currentDisplayingJobs;
			RefreshJobListTimer.Start();
			RefreshJobListTimer.Elapsed += MainTimer_Elapsed;
		}

		//Updating Jobs
		private void MainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				currentDisplayingJobs.Clear();
				foreach (PrintJob pj in printedJobs)
				{
					if(pj is not null)
					{
						currentDisplayingJobs.Insert(0, pj.ToString());
					}					
				}
			}));

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


			savedPrintersList.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
			printingListPrinters.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
			printingListJobs.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSaveJobsTo.FullName);


			if (!Helper.ConnectedToHP())
			{
				MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
				firmwareTab.IsEnabled = false;
			}
			else
			{
				joltYearSelect.ItemsSource = await Helper.PopulateFromPathOrSite(JOLTPATH, flip: true);
				joltYearSelect.SelectedIndex = 0;

				duneVersionSelect.ItemsSource = await Helper.PopulateFromPathOrSite(DUNESITE + "?C=M;O=D");
				duneVersionSelect.SelectedIndex = 0;

				sirusSGPSelect.Items.Add("yolo_sgp/");
				sirusSGPSelect.Items.Add("avengers_sgp/");
				sirusSGPSelect.SelectedIndex = 0;
			}
		}

		#endregion Startup

		#region Printers


		private void openPathSavedPrinters_Click(object sender, RoutedEventArgs e)
		{
			Helper.OpenPath(pathToSavePrintersTo.FullName);
		}

		private async void addPrinter_Click(object sender, RoutedEventArgs e)
		{
			InstallPrinterPopup pop = new();
			pop.ShowDialog();
			if (pop.DialogResult.Value)
			{
				Printer pt = new(pop.ipaddress.Text, pop.nickname.Text);
				await pt.RefreshValues();
				Printer.SavePrinter(pathToSavePrintersTo, pt);
				savedPrintersList.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
				printingListPrinters.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
				savedPrintersList.SelectedItem = pop.nickname.Text;
			}
		}

		private async void UpdatePrinterDisplayValues(Printer p)
		{
			currPrinterIP.Text = p.IP;
			currPrinterLabel.Text = p.Nickname;
			currPrinterName.Text = p.IPPName;
			currPrinterNameInfo.Text = p.IPPPNameInfo;
			currPrinterFirmware.Text = p.IPPFirmwareInstalled;
			currPrinterPPM.Text = p.IPPPPM + " ppm";
			currPrinterColorSupported.Text = p.IPPColorSupported;
			currPrinterUUID.Text = p.IPPUUID;
			currPrinterLocation.Text = p.IPPLocation;
			currPrinterState.Text = p.IPPPrinterState;
			currPrinterStateMessage.Text = p.IPPPrinterStateMessage;
			currPrinterSupplyList.ItemsSource = p.IPPSupplyValues;
			currPrinterSupportedMedia.ItemsSource = p.IPPSupportedMedia;
			currPrinterSupportedMediaSource.ItemsSource = p.IPPSupportedMediaSource;
			currPrinterSupportedMediaType.ItemsSource = p.IPPSupportedMediaType;
			currPrinterSupportedOutputTray.ItemsSource = p.IPPSuppportedOutputBin;
			currPrinterSupportedFinishings.ItemsSource = p.IPPSupportedFinishings;
			currPrinterSupportedSides.ItemsSource = p.IPPSupportedSides;
		}

		private async void deletePrinter_Click(object sender, RoutedEventArgs e)
		{
			foreach (string file in savedPrintersList.SelectedItems)
			{
				File.Delete(pathToSavePrintersTo + "\\" + file);
			}
			savedPrintersList.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
		}

		private void savedPrintersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) { return; } //handeling unselection
			foreach (FileInfo file in pathToSavePrintersTo.EnumerateFiles())
			{
				if (file.Name == e.AddedItems[0].ToString()) //getting first item selected
				{
					currentlyDisplayedPrinter = Printer.ReadFromFile(file);
					UpdatePrinterDisplayValues(currentlyDisplayedPrinter);
				}
			}
		}

		private async void currPrinterRefresh_Click(object sender, RoutedEventArgs e)
		{
			if (await currentlyDisplayedPrinter.RefreshValues())
			{
				UpdatePrinterDisplayValues(currentlyDisplayedPrinter);
			}
			else
			{
				MessageBox.Show("Cannot communicate with printer. IP Might have changed or printer is off.", "Cannot communicate with printer", MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}

		private async void currPrinterEditValues_Click(object sender, RoutedEventArgs e)
		{
			InstallPrinterPopup pop = new(currentlyDisplayedPrinter.Nickname, currentlyDisplayedPrinter.IP);
			pop.ShowDialog();
			if (pop.DialogResult.Value)
			{
				currentlyDisplayedPrinter.IP = pop.ipaddress.Text;
				currentlyDisplayedPrinter.Nickname = pop.nickname.Text;
				await currentlyDisplayedPrinter.RefreshValues();
				Printer.SavePrinter(pathToSavePrintersTo, currentlyDisplayedPrinter);
				savedPrintersList.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSavePrintersTo.FullName);
				savedPrintersList.SelectedItem = pop.nickname.Text;
			}
		}

		#endregion

		#region Firmware Tab
		#region Jolt


		private async void JoltYearSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = JOLTPATH + joltYearSelect.SelectedValue + "\\";
			joltMonthSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, flip: true);
			joltMonthSelect.SelectedIndex = 0;
		}

		private async void JoltMonthSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = JOLTPATH + joltYearSelect.SelectedValue + "\\" + joltMonthSelect.SelectedValue + "\\";
			joltDaySelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, flip: true);
			joltDaySelect.SelectedIndex = 0;
		}
		private async void JoltDaySelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = JOLTPATH + joltYearSelect.SelectedValue + "\\" + joltMonthSelect.SelectedValue + "\\" + joltDaySelect.SelectedValue + "\\Products\\";
			joltProductSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path);
			joltProductSelect.SelectedIndex = 0;
		}

		private async void JoltProductSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = JOLTPATH + joltYearSelect.SelectedValue + "\\" + joltMonthSelect.SelectedValue + "\\" + joltDaySelect.SelectedValue + "\\Products\\" + joltProductSelect.SelectedValue + "\\";
			joltVersionSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path);
			joltVersionSelect.SelectedIndex = 0;
		}

		private async void JoltVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = JOLTPATH + joltYearSelect.SelectedValue + "\\" + joltMonthSelect.SelectedValue + "\\" + joltDaySelect.SelectedValue + "\\Products\\" + joltProductSelect.SelectedValue + "\\" + joltVersionSelect.SelectedValue + "\\";
			joltBuildSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "bdl");
			joltCSVSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "csv");
			joltFIMSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "exe");
			joltBuildSelect.SelectedIndex = 0;
			joltCSVSelect.SelectedIndex = 0;
			joltFIMSelect.SelectedIndex = 0;
		}

		private async void JoltCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			string path = joltCustomLink.Text;
			joltBuildSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "bdl");
			joltCSVSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "csv");
			joltFIMSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "exe");
			joltBuildSelect.SelectedIndex = 0;
			joltCSVSelect.SelectedIndex = 0;
			joltFIMSelect.SelectedIndex = 0;
		}

		private async void JoltFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (joltFwTab.SelectedIndex == 0)
			{
				string path = JOLTPATH + joltYearSelect.SelectedValue + "\\" + joltMonthSelect.SelectedValue + "\\" + joltDaySelect.SelectedValue + "\\Products\\" + joltProductSelect.SelectedValue + "\\" + joltVersionSelect.SelectedValue + "\\";
				joltBuildSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "bdl");
				joltCSVSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "csv");
				joltFIMSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "exe");
				joltBuildSelect.SelectedIndex = 0;
				joltCSVSelect.SelectedIndex = 0;
				joltFIMSelect.SelectedIndex = 0;
			}
			else
			{
				string path = joltCustomLink.Text;
				joltBuildSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "bdl");
				joltCSVSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "csv");
				joltFIMSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "exe");
				joltBuildSelect.SelectedIndex = 0;
				joltCSVSelect.SelectedIndex = 0;
				joltFIMSelect.SelectedIndex = 0;
			}
		}


		private async void JoltStart_Click(object sender, RoutedEventArgs e)
		{
			IPEntry ipgetter = new();
			ipgetter.ShowDialog();
			if(ipgetter.DialogResult == false) { return; }
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

			await ptHelper.TryDownloadOrCopyFile(joltBuildSelect.Text, sourceLink);
			await ptHelper.TryDownloadOrCopyFile(joltCSVSelect.Text, sourceLink);
			await ptHelper.TryDownloadOrCopyFile(joltFIMSelect.Text, sourceLink);
			await ptlog.Log($"Finished downloading of {joltBuildSelect.Text}, {joltCSVSelect.Text}, and{joltFIMSelect.Text}");
			string armcommand = "arm";
			string csvcommand = "";

			if (!(joltEnableArm.IsChecked ?? false)) { armcommand += "64"; }
			if (joltEnableCSV.IsChecked ?? false) { csvcommand = $"-c {joltCSVSelect.Text}"; }

			string command = $"-x {ipgetter.ipaddress.Text} -t bios -n {armcommand} {csvcommand} {joltBuildSelect.Text}";

			await ptlog.Log($"Running fimClient with these commands : {joltFIMSelect.Text} {command}");
			ProcessHandler handler = new(joltFIMSelect.Text, command, ptlog);
			await handler.Start(cancelSource.Token);

			try
			{
				File.Delete(joltBuildSelect.Text);
				File.Delete(joltCSVSelect.Text);
				File.Delete(joltFIMSelect.Text);
			}
			catch { };


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
			string path = SIRUSSITE + sirusSGPSelect.SelectedValue;
			sirusDistSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "dist/");
			sirusDistSelect.SelectedIndex = 0;
		}

		private async void SirusDistSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = SIRUSSITE + sirusSGPSelect.SelectedValue + sirusDistSelect.SelectedValue + "?C=M;O=D";
			sirusFWVersionSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path);
			sirusFWVersionSelect.SelectedIndex = 0;
		}

		private async void SirusFWVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = SIRUSSITE + sirusSGPSelect.SelectedValue + sirusDistSelect.SelectedValue + sirusFWVersionSelect.SelectedValue;
			sirusBranchSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path);
			sirusBranchSelect.SelectedIndex = 0;
		}

		private async void SirusBranchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = SIRUSSITE + sirusSGPSelect.SelectedValue + sirusDistSelect.SelectedValue + sirusFWVersionSelect.SelectedValue + sirusBranchSelect.SelectedValue + "?C=S;O=D";
			sirusPackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
			sirusPackageSelect.SelectedIndex = 0;
		}

		private async void SiriusCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			string path = sirusCustomLink.Text;
			sirusPackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
			sirusPackageSelect.SelectedIndex = 0;
		}

		private async void SirusFwTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await Task.Delay(10);
			if (sirusFwTab.SelectedIndex == 0)
			{
				string path = SIRUSSITE + sirusSGPSelect.SelectedValue + sirusDistSelect.SelectedValue + sirusFWVersionSelect.SelectedValue + sirusBranchSelect.SelectedValue + "?C=S;O=D";
				sirusPackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
				sirusPackageSelect.SelectedIndex = 0;
			}
			else
			{
				string path = sirusCustomLink.Text;
				sirusPackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
				sirusPackageSelect.SelectedIndex = 0;
			}
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
		private void yoloHarishREset_Click(object sender, RoutedEventArgs e)
		{
			sirusCustomLink.Text = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/";
			sirusFwTab.SelectedIndex = 1;
		}


		private async void SirusSendFW_Click(object sender, RoutedEventArgs e)
		{
			System.Threading.CancellationToken cancelToken = cancelSource.Token;
			if (sirusFwTab.SelectedIndex == 0)
			{
				await ptHelper.DLAndSend(sirusPackageSelect.Text, SIRUSSITE + sirusSGPSelect.Text + sirusDistSelect.Text + sirusFWVersionSelect.Text + sirusBranchSelect.Text, ptlog, sirusSendFW, cancelToken);
			}
			else
			{
				await ptHelper.DLAndSend(sirusPackageSelect.Text, sirusCustomLink.Text, ptlog, sirusSendFW, cancelToken);
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
				string path = DUNESITE + duneVersionSelect.SelectedValue + duneModelSelect.SelectedValue + "?C=S;O=D";
				dunePackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
				dunePackageSelect.SelectedIndex = 0;
			}
			else
			{
				string path = duneCustomLink.Text;
				dunePackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
				dunePackageSelect.SelectedIndex = 0;
			}
		}


		private async void DuneVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = DUNESITE + duneVersionSelect.SelectedValue;
			duneModelSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path);
			if (duneModelSelect.Items[0].ToString().Contains("defaultProductGroup"))
			{
				duneModelSelect.Items.RemoveAt(0);
			}
			duneModelSelect.SelectedIndex = 0;
		}

		private async void DuneModelSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string path = DUNESITE + duneVersionSelect.Text + duneModelSelect.Text + "?C=S;O=D";
			dunePackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
			dunePackageSelect.SelectedIndex = 0;
		}

		private async void DuneCustomLink_TextChanged(object sender, TextChangedEventArgs e)
		{
			string path = duneCustomLink.Text;
			dunePackageSelect.ItemsSource = await Helper.PopulateFromPathOrSite(path, "fhx");
			dunePackageSelect.SelectedIndex = 0;
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
				await ptHelper.DLAndSend(dunePackageSelect.Text, DUNESITE + duneVersionSelect.Text + duneModelSelect.Text, ptlog, duneSendFW, cancelToken);
			}
			else
			{
				await ptHelper.DLAndSend(dunePackageSelect.Text, duneCustomLink.Text, ptlog, duneSendFW, cancelToken);
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


		private async void printingPrintJobs_Click(object sender, RoutedEventArgs e)
		{
			PrintJob.JobParams myParams = new()
			{
				media = printingMedia.Text,
				duplexing = printingDuplex.Text,
				sourceTray = printingMediaSource.Text,
				outputTray = printingOutputBin.Text,
				copies = int.Parse(printingCopies.Text),
				collation = printingCollate.Text,
				finishing = (SharpIpp.Model.Finishings)Enum.Parse(typeof(SharpIpp.Model.Finishings), printingFinishings.Text),
				mediaAttributes = printingPaperAttributes.Text,
			};

			List<Printer> printersToSendTo = new();
			List<FileInfo> jobsToSend = new();
			foreach (string file in printingListPrinters.SelectedItems)
			{
				printersToSendTo.Add(Printer.ReadFromFile(new(pathToSavePrintersTo + "\\" + file))); //creates a printer for all selected printers
			}
			foreach (string file in printingListJobs.SelectedItems)
			{
				jobsToSend.Add(new(pathToSaveJobsTo + "\\" + file));
			}



			if (jobsToSend.Count > 1)
			{

			}

			foreach (Printer tempPrinter in printersToSendTo)
			{
				foreach (FileInfo job in jobsToSend)
				{
					printedJobs.Add(await tempPrinter.TrySendJob(job, myParams));
				}
			}
		}

		private async void printingPrintTemplate_Click(object sender, RoutedEventArgs e)
		{
			if(printingListPrinters.Items.Count == 0) { return; }
			PrintJob.JobParams myParams = new()
			{
				media = printingMedia.Text,
				duplexing = printingDuplex.Text,
				sourceTray = printingMediaSource.Text,
				outputTray = printingOutputBin.Text,
				copies = int.Parse(printingCopies.Text),
				collation = printingCollate.Text,
				finishing = (SharpIpp.Model.Finishings)Enum.Parse(typeof(SharpIpp.Model.Finishings), printingFinishings.Text),
				mediaAttributes = printingPaperAttributes.Text,
			};
			string finished = "";

			foreach (int i in Enumerable.Range(0, int.Parse(printingPages.Text)))
			{
				finished += "/Helvetica findfont 12 scalefont setfont \r\n" +
							"clippath stroke\r\n" +
							"20 240 moveto\r\n" +
							$"(PrintTool ver : {Settings.Default.Version.ToString()}) show \r\n" +
							"20 220 moveto\r\n" +
							$"(Generated at : {DateTime.Now.ToString()}) show\r\n" +
							"20 200 moveto\r\n" +
							$"(Page : {i}) show\r\n" +
							"20 180 moveto\r\n" +
							$"(Media : {printingMedia.Text}) show\r\n" +
							"20 160 moveto\r\n" +
							$"(Paper : {printingPaperAttributes.Text}) show\r\n" +
							"20 140 moveto\r\n" +
							$"(Finishing : {printingFinishings.Text}) show\r\n" +
							"20 120 moveto\r\n" +
							$"(Collation : {printingCollate.Text}) show\r\n" +
							"20 100 moveto\r\n" +
							$"(Duplexing : {printingDuplex.Text}) show\r\n" +
							"20 80 moveto\r\n" +
							$"(Source Tray : {printingMediaSource.Text}) show\r\n" +
							"20 60 moveto\r\n" +
							$"(Output Tray : {printingOutputBin.Text}) show\r\n" +
							"20 40 moveto\r\n" +
							$"(Copies : {printingCopies.Text}) show\r\n" +
							"20 20 moveto\r\n" +
							$"(Have a good day {Environment.UserName}:\\)) show\r\n" +
							"showpage\r\n";
			}
			await File.WriteAllTextAsync(pathToSaveJobsTo.FullName + "\\temp.ps", finished);

			List<Printer> printersToSendTo = new();
			foreach (string file in printingListPrinters.SelectedItems)
			{
				printersToSendTo.Add(Printer.ReadFromFile(new(pathToSavePrintersTo + "\\" + file))); //creates a printer for all selected printers
			}
			foreach (Printer tempPrinter in printersToSendTo)
			{
				printedJobs.Add(await tempPrinter.TrySendJob(new(pathToSaveJobsTo.FullName + "\\temp.ps"), myParams));
			}
		}

		private void openPathToJobs(object sender, RoutedEventArgs e)
		{
			Helper.OpenPath(pathToSaveJobsTo.FullName);
		}

		private async void printingCancelJobs_Click(object sender, RoutedEventArgs e)
		{
			foreach (string file in printingListPrinters.SelectedItems)
			{
				Printer p = Printer.ReadFromFile(new(pathToSavePrintersTo + "\\" + file));
				if (!await p.CancelJobs())
				{
					MessageBox.Show("Unable To Cancel Jobs", "Error When Cancelling", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void printingListPrinters_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			foreach (string file in printingListPrinters.SelectedItems)
			{
				Printer p = Printer.ReadFromFile(new(pathToSavePrintersTo + "\\" + file));
				printingMedia.ItemsSource = p.IPPSupportedMedia;
				printingDuplex.ItemsSource = p.IPPSupportedSides;
				printingMediaSource.ItemsSource = p.IPPSupportedMediaSource;
				printingOutputBin.ItemsSource = p.IPPSuppportedOutputBin;
				printingCollate.ItemsSource = p.IPPSupportedCollate;
				printingFinishings.ItemsSource = p.IPPSupportedFinishings;
				printingPaperAttributes.ItemsSource = p.IPPSupportedMediaType;
			}

			printingMedia.SelectedItem = "na_letter_8.5x11in";
			printingDuplex.SelectedIndex = 0;
			printingMediaSource.SelectedItem = "auto";
			printingOutputBin.SelectedIndex = 0;
			printingCollate.SelectedIndex = 0;
			printingFinishings.SelectedIndex = 0;
			printingPaperAttributes.SelectedItem = "stationery";
		}

		private async void RefreshJobsList(object sender, RoutedEventArgs e)
		{
			printingListJobs.ItemsSource = await Helper.PopulateFromPathOrSite(pathToSaveJobsTo.FullName);
		}



		#endregion IPP

		#region Testing

		bool currTestingUSB = false;
		private async void TestUSBSend_Click(object sender, RoutedEventArgs e)
		{
			if (currTestingUSB)
			{
				cancelSource.Cancel();
				cancelSource = new();
				testUSBSend.Background = System.Windows.Media.Brushes.DarkGreen;
				testUSBSend.Content = "Test USB Send";
				currTestingUSB = false;
			}
			else
			{
				currTestingUSB = true;
				testUSBSend.Background = System.Windows.Media.Brushes.DarkRed;
				testUSBSend.Content = "Cancel";
				var tok = cancelSource.Token;
				ProcessHandler usbSend = new("Services\\USBSend.exe", @"Data\template.ps", ptlog);
				await usbSend.Start(tok);
				testUSBSend.Background = System.Windows.Media.Brushes.DarkGreen;
				testUSBSend.Content = "Test USB Send";
				currTestingUSB = false;
			}
		}



		private async void TestQscan_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process pc = new();
			pc.StartInfo.RedirectStandardInput = true;

			pc.StartInfo.FileName = "cmd";
			pc.StartInfo.RedirectStandardInput = true;

			pc.Start();

			pc.StandardInput.WriteLine(@"Services\QScanWS.exe");
			await Task.Delay(1000);
			pc.StandardInput.WriteLine("Ver=2010-01-29");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("ProductName=JediColorMFP");
			await Task.Delay(100);
			pc.StandardInput.WriteLine($"FormatterIP=");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("JobType=Copy");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("InputMediaSize=Letter");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("OutputMediaSource=Tray2");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("Sides=SimplexToSimplex");
			await Task.Delay(100);
			pc.StandardInput.WriteLine("Start");


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
				connectSerialButton.Background = System.Windows.Media.Brushes.DarkRed;
				serialConnected = true;
				await ptlog.Log("Finished");
			}
			else
			{
				await ptlog.Log("Disconnecting from serial connections....");
				foreach (SerialConnection sc in serialConnections) { sc.Close(); }
				connectSerialButton.Content = "Connect to Serial";
				connectSerialButton.Background = System.Windows.Media.Brushes.DarkGreen;
				serialConnected = false;
				await ptlog.Log("Finished");
			}

		}
		//bool snoopyConnected = false;
		private void ConnectSnoopy_Click(object sender, RoutedEventArgs e)
		{
			//TODO
		}
		bool telnetConnected = false;

		private async void ConnectTelnet_Click(object sender, RoutedEventArgs e)
		{
			if (!telnetConnected)
			{
				IPEntry ipgetter = new();
				ipgetter.ShowDialog();
				if(ipgetter.DialogResult == false)
				{
					return;
				}
				await ptlog.Log("Connecting to telnet connections...");
				telnetConnections.Clear();
				telnetConnectionsTabControl.Items.Clear();
				foreach (int port in TelnetConnection.GetPorts())
				{
					await ptlog.Log("Connecting to " + port);
					TelnetConnection connection = new(ipgetter.ipaddress.Text, port);
					TabItem tab = new() { Content = connection, Header = port.ToString() };
					telnetConnectionsTabControl.Items.Add(tab);
					telnetConnections.Add(connection);
				}
				connectTelnetButton.Content = "Disconnect from Telnet";
				connectTelnetButton.Background = System.Windows.Media.Brushes.DarkRed;
				telnetConnected = true;
				await ptlog.Log("Finished");

			}
			else
			{
				await ptlog.Log("Disconnecting from Telnet connections....");
				foreach (TelnetConnection tc in telnetConnections) { tc.Close(); }
				connectTelnetButton.Content = "Connect to Telnet";
				connectTelnetButton.Background = System.Windows.Media.Brushes.DarkGreen;
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



		#endregion Testing


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
