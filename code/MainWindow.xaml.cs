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
                logTab.IsEnabled = false;
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
            Tasks.ListBoxRefresh(savedConnections, "Data\\Connections\\");
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
            Shared.ListBoxDelete(savedConnections);
            Shared.ListBoxRefresh(savedConnections, "Data\\Connections\\");
        }

        public void connnectionsTypeClick(object sender, RoutedEventArgs e)
        {
            if (connectionsTypeNone.IsChecked == true) { connectionsSerialGroup.Visibility = Visibility.Collapsed; connectionsDartGroup.Visibility = Visibility.Collapsed; }
            else if (connectionsTypeSerial.IsChecked == true) { connectionsSerialGroup.Visibility = Visibility.Visible; connectionsDartGroup.Visibility = Visibility.Collapsed; }
            else { connectionsSerialGroup.Visibility = Visibility.Collapsed; connectionsDartGroup.Visibility = Visibility.Visible; }
        }
        #endregion Connections

        #region Firmware Tab

        #region Jedi Firmware
        const string JEDIPATH = "\\\\jedibdlserver.boi.rd.hpicorp.net\\JediSystems\\Published\\DailyBuilds\\25s\\2021";
        public void loadJediFWList(object sender, RoutedEventArgs e)
        {
            foreach (string f in Directory.GetDirectories(JEDIPATH))
            {
                firmwareYoloVersion.Items.Clear();
                firmwareYoloVersion.Items.Add(f.Remove(0, JEDIPATH.Length));
            }
        }
        #endregion
        #region Yolo Firmware
        const string YOLOWEBSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";

        public async void firmwareYoloVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await firmwareYoloUpdatePackages();
        }
        public async void firmwareYoloCustomEntry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            await populateComboBox(firmwareYoloCustomPackage, firmwareYoloCustomEntry.Text);
        }
        public async void firmwareYoloSendFirmware_click(object sender, RoutedEventArgs e)
        {
            string path="", filename ="";

            if (firmwareYoloDailyTab.IsSelected)
            {
                path = firmwareYoloProduct.SelectionBoxItem + "/" + firmwareYoloVersion.SelectionBoxItem + "/" + firmwareYoloDist.SelectionBoxItem + "/";
                filename = firmwareYoloPackage.Text;
            }
            else
            {
                path = firmwareYoloCustomEntry.Text;
                filename = firmwareYoloCustomPackage.Text;
            }
            
            string fullpath = YOLOWEBSITE + path + filename;
            loggers[0].Log("Downloading " + fullpath);
            yoloInstallButton.IsEnabled = false;
            yoloInstallButton.Content = "working...";
            await firmwareDlSend(fullpath, filename);
            yoloInstallButton.IsEnabled = true;
            yoloInstallButton.Content = "Download & Install";

        }
        
        public async Task firmwareYoloUpdateRevisons()
        {
            firmwareYoloVersion.Items.Clear();
            string website = YOLOWEBSITE + firmwareYoloProduct.SelectionBoxItem + "/?C=M;O=D";
            List<string> results = await downloadWebsiteIndex(website);            
            foreach(string result in results) { firmwareYoloVersion.Items.Add(result); }
        }

        public async Task firmwareYoloUpdatePackages()
        {
            firmwareYoloPackage.Items.Clear();
            List<string> results = await downloadWebsiteIndex(YOLOWEBSITE + firmwareYoloProduct.SelectionBoxItem + "/" + firmwareYoloVersion.SelectionBoxItem + "/" + firmwareYoloDist.SelectionBoxItem + "/");
            foreach (string result in results) { firmwareYoloPackage.Items.Add(result); }
        }

        public async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloUnsecureB.IsEnabled = false;
            firmwareYoloUnsecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if (firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
            await firmwareDlSend(website, filename);
            firmwareYoloUnsecureB.IsEnabled = true;
            firmwareYoloUnsecureB.Content = "Convert to unsecure";
        }
        public async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloSecureB.IsEnabled = false;
            firmwareYoloSecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if(firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
            await firmwareDlSend(website, filename);
            
            firmwareYoloSecureB.IsEnabled = true;
            firmwareYoloSecureB.Content = "Convert to secured";
        }
        #endregion
        #region Dune Firmware
        //Dune
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        const string DUNEUTILITY = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";

        #region DuneUI
        public void firmwareDuneVersions_DropDownOpened(object sender, EventArgs e)
        {
            firmwareDuneModels.Items.Clear();
            firmwareDunePackages.Items.Clear();
        }
        public async void firmwareDuneModels_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdateModels();
        }
        public async void firmwareDunePackages_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdatePackages();
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
        #endregion Dune UI
        public async Task firmwareDuneUpdateRevisons()
        { 
            await populateComboBox(firmwareDuneVersions,DUNEWEBSITE + "/?C=M;O=D");
            await firmwareDuneUpdateModels();
            
        }
        public async Task firmwareDuneUpdateModels()
        {
            if (firmwareDuneVersions.Text == "") { return; }
            await populateComboBox(firmwareDuneModels, DUNEWEBSITE + firmwareDuneVersions.Text);
            if (firmwareDuneModels.Items.Count == 0) { MessageBox.Show("Version doesn't exist "); return; }
            await firmwareDuneUpdatePackages();
   
        }
        public async Task firmwareDuneUpdatePackages()
        {
            await populateComboBox(firmwareDunePackages, DUNEWEBSITE + firmwareDuneVersions.Text + firmwareDuneModels.Text);
            foreach (string package in firmwareDunePackages.Items)
            {
                if (package.Contains("fhx")) { firmwareDunePackages.Items.Remove(package); }
            }
        }
        public async void firmwareDuneSend(object sender, RoutedEventArgs e)
        {
            string website = "";
            string filename = "";
            if (firmwareDuneCustomTab.IsSelected)
            {
                 website = firmwareDuneCustomEntry.Text;
                filename = firmwareDuneCustomPackage.Text;
            }
            else
            {
                website = DUNEWEBSITE + firmwareDuneVersions.Text + "/" + firmwareDuneModels.Text + "/";
                filename = firmwareDunePackages.Text;
            }
            string fullpath = website + filename;
            duneInstallButton.IsEnabled = false;
            duneInstallButton.Content = "working...";
            await firmwareDlSend(fullpath, filename);
            duneInstallButton.IsEnabled = true;
            duneInstallButton.Content = "Download & Install";
        }

        #endregion
        #region Shared Functions  
         
        public async Task populateComboBox(System.Windows.Controls.ComboBox comboBox, string site)
        {
            comboBox.Items.Clear();
            
            //allowing both http scraping or directory
            if (site.Contains("http"))
            {
                List<string> results = await downloadWebsiteIndex(site);
                foreach(string result in results) { comboBox.Items.Add(result + "/"); }
            }
            else if(site.Contains(@"\\"))
            {
                string[] results = Directory.GetFiles(site);
                if(results.Length == 0) { comboBox.Items.Add("Nothing found"); }
                foreach (string result in results){ comboBox.Items.Add(result + "\\"); }
            }
            else
            {
                MessageBox.Show(site + " Is not valid");
            }
            
        }
        
        public async Task firmwareDlSend(string filename, string path)
        {
            loggers[0].Log("Downloading " + path + filename);
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgressHandler);
            client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(downloadEndHandler);
            await client.DownloadFileTaskAsync(path, filename);
            loggers[0].Log("Download success.");
            loggers[0].Log("Sending firmware to printer");
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = filename;
            usbsend.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            usbsend.StartInfo.RedirectStandardOutput = true;
            usbsend.Start();
            await Task.Delay(1000);

            
            await usbsend.WaitForExitAsync();
            if (usbsend.ExitCode == 0)
            {
                MessageBox.Show("Firmware upgrade success!");
            }
            else
            {
                MessageBox.Show("Firmware upgrade error / canceled");
            }
            File.Delete(filename);
            try { usbsend.Close(); }
            catch {}
        }
        public async Task logOutput(StreamReader streamReader, System.Windows.Controls.TextBox textBox)
        {
            while(streamReader.ReadLine() != null)
            {
                textBox.AppendText(await streamReader.ReadLineAsync());
            }
        }
        public async Task<List<string>> downloadWebsiteIndex(string website)
        {
            List<string> results = new();
            List<string> resultsPartial = new();
            string websiteData = "";
            string patternA = "(?<=<a href=\")(.*)(?=\\/a><\\/td>)";
            string patternText = "(?<=\">)(.*)(?=<)";
            Regex regexATag = new Regex(patternA);
            Regex regexText = new Regex(patternText);
            try
            {
                WebClient client = new WebClient();
                websiteData = await client.DownloadStringTaskAsync(website);
            }
            catch
            {
                MessageBox.Show("Error: Specified site is invalid.");
                loggers[0].Log("Error: Specified site is invalid.");
            }
            MatchCollection matches = regexATag.Matches(websiteData);
            foreach (Match match in matches) { resultsPartial.Add(match.Value); }
            foreach (string match in resultsPartial) { results.Add(regexText.Match(match).Value); }
            
            results.RemoveAt(0);
            
            int resultstokeep = 100;
            try { results.RemoveRange(resultstokeep, results.Count- resultstokeep); }
            catch { }
            if (results.Count == 0)
            {
                results.Add("Found no listings for current selection.");
            }
            return results;
        }     
        public void firmwareUSBDestroy(object sender, RoutedEventArgs e)
        {
            try
            {
                usbsend.Kill();
            }
            catch
            {

            }
        }
        #endregion Shared Functions

        #endregion Firmware

        #region Printing tab 
        #region UI
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

            try
            {
                File.Delete(printSavedJobs.SelectedItem.ToString());
            }
            catch { MessageBox.Show("Please select something first"); }
            printSavedJobsRefresh();
        }

        public async void printSendJob_Click(object sender, RoutedEventArgs e)
        {
            if(printSavedJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            await printSendIP(connectionsIpPrinterEntry.Text, printSavedJobs.SelectedItem.ToString());
        }

        #endregion

        public void printSavedJobsRefresh()
        {
            printSavedJobs.Items.Clear();
            string[] filenames = Directory.GetFiles(@"Data\Jobs\");
            foreach (string filename in filenames)
            {
                printSavedJobs.Items.Add(filename);
            }
            

        }

        public async Task<string> printGenerator()
        {
            string filename = "temp.ps";
            string jobname = "\"PrintTool Selection Send\"";
            int pages = int.Parse(printPages.Text);
            string start = await PJLStart(jobname);
            string middle = await printGeneratorCaller(pages);
            string end = await PJLEnd(jobname);

            
            string alltogether =  start +  middle + end;
            if (File.Exists(filename)) { File.Delete(filename); }
            StreamWriter tempFile = File.CreateText(filename);
            tempFile.Write(alltogether);
            tempFile.Close();
            return filename;
            
            
        }

        public async Task<string> PJLStart(string jobname)
        {
            
            string duplexOnOrOff = "OFF";
            string duplexBinding = "SHORTEDGE";

            if(duplexLEButton.IsChecked == true || duplexSEButton.IsChecked == true) { duplexOnOrOff = "ON"; }
            if (duplexLEButton.IsChecked == true) { duplexBinding = "LONGEDGE"; }
            string pjlPart = "";
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "%-12345X";
            pjlPart = pjlPart + escapeSequence + "@PJL\r\n"
                + "@PJL RESET\r\n"
                + "@PJL JOB NAME = " + jobname + "\r\n"
                + "@PJL SET JOBNAME = " + jobname + "\r\n"
                + "@PJL SET DUPLEX = " + duplexOnOrOff + "\r\n"
                + "@PJL SET BINDING = " + duplexBinding + "\r\n"
                + "@PJL SET COPIES = " + printCopies.Text + "\r\n"
                + "@PJL SET PAPER = " + paperTypeSelection.Text + "\r\n"
                + "@PJL SET OUTBIN = " + printOutputTray.Text + "\r\n"
                + "@PJL SET MEDIASOURCE = " + printSourceTray.Text+"\r\n";
            return pjlPart;
        }

        public async Task<string> PJLEnd(string jobname)
        {
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "%-12345X";
            string endpart = escapeSequence + "@PJL\r\n"
                + "@PJL EOJ NAME = " + jobname +"\r\n"
                + escapeSequence+"\r\n";

            return endpart;
        }

        public async Task<string> printGeneratorCaller(int pages)
        {
            string output = "";
            if (psButton.IsChecked == true)
            {
                output = await printCreatePS(pages);
            }
            else if (pclButton.IsChecked == true)
            {
                output = await printCreatePCL(pages);
            }
            else if (escpButton.IsChecked == true)
            {
                output = await printCreateESCP(pages);
            }
            return output;
        }

        public async Task<string> printCreatePCL(int pages)
        {   
            return "";
        }
        public async Task<string> printCreatePS(int pages)
        {
            string output = "@PJL ENTER LANGUAGE=POSTSCRIPT \r\n" + "/Times-Roman findfont 14 scalefont setfont \r\n";

            for(int i =0; i<pages; i++)
            {
                output = output
                    + "clippath stroke\r\n"
                    + "15 60 moveto\r\n"
                    + "(PrintTool) show\r\n"
                    + "20 40 moveto \r\n"
                    + "( Page number = " + (i+1) + " ) show\r\n"
                     + "20 20 moveto \r\n"
                    + "( Source PC name  = " + Environment.MachineName + " ) show\r\n"
                    + "showpage\r\n";

            }
            return output;
        }

        public async Task<string> printCreateESCP(int pages)
        {
            return "";
        }

        public async Task printSendIP(string ip, string file)
        {
            byte[] data = new byte[0];
            try
            {
                data = File.ReadAllBytes(file);
            }
            catch
            {
                MessageBox.Show("File read error");
                return;
            }
            
            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(ip, 9100);
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                client.Close();
            }
            catch
            {
                MessageBox.Show("Incorrect IP");
                return;
            }
            

            return;
        }
        public async Task printSendUSB(string file)
        {
            Process usbsend = new();
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = file;
            usbsend.StartInfo.CreateNoWindow = true;
            usbsend.Start();        
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
