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

        #region Startup
        private async void StartupTasks(object sender, EventArgs e)
        {
            if (checkHPStatus())
            {
                checkForUpdates();
                firmwareYoloUpdateRevisons();
                firmwareDuneUpdateRevisons();
                
                checkExePath();
               
            }
            createSubFolders();
            setDefaults();
            connectionsConfigRefresh();
            printSavedJobsRefresh();



            //SharpIppClient client = new();
            //var printerUri = new Uri("ipp://192.168.0.234:631");
            //var request = new GetPrinterAttributesRequest { PrinterUri = printerUri };
            //var response = await client.GetPrinterAttributesAsync(request);

            //pgLog(response.ToString());
        }
        private void createSubFolders()
        {
            Directory.CreateDirectory("Data/Connections");
            Directory.CreateDirectory("Data/Logs/Temp");
            Directory.CreateDirectory("Data/Logs/Captured");
            Directory.CreateDirectory("Data/Jobs/");
        }
        private void setDefaults()
        {
            connectionsIpPrinterEntry.Text = Settings.Default.PrinterIp;
            connectionsIpDartEntry.Text = Settings.Default.DartIp;
        }
        private bool checkHPStatus()
        {
            if (File.Exists(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\@Shared\PrintToolVersion.txt"))
            {
                return true;
            }
            else
            {
                MessageBox.Show("Attention! You are not connected or do not have access to required files. The tabs needing these resources will be disabled");
                firmwareTab.IsEnabled = false;
                logTab.IsEnabled = false;
                return false;
            }
        }
        private void checkForUpdates()
        {

            StreamReader sr = File.OpenText(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\versionAndNotes.txt");

            int version =  int.Parse(sr.ReadLine());
            sr.Close();
            if (Settings.Default.Version < version)
            {
                MessageBox.Show(@"Please update this tool, navigate to \DevScratch\Derek\PrintTool and run the executable.");
            }
        }
        private void checkExePath()
        {
            
            if(Directory.GetCurrentDirectory().Contains( @"\DevScratch\Derek\PrintTool"))
            {
                var result = MessageBox.Show("This program will now install itself into : " + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\", "Please click yes or no", MessageBoxButton.OKCancel);
                if(result == MessageBoxResult.Cancel)
                {
                    MessageBox.Show("Exiting.");
                    this.Close();
                }
                string copyfrompath = @"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\";
                
                string copytopath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\";
                Directory.CreateDirectory(copytopath);
                List<string> filestoCopy = new();
                
                filestoCopy.Add("PrintTool.exe");
                filestoCopy.Add("USBSend.exe");
                foreach(string file in filestoCopy) 
                {
                    if (File.Exists(copytopath + file)) { File.Delete(copytopath + file); }
                    File.Copy(copyfrompath + file, copytopath + file); 
                }
                MessageBox.Show("Sucessfully installed. now closing.");
                Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\");
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\PrintTool.exe");
                this.Close();
                
                


            }
        }


        private void EndTasks(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Save();
            try
            {
                usbsend.Kill();
            }
            catch { }
        }

        #endregion
        #region Connections Tab

        #region UI Logic
        private void connectionsPrinterIPCheck(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string regexmatch = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$";
            var myRegex = Regex.Match(connectionsIpPrinterEntry.Text, regexmatch);
            if (myRegex.Success)
            {
                connectionsIpPrinterEntry.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                connectionsIpPrinterEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
            Settings.Default.PrinterIp = connectionsIpPrinterEntry.Text;
            Settings.Default.Save();
        }
        private void connectionsDartIPCheck(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string regexmatch = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$";
            var myRegex = Regex.Match(connectionsIpDartEntry.Text, regexmatch);
            if (myRegex.Success)
            {
                connectionsIpDartEntry.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                connectionsIpDartEntry.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
            Settings.Default.DartIp = connectionsIpDartEntry.Text;

        }
        private void connnectionsTypeClick(object sender, RoutedEventArgs e)
        {
            if(connectionsTypeNone.IsChecked == true) { connectionsSerialGroup.Visibility=Visibility.Collapsed; connectionsDartGroup.Visibility=Visibility.Collapsed;}
            else if (connectionsTypeSerial.IsChecked == true) { connectionsSerialGroup.Visibility = Visibility.Visible; connectionsDartGroup.Visibility = Visibility.Collapsed; }
            else {  connectionsSerialGroup.Visibility = Visibility.Collapsed; connectionsDartGroup.Visibility = Visibility.Visible; }
        }
        private void connectionsSaveDefaults(object sender, RoutedEventArgs e)
        {
            if (connectionsModel.Text == "")
            {
                MessageBox.Show("Please select something");
                return;
            }
            string dataToSave =
                  connectionsModel.Text + ","
                + connectionsIpPrinterEntry.Text + ","
                + connectionsIpDartEntry.Text + ","
                + connectionsTypeNone.IsChecked + ","
                + connectionsTypeSerial.IsChecked + ","
                + connectionsTypeDart.IsChecked + ","
                + connectionsSerialGroup.Visibility.ToString() + ","
                + connectionsDartGroup.Visibility.ToString() + ","
                + connectionsBashSelect.Text + ","
                + connectionsSirusSelect.Text + ","
                + connectionsOptSelect.Text + ","
                + connectionsPort1.Text + ","
                + connectionsPort2.Text + ","
                + connectionsPort3.Text;
            StreamWriter myFile = File.CreateText("Data/Connections/"+connectionsModel.Text);
            myFile.WriteLine(dataToSave);
            myFile.Close();
            connectionsConfigRefresh();
        }

        private void LoadDefaults(object sender, RoutedEventArgs e)
        {
            List<string> data = new(File.ReadAllText(SavedConnectionConfigs.SelectedItem.ToString()).Split(","));

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
        private void DefaultsDelete(object sender, RoutedEventArgs e)
        {
            string path = SavedConnectionConfigs.SelectedItem.ToString();
            File.Delete(path);
            connectionsConfigRefresh();

        }

        #endregion


        private void connectionsConfigRefresh()
        {          
            string[] filenames = Directory.GetFiles("Data/Connections");
            SavedConnectionConfigs.Items.Clear();
            foreach (string filename in filenames)
            {
                SavedConnectionConfigs.Items.Add(filename);
            }   
        }
        





        #endregion
        #region Firmware Tab
        #region Jedi Firmware
        const string JEDIPATH = "\\\\jedibdlserver.boi.rd.hpicorp.net\\JediSystems\\Published\\DailyBuilds\\25s\\2021";
        private void loadJediFWList(object sender, RoutedEventArgs e)
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
        private async void firmwareYoloUpdateRevisons()
        {
            firmwareYoloVersion.Items.Clear();
            string website = YOLOWEBSITE + firmwareYoloProduct.SelectionBoxItem + "/?C=M;O=D";
            List<string> results = await downloadWebsiteIndex(website);            
            foreach(string result in results) { firmwareYoloVersion.Items.Add(result); }
        }
        private async void firmwareYoloSendVersion(object sender, RoutedEventArgs e)
        {

            string selection = firmwareYoloProduct.SelectionBoxItem + "/" + firmwareYoloVersion.SelectionBoxItem + "/" + firmwareYoloDist.SelectionBoxItem + "/";
            string filename = firmwareYoloProduct.SelectionBoxItem + "_" + firmwareYoloVersion.SelectionBoxItem + "_" + firmwareYoloDist.SelectionBoxItem + "_rootfs.fhx";
            if (firmwareYoloDist.SelectedIndex == 3)
            {
                filename = firmwareYoloProduct.SelectionBoxItem.ToString() + "_" + firmwareYoloVersion.SelectionBoxItem.ToString() + "_nonassert_appsigned_lbi_rootfs_secure_signed.fhx";
            }
            string fullpath = YOLOWEBSITE + selection + filename;
            pgLog("Downloading " + fullpath);
            yoloInstallButton.IsEnabled = false;
            yoloInstallButton.Content = "working...";
            await firmwareDownloadFile(fullpath, filename);
            await firmwareUSBSend(filename);
            yoloInstallButton.IsEnabled = true;
            yoloInstallButton.Content = "Download & Install";
        }
        private async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloUnsecureB.IsEnabled = false;
            firmwareYoloUnsecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if (firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
            await firmwareDownloadFile(website, filename);
            await firmwareUSBSend(filename);
            firmwareYoloUnsecureB.IsEnabled = true;
            firmwareYoloUnsecureB.Content = "Convert to unsecure";
        }
        private async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloSecureB.IsEnabled = false;
            firmwareYoloSecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if(firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
            await firmwareDownloadFile(website, filename);
            await firmwareUSBSend(filename);
            firmwareYoloSecureB.IsEnabled = true;
            firmwareYoloSecureB.Content = "Convert to secured";
        }
        #endregion
        #region Dune Firmware
        //Dune
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";




        private async void firmwareDuneModels_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdateModels();
        }

        private async void firmwareDunePackages_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdatePackages();
        }




        private async Task firmwareDuneUpdateRevisons()
        {
            firmwareDuneVersions.Items.Add(Settings.Default.DuneLastFW);
            List<string> versions = await downloadWebsiteIndex(DUNEWEBSITE + "/?C=M;O=D");
            foreach(string version in versions)
            {
                firmwareDuneVersions.Items.Add(version);
            }
            await firmwareDuneUpdateModels();
            
        }

        private async Task firmwareDuneUpdateModels()
        {
            if (firmwareDuneVersions.Text == "") { return; }
            List<string> models = await downloadWebsiteIndex(DUNEWEBSITE + firmwareDuneVersions.Text + "/");
            if (models.Count == 0) { MessageBox.Show("Version doesn't exist "); return; }
            firmwareDuneModels.Items.Clear();
      
            foreach (string model in models)
            {
                firmwareDuneModels.Items.Add(model);
            }
            await firmwareDuneUpdatePackages();
   
        }
        private async Task firmwareDuneUpdatePackages()
        {
            firmwareDunePackages.Items.Clear();
            List<string> dunePackage = await downloadWebsiteIndex(DUNEWEBSITE + firmwareDuneVersions.Text + firmwareDuneModels.Text);
            foreach (string package in dunePackage)
            {
                if (package.Contains("fhx")) { firmwareDunePackages.Items.Add(package); }
            }
        }
        
   

        private async void firmwareDuneSend(object sender, RoutedEventArgs e)
        {
            string website = DUNEWEBSITE + firmwareDuneVersions.Text + "/" + firmwareDuneModels.Text + "/";
            List<string> packages = await downloadWebsiteIndex(website);
            string filename = "";
            foreach(string package in packages)
            {
                if (package.Contains("boot_Ulbi_URootfs")) { filename = package; }
            }
            
            string fullpath = website + filename;
            duneInstallButton.IsEnabled = false;
            duneInstallButton.Content = "working...";
            await firmwareDownloadFile(fullpath, filename);
            await firmwareUSBSend(filename);
            duneInstallButton.IsEnabled = true;
            duneInstallButton.Content = "Download & Install";
            Settings.Default.DuneLastFW = firmwareDuneVersions.Text;
        }

        #endregion
        #region Shared Functions        
        private async Task firmwareDownloadFile(string fullpath, string filename)
        {
            pgLog("Downloading " + fullpath + filename);
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgressHandler);
            client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(downloadEndHandler);
            await client.DownloadFileTaskAsync(fullpath, filename);
            pgLog("Download success.");
        }
        private async Task<List<string>> downloadWebsiteIndex(string website)
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
                pgLog("Error: Specified site is invalid.");
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
        private async Task firmwareUSBSend(string filename)
        {
            pgLog("Sending firmware to printer");
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = filename;
            usbsend.StartInfo.CreateNoWindow = true;
            usbsend.StartInfo.RedirectStandardOutput = true;
            usbsend.Start();

            while (usbsend.HasExited == false)
            {
                pgLog(await usbsend.StandardOutput.ReadLineAsync());
            }
              
            if (usbsend.ExitCode == 0)
            {
                MessageBox.Show("Firmware upgrade success!");
            }
            else
            {
                MessageBox.Show("Firmware upgrade error / canceled");
            }
            File.Delete(filename);
        }
        private void firmwareUSBDestroy(object sender, RoutedEventArgs e)
        {
            try
            {
                usbsend.Kill();
            }
            catch
            {

            }
        }
        #endregion
        #endregion
        #region Printing tab 
        #region UI
        private async void printSend9100Button(object sender, RoutedEventArgs e)
        {
            await printSendIP(connectionsIpPrinterEntry.Text, await printGenerator());
        }
        private async void printSendUSBButton(object sender, RoutedEventArgs e)
        {
            await printSendIP(connectionsIpPrinterEntry.Text, await printGenerator());

        }

        private async void printSaveJob_Click(object sender, RoutedEventArgs e)
        {
            File.Copy(await printGenerator(), @"Data\Jobs\" + printNameJob.Text);
            printSavedJobsRefresh();
        }

        private void printDeteleJob_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(printSavedJobs.SelectedItem.ToString());
            printSavedJobsRefresh();
        }

        private async void printSendJob_Click(object sender, RoutedEventArgs e)
        {
            if(printSavedJobs.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            await printSendIP(connectionsIpPrinterEntry.Text, printSavedJobs.SelectedItem.ToString());
        }

        #endregion

        private void printSavedJobsRefresh()
        {
            printSavedJobs.Items.Clear();
            string[] filenames = Directory.GetFiles(@"Data\Jobs\");
            foreach (string filename in filenames)
            {
                printSavedJobs.Items.Add(filename);
            }
            

        }

        private async Task<string> printGenerator()
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

        private async Task<string> PJLStart(string jobname)
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

        private async Task<string> PJLEnd(string jobname)
        {
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "%-12345X";
            string endpart = escapeSequence + "@PJL\r\n"
                + "@PJL EOJ NAME = " + jobname +"\r\n"
                + escapeSequence+"\r\n";

            return endpart;
        }

        private async Task<string> printGeneratorCaller(int pages)
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

        private async Task<string> printCreatePCL(int pages)
        {   
            return "";
        }
        private async Task<string> printCreatePS(int pages)
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

        private async Task<string> printCreateESCP(int pages)
        {
            return "";
        }

        private async Task printSendIP(string ip, string file)
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
        private async Task printSendUSB(string file)
        {
            Process usbsend = new();
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = file;
            usbsend.StartInfo.CreateNoWindow = true;
            usbsend.Start();        
        }


        #endregion

        #region Shared Tasks
        private void pgLog(string log)
        {
            ApplicationLogs.AppendText("[" + DateTime.Now.ToShortTimeString() + "] "+log + "\n");
        }

        private void downloadProgressHandler(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            applicaitonProgressBar.Value = percentage;
        }
        private void downloadEndHandler(object sender, DownloadDataCompletedEventArgs e)
        {
            applicaitonProgressBar.Value = 0;
        }
        Process usbsend = new Process();











        #endregion

        
    }
}
