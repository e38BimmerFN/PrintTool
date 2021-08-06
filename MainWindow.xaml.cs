﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;




namespace PrintTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
       
        Printer currPrinter = new();
        Logger appLogs = new("Application");
        Logger bashLogs = new("Bash");
        Logger sirusLogs = new("Sirus");
        Logger optLogs = new("Opt");


        const string YOLOSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
        const string DUNESITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        const string DUNEUTILITY = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";
        System.Threading.CancellationTokenSource cancelSource = new();

        #region Startup

        private async void LoadTrigger(object sender, EventArgs e)
        {
            Tasks.runStartUp();
            Tasks.PopulateListBox(savedPrinters, "Data\\Printers\\");
            Tasks.PopulateListBox(savedPrintJobs, "Data\\Jobs\\");
            if (!Tasks.checkHPStatus())
            {
                MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
                firmwareTab.IsEnabled = false;
            }           
            await Tasks.PopulateComboBox(duneVersions, DUNESITE + "?C=M;O=D");
            appLogs.AddTextBox(logsBottomApp);
            
        }

        private void ExitTrigger(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tasks.RunEndTasks();
        }
        #endregion Startup

        #region Connections Tab

        private async void connectionsIpPrinterEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (await Tasks.CheckIP(printerIPEntry.Text))
            {
                printerIPEntry.Background = System.Windows.Media.Brushes.LightGreen;
            }

            else
            {
                printerIPEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
            
        }

        private async void connectionsIpDartEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (await Tasks.CheckIP(dartIPEntry.Text))
            {
                printerIPEntry.Background = System.Windows.Media.Brushes.LightGreen;
            }

            else
            {
                printerIPEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
        }

        public void ConnectionsSaveDefaults(object sender, EventArgs e)
        {
            currPrinter.model = printerModel.Text;
            currPrinter.id = printerID.Text;
            currPrinter.engineType = printerEngine.Text;
            currPrinter.ip = printerIPEntry.Text;
            currPrinter.dart.isEnabled = enableDart.IsChecked ?? false;
            currPrinter.dart.usingPorts = enableDartSerial.IsChecked ?? false;
            currPrinter.dart.ip = dartIPEntry.Text;
            currPrinter.usingSerial = serialEnabled.IsChecked ?? false;
            currPrinter.Save();            
            Tasks.PopulateListBox(savedPrinters, "Data\\Printers\\");
        }
        public void ConnectionsLoadDefaults(object sender, EventArgs e)
        {
            currPrinter.Load(@"Data\Printers\" + savedPrinters.SelectedItem.ToString());
            printerModel.Text = currPrinter.model;
            printerID.Text = currPrinter.id;
            printerEngine.Text =currPrinter.engineType;
            printerIPEntry.Text = currPrinter.ip.ToString();
            enableDart.IsChecked = currPrinter.dart.isEnabled;
            enableDartSerial.IsChecked = currPrinter.dart.usingPorts;
            dartIPEntry.Text = currPrinter.dart.ip.ToString();
            serialEnabled.IsChecked = currPrinter.usingSerial;      
        }
        public void ConnectionsDeleteDefaults(object sender, EventArgs e)
        {
            File.Delete(@"Data\Printers\" + savedPrinters.SelectedItem);
            Tasks.PopulateListBox(savedPrinters, "Data\\Printers\\");
        }

       
        #endregion Connections

        #region Firmware Tab
        #region Yolo
        private async void yoloProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            await Tasks.PopulateComboBox(yoloVersions, YOLOSITE + yoloProducts.Text + "/?C=M;O=D");
        }

        private async void yoloVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);            
            await Tasks.PopulateComboBox(yoloDistros, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text);
        }

        private async void yoloDistros_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            await Tasks.PopulateComboBox(yoloPackages, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/?C=S;O=D", "fhx");

        }

        private async void firmwareYoloCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await Task.Delay(100);
            await Tasks.PopulateComboBox(yoloCustomPackages, yoloCustomEntry.Text,"fhx");
        }


       

        public async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
            List<string> results = await Tasks.getListings(website);
            string filename = "";
            if (yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
            await Firmware.DLAndSend(filename,website,appLogs,firmwareYoloSecureB, cancelToken);
            firmwareYoloUnsecureB.Content = "Convert to unsecure";
        }
        public async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
            List<string> results = await Tasks.getListings(website);
            string filename = "";
            if(yoloProducts.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
            await Firmware.DLAndSend(filename, website, appLogs, firmwareYoloSecureB, cancelToken);
            firmwareYoloSecureB.Content = "Convert to secured";
        }

        #endregion Yolo
        #region Dune
        

        private async void duneVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            if (duneVersions.Text == "") { return; }
            await Tasks.PopulateComboBox(duneProducts, DUNESITE + duneVersions.Text);
        }

        private async void duneProducts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            if(duneVersions.Text =="" || duneProducts.Text == "") { return; }
            await Tasks.PopulateComboBox(dunePackages, DUNESITE + duneVersions.Text + duneProducts.Text + "/?C=S;O=D", "fhx");
        }

        private async void firmwareDuneCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if (duneCustomEntry.Text == "" || !duneCustomEntry.Text.EndsWith("/")) { return; }
            await Tasks.PopulateComboBox(duneCustomPackages, duneCustomEntry.Text, "fhx");
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

        #endregion Dune


        private async void firmwareUSBSend(object sender, RoutedEventArgs e)
        {
            System.Threading.CancellationToken cancelToken = cancelSource.Token;
            if (yoloTab.IsSelected)
            {
                if (yoloDailyTab.IsSelected)
                {
                    await Firmware.DLAndSend(yoloPackages.Text, YOLOSITE + yoloProducts.Text + "/" + yoloVersions.Text + yoloDistros.Text + "/", appLogs, yoloInstallButton,cancelToken);
                }
                else
                {
                    await Firmware.DLAndSend(yoloCustomPackages.Text, yoloCustomEntry.Text, appLogs, yoloInstallButton, cancelToken);
                }
            }
            else
            {
                if (duneDailyTab.IsSelected)
                {
                    await Firmware.DLAndSend(dunePackages.Text, DUNESITE + duneVersions.Text + duneProducts.Text, appLogs, duneInstallButton, cancelToken);
                }
                else
                {
                    await Firmware.DLAndSend(duneCustomPackages.Text, yoloCustomEntry.Text, appLogs, duneInstallButton, cancelToken);
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
            string filename = PrintJob.PrintGenerator(generateArgs());
            await PrintJob.SendIP(printerIPEntry.Text, filename);
        }
        private async void printSendUSBButton(object sender, RoutedEventArgs e)
        {
            string filename = PrintJob.PrintGenerator(generateArgs());
            await PrintJob.SendUSB(filename);

        }

        private void printSaveJob_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists( @"Data\Jobs\"+printNameJob.Text)) { File.Delete(@"Data\Jobs\" + printNameJob.Text); }
            File.Copy(PrintJob.PrintGenerator(generateArgs()), @"Data\Jobs\" + printNameJob.Text);
            Tasks.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
        }

        private void printDeteleJob_Click(object sender, RoutedEventArgs e)
        {
            if(!File.Exists(@"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString()) ) { MessageBox.Show(savedPrintJobs.SelectedItem.ToString() + "Doesnt exist"); }
            File.Delete(@"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString());
            Tasks.PopulateListBox(savedPrintJobs, @"Data\Jobs\");
        }

        private async void printSendJob_Click(object sender, RoutedEventArgs e)
        {
            if(savedPrintJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            await PrintJob.SendIP(printerIPEntry.Text, @"Data\Jobs\" + savedPrintJobs.SelectedItem.ToString());
        }






        #endregion

        
    }
}
