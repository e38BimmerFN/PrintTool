using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using SharpIpp.Model;
using SharpIpp.Exceptions;
using SharpIpp;



namespace PrintTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        List<Logger> loggers = new();
        const string YOLOSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
        const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        const string DUNEUTILITY = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";
        System.Threading.CancellationTokenSource cancelSource = new();

        #region Startup

        private async void LoadTrigger(object sender, EventArgs e)
        {
            Tasks.runStartUp();
            Tasks.PopulateListBox(savedConnections, "Data\\Connections\\");
            Tasks.PopulateListBox(savedPrintJobs, "Data\\Jobs\\");
            if (!Tasks.checkHPStatus())
            {
                MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
                firmwareTab.IsEnabled = false;
            }
            else
            {
                await Tasks.PopulateComboBox(duneVersions, DUNESITE + "?C=M;O=D");
            }
            loggers.Add(new Logger("application"));
            loggers[0].AddTextBox(logsBottomApp);
            
        }

        private void ExitTrigger(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tasks.RunEndTasks();
        }
        #endregion Startup

        #region Connections Tab

        private void connectionsIpPrinterEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Connections.checkIP(connectionsIpPrinterEntry);
        }

        private void connectionsIpDartEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Connections.checkIP(connectionsIpPrinterEntry);
        }

        public void ConnectionsSaveDefaults(object sender, EventArgs e)
        {
            List<string> items = new();
            items.Add(connectionsModel.Text);
            items.Add(connectionsIpPrinterEntry.Text);
            items.Add(connectionsIpDartEntry.Text);
            items.Add(connectionsTypeNone.IsChecked.ToString());
            items.Add(connectionsTypeSerial.IsChecked.ToString());
            items.Add(connectionsTypeDart.IsChecked.ToString());
            items.Add(connectionsBashSelect.Text);
            items.Add(connectionsSirusSelect.Text);
            items.Add(connectionsOptSelect.Text);
            items.Add(connectionsPort1.Text);
            items.Add(connectionsPort2.Text);
            items.Add(connectionsPort3.Text);
            Connections.SaveDefaults(items);
            Tasks.PopulateListBox(savedConnections, "Data\\Connections\\");
        }
        public void ConnectionsLoadDefaults(object sender, EventArgs e)
        {
            List<string> data = Connections.LoadDefaults(savedConnections);
            if (data.Count == 0) { return; }

            connectionsModel.Text = data[0];
            connectionsIpPrinterEntry.Text = data[1];
            connectionsIpDartEntry.Text = data[2];
            connectionsTypeNone.IsChecked = bool.Parse(data[3]);
            connectionsTypeSerial.IsChecked = bool.Parse(data[4]);
            connectionsTypeDart.IsChecked = bool.Parse(data[5]);
            connectionsBashSelect.Text = data[6];
            connectionsSirusSelect.Text = data[7];
            connectionsOptSelect.Text = data[8];
            connectionsPort1.Text = data[9];
            connectionsPort2.Text = data[10];
            connectionsPort3.Text = data[11];
        }
        public void ConnectionsDeleteDefaults(object sender, EventArgs e)
        {
            Tasks.ListBoxDelete(savedConnections);
            Tasks.PopulateListBox(savedConnections, "Data\\Connections\\");
        }

        public void connnectionsTypeClick(object sender, RoutedEventArgs e)
        {
            if (connectionsTypeNone.IsChecked == true) { connectionsSerialGroup.Visibility = Visibility.Collapsed; connectionsDartGroup.Visibility = Visibility.Collapsed; }
            else if (connectionsTypeSerial.IsChecked == true) { connectionsSerialGroup.Visibility = Visibility.Visible; connectionsDartGroup.Visibility = Visibility.Collapsed; }
            else { connectionsSerialGroup.Visibility = Visibility.Collapsed; connectionsDartGroup.Visibility = Visibility.Visible; }
        }
        #endregion Connections

        #region Firmware Tab

        private async void yoloProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            await Tasks.PopulateComboBox(yoloVersions, YOLOSITE + yoloProducts.Text + "/?C=M;O=D");
        }

        private async void yoloVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            if (yoloVersions.Text == "") { return; }
            await Tasks.PopulateComboBox(yoloPackages, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/?C=S;O=D");
        }

        private async void yoloDistros_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            if(yoloVersions.Text == "") { return; }
            await Tasks.PopulateComboBox(yoloPackages, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/?C=S;O=D");

        }

       


        public async void YoloUpdateCustom(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if(yoloCustomEntry.Text == "" || yoloCustomEntry.Text.EndsWith("\\") == false) { return; }
            await Tasks.PopulateComboBox(yoloCustomPackages, firmwareYoloCustomEntry.Text);
        }

        public async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
            List<string> results = await Tasks.DownloadWebsiteIndex(website);
            string filename = "";
            if (yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
            await Firmware.downloadAndSendUSB(filename,website,loggers[0],firmwareYoloSecureB, cancelToken);
            firmwareYoloUnsecureB.Content = "Convert to unsecure";
        }
        public async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
            List<string> results = await Tasks.DownloadWebsiteIndex(website);
            string filename = "";
            if(yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
            await Firmware.downloadAndSendUSB(filename, website, loggers[0], firmwareYoloSecureB, cancelToken);
            firmwareYoloSecureB.Content = "Convert to secured";
        }





        private async void duneProducts_DropDownOpened(object sender, EventArgs e)
        {
            await Tasks.PopulateComboBox(duneProducts, DUNESITE + duneVersions.Text);
        }

        private async void dunePackages_DropDownOpened(object sender, EventArgs e)
        {
            await Tasks.PopulateComboBox(dunePackages, DUNESITE + duneVersions.Text + duneProducts.Text + "/?C=S;O=D","fhx");
        }

        private async void duneCustomPackages_DropDownOpened(object sender, EventArgs e)
        {
            await Tasks.PopulateComboBox(duneCustomPackages, firmwareDuneCustomEntry.Text, "fhx");
        }   
        


        private void firmewareDuneUnsecure_Click(object sender, RoutedEventArgs e)
        {

        }

        private void firmewareDuneSecure_Click(object sender, RoutedEventArgs e)
        {

        }

        private void firmewareDuneReset_Click(object sender, RoutedEventArgs e)
        {
            // todo WORK ONM THIS
        }


        
        
        private async void firmwareUSBSend(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            if (yoloTab.IsSelected)
            {
                if (yoloDailyTab.IsSelected)
                {
                    await Firmware.downloadAndSendUSB(yoloPackages.Text, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/", loggers[0], yoloInstallButton,cancelToken);
                }
                else
                {
                    await Firmware.downloadAndSendUSB(yoloCustomPackages.Text, firmwareYoloCustomEntry.Text, loggers[0], yoloInstallButton, cancelToken);
                }
            }
            else
            {
                if (duneDailyTab.IsSelected)
                {
                    await Firmware.downloadAndSendUSB(dunePackages.Text, DUNESITE + duneVersions.Text + duneProducts.Text, loggers[0], duneInstallButton, cancelToken);
                }
                else
                {
                    await Firmware.downloadAndSendUSB(duneCustomPackages.Text, firmwareYoloCustomEntry.Text, loggers[0], duneInstallButton, cancelToken);
                }
            }
        }
           
        private void firmwareCancel(object sender, RoutedEventArgs e)
        {
            cancelSource.Cancel();
        }
        

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
            string filename = Printer.PrintGenerator(generateArgs());
            await Printer.SendIP(connectionsIpPrinterEntry.Text, filename);
        }
        private async void printSendUSBButton(object sender, RoutedEventArgs e)
        {
            string filename = Printer.PrintGenerator(generateArgs());
            await Printer.SendUSB(filename);

        }

        private void printSaveJob_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists( @"Data\Jobs\"+printNameJob.Text)) { File.Delete(@"Data\Jobs\" + printNameJob.Text); }
            File.Copy(Printer.PrintGenerator(generateArgs()), @"Data\Jobs\" + printNameJob.Text);
            Tasks.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
        }

        private void printDeteleJob_Click(object sender, RoutedEventArgs e)
        {
            Tasks.ListBoxDelete(savedPrintJobs);
            Tasks.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
        }

        private async void printSendJob_Click(object sender, RoutedEventArgs e)
        {
            if(savedPrintJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            await Printer.SendIP(connectionsIpPrinterEntry.Text, savedPrintJobs.SelectedItem.ToString());
        }




        #endregion

      
    }
}
