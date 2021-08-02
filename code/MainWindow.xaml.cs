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

        private async void LoadTask(object sender, EventArgs e)
        {
            await Tasks.runStartUp();
            Tasks.PopulateListBox(savedConnections, "Data\\Connections\\");
            Tasks.PopulateListBox(savedPrintJobs, "Data\\Jobs\\");
            if (Tasks.checkHPStatus())
            {
                MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
                firmwareTab.IsEnabled = false;
            }
            else
            {
                await Tasks.PopulateComboBox(duneVersions, DUNESITE + "?C=M;O=D");
            }
            loggers.Add(new Logger("application"));
        }

        private async void ClosingTask(object sender, System.ComponentModel.CancelEventArgs e)
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

            connectionsModel.Text = data[0];
            connectionsIpPrinterEntry.Text = data[1];
            connectionsIpDartEntry.Text = data[2];
            connectionsTypeNone.IsChecked = bool.Parse(data[3]);
            connectionsTypeSerial.IsChecked = bool.Parse(data[4]);
            connectionsTypeDart.IsChecked = bool.Parse(data[5]);
            connectionsBashSelect.Text = data[8];
            connectionsSirusSelect.Text = data[9];
            connectionsOptSelect.Text = data[10];
            connectionsPort1.Text = data[11];
            connectionsPort2.Text = data[12];
            connectionsPort3.Text = data[13];
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

        

        private async void YoloUpdateDaily(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Tasks.PopulateComboBox(yoloVersions, YOLOSITE + yoloProducts.Text);
            await Tasks.PopulateComboBox(yoloPackages, YOLOSITE + yoloProducts.Text + yoloVersions.Text + yoloDistros.Text);
        }


        public async void YoloUpdateCustom(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
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
       
        
        
        

        
       
        
        
        private async void DuneUpdateDaily(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Tasks.PopulateComboBox(duneProducts, DUNESITE + duneVersions.Text);
            await Tasks.PopulateComboBox(dunePackages, DUNESITE + duneVersions.Text + duneProducts.Text);
        }


        public async void DuneUpdateCustom(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await Tasks.PopulateComboBox(duneCustomPackages, firmwareYoloCustomEntry.Text);
        }

        public void firmewareDuneUnsecure_Click(object sender, RoutedEventArgs e)
        {

        }

        public void firmewareDuneSecure_Click(object sender, RoutedEventArgs e)
        {

        }

        public void firmewareDuneReset_Click(object sender, RoutedEventArgs e)
        {
            // todo WORK ONM THIS
        }


        
        
        public async void firmwareNormalSend(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            if (yoloTab.IsSelected)
            {
                if (yoloDailyTab.IsSelected)
                {
                    await Firmware.downloadAndSendUSB(yoloPackages.Text, YOLOSITE + yoloProducts.Text + yoloVersions.Text + yoloDistros.Text, loggers[0], yoloInstallButton,cancelToken);
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
           
        public void firmwareCancel(object sender, RoutedEventArgs e)
        {
            cancelSource.Cancel();
        }
        

        #endregion Firmware

        #region Printing tab 
        private async Task<List<string>> generateArgs()
        {
            string sendType = "";
            if (psButton.IsChecked == true) { sendType = "1"; }
            if (pclButton.IsChecked == true) { sendType = "2"; }
            if (escpButton.IsChecked == true) { sendType = "3"; }

            string duplex = "OFF";
            string duplexMode = "";
            if (simplexButton.IsChecked == true) { duplex = "ON"; }
            if (duplexLEButton.IsChecked == true) { duplex = "ON"; duplexMode = "LONGEDGE"; }
            if (duplexSEButton.IsChecked == true) { duplex = "ON"; duplexMode = "SHORTEDGE"; }

            List<string> args = new();
            args.Add("temp.ps"); //filename
            args.Add("PrintTool Selection Send"); //jobname
            args.Add(sendType); //what language
            args.Add(printPages.Text); // amoount of pages
            args.Add(duplex); // duplexing on or off
            args.Add(duplexMode); //duplexing selection
            args.Add(paperTypeSelection)
            return args;
        }

        public async void printSend9100Button(object sender, RoutedEventArgs e)
        {           

            await printSendIP(connectionsIpPrinterEntry.Text, await printGenerator());
        }
        public async void printSendUSBButton(object sender, RoutedEventArgs e)
        {
            await printSendIP(connectionsIpPrinterEntry.Text, await printGenerator());

        }

        public async void printSaveJob_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists( @"Data\Jobs\"+printNameJob.Text)) { File.Delete(@"Data\Jobs\" + printNameJob.Text); }
            File.Copy(await printGenerator(), @"Data\Jobs\" + printNameJob.Text);
            printSavedJobsRefresh();
        }

        public void printDeteleJob_Click(object sender, RoutedEventArgs e)
        {

            Tasks.ListBoxDelete(savedPrintJobs);
            catch { MessageBox.Show("Please select something first"); }
            Tasks.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
        }

        public async void printSendJob_Click(object sender, RoutedEventArgs e)
        {
            if(printSavedJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            await printSendIP(connectionsIpPrinterEntry.Text, printSavedJobs.SelectedItem.ToString());
        }

        #endregion

        
        

        #region Shared Tasks
        
        public void downloadProgressHandler(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            applicaitonProgressBar.Value = percentage;
        }
        public void downloadEndHandler(object sender, DownloadDataCompletedEventArgs e)
        {
            applicaitonProgressBar.Value = 0;
        }
        Process usbsend = new Process();



















        #endregion




    }
}
