using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;
using System.IO.Ports;
using System.IO;



namespace PitCrewUltimateByDerekHearst
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            updater();
        }
        private void updater()
        {
            StreamReader sr = File.OpenText("\\\\jedibdlbroker.boi.rd.hpicorp.net\\DevScratch\\Derek\\1AUpdates\\PitCrewUltimate\\version.txt");
            decimal version = decimal.Parse(sr.ReadLine());
            if (Settings.Default.Version < version)
            {
                MessageBox.Show("Please update this tool");
            }
        }

        

        //LOG Collecting
        private void mainLog(string x)
        {
            mainLogBlock.Text = mainLogBlock.Text + "\n" + x;
        }
        private void yoloLogs(string x)
        {
            
            yoloTBLogs.Text = yoloTBLogs.Text + "\n" + x;
            
        }
        private void duneLogs(string x)
        {
            
            
            duneTBLogs.Text = duneTBLogs.Text + "\n" + x;
        }
        private void fwLogs(string x)
        {
            yoloLogs(x);
            duneLogs(x);
            
        }


        //Firmware Install//

        //Jedi
       
  
        const string JEDIPATH = "\\\\jedibdlserver.boi.rd.hpicorp.net\\JediSystems\\Published\\DailyBuilds\\25s\\2021";

        private void loadJediFWList(object sender, RoutedEventArgs e)
        {
            foreach (string f in Directory.GetDirectories(JEDIPATH))
            {
                yoloFWDateSelector.Items.Clear();
                yoloFWDateSelector.Items.Add(f.Remove(0, JEDIPATH.Length));
            }
        }




        //Yolo
        const string YOLOWEBSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
        List<string> yoloFWList = new List<string>();
        private async void grabYoloFirmware(object sender, EventArgs e)
        {
            
            yoloFWList.Clear();
            string sitetograbstring = YOLOWEBSITE + yoloProductSelection.SelectionBoxItem.ToString() + "/?C=M;O=D";
            string data = await downloadSite(sitetograbstring);
            string pattern = "[\"]001[.]\\d\\d\\d\\d.";
            Regex myPattern = new Regex(pattern);
            foreach (Match match in myPattern.Matches(data))
            {
                string matchoc = match.Value.Substring(1);
                yoloFWList.Add(matchoc);
            }

            for (int i = 0; i < yoloFWList.Count; i++)
            {
                yoloFWDateSelector.Items.Add(yoloFWList[i]);
            }
            yoloLogs("Succesfully grabbed " + yoloFWList.Count + " versions from " + yoloProductSelection.SelectionBoxItem.ToString());
        }
        private async void yoloUpdateButton(object sender, RoutedEventArgs e)
        {

            string selection = yoloProductSelection.SelectionBoxItem.ToString() + "/" + yoloFWDateSelector.SelectionBoxItem.ToString() + "/" + yoloFWSelect.SelectionBoxItem.ToString() + "/";
            string filename = yoloProductSelection.SelectionBoxItem.ToString() + "_" + yoloFWDateSelector.SelectionBoxItem.ToString() + "_" + yoloFWSelect.SelectionBoxItem.ToString() + "_rootfs.fhx";
            string fullpath = YOLOWEBSITE + selection + filename;
            yoloLogs("Downloading " + fullpath);
            yoloInstallButton.IsEnabled = false;
            yoloInstallButton.Content = "working...";
            await downloadFile(fullpath, filename);
            await usbSendFirmware(filename);
            yoloInstallButton.IsEnabled = true;
            yoloInstallButton.Content = "Download & Install";
        }


        //Dune
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        private async void grabDuneRevison(object sender, EventArgs e)
        {
            if(duneVSelect.Items.Count != 0)
            {
                return;
            }
            duneLogs("Grabbing firmware");
            duneVSelect.Items.Clear();
            duneVSelect.Items.Add(Settings.Default.DuneLastFW);
            string websiteData = await downloadSite(DUNEWEBSITE+ "?C=M;O=D");
            string regexPattern = "\\d[.]\\d[.]\\d[.]\\d\\d\\d\\d.";
            Regex myPattern = new Regex(regexPattern);
            var matches = myPattern.Matches(websiteData);
            for(int i =0; i<30; i++)
            {
                duneVSelect.Items.Add(matches[i].Value);
            }
            duneLogs("Grabbed " + matches.Count + " versions, But only showing 30.");
        }
        private async void grabDuneProduct(object sender, EventArgs e)
        {
            duneHWModelBox.Items.Clear();
            string websiteData = await downloadSite(DUNEWEBSITE + duneVSelect.Text);
            string regexPattern = "(?<=bdl\\/\">)(.*)(?=<\\/a)";
            Regex myPattern = new Regex(regexPattern);
            var matches = myPattern.Matches(websiteData);
            foreach(Match match in matches)
            {
                duneHWModelBox.Items.Add(match.Value);
            }
            if (duneHWModelBox.Items.Count == 0)
            {
                duneLogs("Found no models, check that version is correct");
            }
        }
        private async void duneUpdateButton(object sender, RoutedEventArgs e)
        {
            string path = DUNEWEBSITE + duneVSelect.Text + "/" + duneHWModelBox.SelectionBoxItem.ToString() + "/";
            string websiteData = await downloadSite(path);
            string regexPattern = "(?<=fhx\">)boot_Ulbi_URootfs(.*)(?=<\\/a)";
            Regex myPattern = new Regex(regexPattern);
            var matches = myPattern.Matches(websiteData);
            string filename =matches.First().ToString();
            string fullpath = path + filename;
            duneLogs("Downloading " + filename);
            duneInstallButton.IsEnabled = false;
            duneInstallButton.Content = "working...";
            await downloadFile(fullpath, filename);
            await usbSendFirmware(filename);
            duneInstallButton.IsEnabled = true;
            duneInstallButton.Content = "Download & Install";
            Settings.Default.DuneLastFW = duneVSelect.Text;
            Settings.Default.Save();
        }

        //Firmware download / install 
        private async Task downloadFile(string fullpath, string filename)
        {
            
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgress);
            await client.DownloadFileTaskAsync(fullpath, filename);
            fwLogs("Download success!");
        }
        private async Task<string> downloadSite(string website)
        {
            string result = "";
            try
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgress);
                result = await client.DownloadStringTaskAsync(website);
            }
            catch
            {
                MessageBox.Show("Error. Site invalid");
                return "Error";
            }

            return result;
        }
        private void downloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            
        }
        Process usbsend = new Process();
        private async Task usbSendFirmware(string filename)
        {
            fwLogs("Sending firmware to printer");
            
            usbsend.StartInfo.FileName = "\\\\jedibdlbroker.boi.rd.hpicorp.net\\DevScratch\\Derek\\1AUpdates\\USBSend.exe";
            usbsend.StartInfo.Arguments = filename;
            usbsend.StartInfo.CreateNoWindow = true;
            usbsend.Start();
            await usbsend.WaitForExitAsync();
            System.IO.File.Delete(filename);
            if(usbsend.ExitCode == 0)
            {
                fwLogs("Firmware upgrade success!");
            }
            else
            {
                fwLogs("Firmware upgrade error / canceled");
            }
            
            
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

        
    }
}
