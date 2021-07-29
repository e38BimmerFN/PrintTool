using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using HtmlAgilityPack;




namespace PrintTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Startup
        private void StartupTasks(object sender, EventArgs e)
        {
            if (checkHPStatus())
            {
                checkForUpdates();
                firmwareYoloVersionUpdate();
                firmwareDuneVersionUpdate();
                checkForUpdates();
                checkExePath();
               
            }
            setDefaults();
            ConnectionConfigRefresh();
            createSubFolders();
            
            
            
        }
        private void createSubFolders()
        {
            Directory.CreateDirectory("Data/Connections");
            Directory.CreateDirectory("Data/Logs/Temp");
            Directory.CreateDirectory("Data/Logs/Captured");
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

            StreamReader sr = File.OpenText(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\@Shared\PrintToolVersion.txt");

            decimal version = decimal.Parse(sr.ReadLine());
            if (Settings.Default.Version < version)
            {
                MessageBox.Show("Please update this tool");
            }
        }
        private void checkExePath()
        {
            if(Directory.GetCurrentDirectory() .Contains( @"\DevScratch\Derek\PrintTool\"))
            {
                var result = MessageBox.Show("This program will now install itself into its own folder in the path C:/Users/" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "PrintTool/", "Please click yes or no", MessageBoxButton.OKCancel);
                if(result == MessageBoxResult.Cancel)
                {
                    MessageBox.Show("Exiting.");
                    this.Close();
                }
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "PrintTool/")){
                    File.Copy(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\PrintTool.ext", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "PrintTool/PrintTool.exe");
                }
                else
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "PrintTool/");
                    File.Copy(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\PrintTool.ext", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "PrintTool/PrintTool.exe");

                }
            }
        }


        private void EndTask(object sender, System.ComponentModel.CancelEventArgs e)
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

        private void connectionsEnableSerial_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SaveDefaults(object sender, RoutedEventArgs e)
        {
            if (modelEntry.Text == "")
            {
                MessageBox.Show("Please select something");
                return;
            }
// TODO Write this
            ConnectionConfigRefresh();
        }
        private void LoadDefaults(object sender, RoutedEventArgs e)
        {

        }
     
// TODO Write this


        private void ConnectionConfigRefresh()
        {          
            string[] filenames = Directory.GetFiles("Data/Connections");
            SavedConnectionConfigs.Items.Clear();
            foreach (string filename in filenames)
            {
                SavedConnectionConfigs.Items.Add(filename);
            }   
        }


        private void DefaultsDelete(object sender, RoutedEventArgs e)
        {
            string path = SavedConnectionConfigs.SelectedItem.ToString();
            File.Delete(path);
            ConnectionConfigRefresh();

        }

        private void PrinterIPBox(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
        private void DartIPBox(object sender, System.Windows.Controls.TextChangedEventArgs e)
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

        #endregion
        #region Firmware Tab
        #region Jedi Firmware
        const string JEDIPATH = "\\\\jedibdlserver.boi.rd.hpicorp.net\\JediSystems\\Published\\DailyBuilds\\25s\\2021";
        private void loadJediFWList(object sender, RoutedEventArgs e)
        {
            foreach (string f in Directory.GetDirectories(JEDIPATH))
            {
                firmwareYoloVersions.Items.Clear();
                firmwareYoloVersions.Items.Add(f.Remove(0, JEDIPATH.Length));
            }
        }
        #endregion
        #region Yolo Firmware
        const string YOLOWEBSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";

        private async void firmwareYoloVersionUpdate()
        {
            firmwareYoloVersions.Items.Clear();
            string website = YOLOWEBSITE + yoloProductSelection.SelectionBoxItem.ToString() + "/?C=M;O=D";
            List<string> results = await downloadWebsiteIndex(website);            
            foreach(string result in results) { firmwareYoloVersions.Items.Add(result); }
        }

        private async void firmwareYoloSendVersion(object sender, RoutedEventArgs e)
        {

            string selection = yoloProductSelection.SelectionBoxItem.ToString() + "/" + firmwareYoloVersions.SelectionBoxItem.ToString() + "/" + yoloFWSelect.SelectionBoxItem.ToString() + "/";
            string filename = yoloProductSelection.SelectionBoxItem.ToString() + "_" + firmwareYoloVersions.SelectionBoxItem.ToString() + "_" + yoloFWSelect.SelectionBoxItem.ToString() + "_rootfs.fhx";
            if (yoloFWSelect.SelectedIndex == 3)
            {
                filename = yoloProductSelection.SelectionBoxItem.ToString() + "_" + firmwareYoloVersions.SelectionBoxItem.ToString() + "_nonassert_appsigned_lbi_rootfs_secure_signed.fhx";
            }
            string fullpath = YOLOWEBSITE + selection + filename;
            pgLog("Downloading " + fullpath);
            yoloInstallButton.IsEnabled = false;
            yoloInstallButton.Content = "working...";
            await downloadFile(fullpath, filename);
            await usbSendFirmware(filename);
            yoloInstallButton.IsEnabled = true;
            yoloInstallButton.Content = "Download & Install";
        }
        #endregion
        #region Dune Firmware
        //Dune
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        private async void firmwareDuneVersionUpdate()
        {
            if (firmwareDuneVersions.Items.Count != 0)
            {
                return;
            } 
            firmwareDuneVersions.Items.Clear();
            firmwareDuneVersions.Items.Add(Settings.Default.DuneLastFW);
            List<string> versions = await downloadWebsiteIndex(DUNEWEBSITE+"/?C=M;O=D");
            foreach(string version in versions)
            {
                firmwareDuneVersions.Items.Add(version);
            }
            
        }

        private async void firmwareDuneModelUpdate(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            firmwareDuneModels.Items.Clear();
            List<string> models = await downloadWebsiteIndex(DUNEWEBSITE + firmwareDuneVersions.Text);
            foreach (string model in models)
            {
                firmwareDuneModels.Items.Add(model);
            }
            if (firmwareDuneModels.Items.Count == 0)
            {
                MessageBox.Show("Found no models, check that version is correct");
            }
        }

        private async void firmwareDuneUpdate(object sender, RoutedEventArgs e)
        {
            string path = DUNEWEBSITE + firmwareDuneVersions.Text + "/" + firmwareDuneModels.Text + "/";
            List<string> websiteData = await downloadWebsiteIndex(path);
            string regexPattern = "(?<=fhx\">)boot_Ulbi_URootfs(.*)(?=<\\/a)";
            Regex myPattern = new Regex(regexPattern);
            var matches = myPattern.Matches(websiteData);
            string filename = matches.First().ToString();
            string fullpath = path + filename;
            duneInstallButton.IsEnabled = false;
            duneInstallButton.Content = "working...";
            await downloadFile(fullpath, filename);
            await usbSendFirmware(filename);
            duneInstallButton.IsEnabled = true;
            duneInstallButton.Content = "Download & Install";
            Settings.Default.DuneLastFW = firmwareDuneVersions.Text;
            Settings.Default.Save();
        }

        #endregion
        #region Shared Functions

        

        private async Task downloadFile(string fullpath, string filename)
        {
            pgLog("Downloading " + fullpath + filename);
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgress);
            client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(downloadDone);
            await client.DownloadFileTaskAsync(fullpath, filename);
            pgLog("Download success.");
        }

        private async Task<List<string>> downloadWebsiteIndex(string website)
        {
            List<string> results = new();
            string websiteData = "";
            Regex myPattern = new Regex("(?<=\\/\" >)(.*)(?=<\\/ a >)");
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
            MatchCollection matches = myPattern.Matches(websiteData);
            foreach(Match match in matches)
            {
                results.Add(match.Value);
            }
            results.RemoveAt(0);
            return results;
        }
        private void downloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            applicaitonProgressBar.Value = percentage;
        }
        private void downloadDone(object sender, DownloadDataCompletedEventArgs e)
        {
            applicaitonProgressBar.Value = 0;
        }
        Process usbsend = new Process();
        private async Task usbSendFirmware(string filename)
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
        private void usbSendDestroy(object sender, RoutedEventArgs e)
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
        

        #endregion

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
                client.Connect(ip, 9100);
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

        #endregion

        
    }
}
