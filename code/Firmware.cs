using System;
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
    class Firmware
    {
        Process process;
        const string YOLOWEBSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
        #region Jedi Firmware
        const string JEDIPATH = "\\\\jedibdlserver.boi.rd.hpicorp.net\\JediSystems\\Published\\DailyBuilds\\25s\\2021";
        public static void loadJediFWList(object sender, RoutedEventArgs e)
        {
            foreach (string f in Directory.GetDirectories(JEDIPATH))
            {
                firmwareYoloVersion.Items.Clear();
                firmwareYoloVersion.Items.Add(f.Remove(0, JEDIPATH.Length));
            }
        }
        #endregion
        #region Yolo Firmware
        

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
            string path = "", filename = "";

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
            logger.Log("Downloading " + fullpath);
            yoloInstallButton.IsEnabled = false;
            yoloInstallButton.Content = "working...";
            await downloadAndSend(fullpath, filename);
            yoloInstallButton.IsEnabled = true;
            yoloInstallButton.Content = "Download & Install";

        }

        public static async Task firmwareYoloUpdateRevisons(System.Windows.Controls.ListBox listBox, string path)
        {
            listBox.Items.Clear();
            string website = YOLOWEBSITE + listBox.SelectionBoxItem + "/?C=M;O=D";
            List<string> results = await downloadWebsiteIndex(website);
            foreach (string result in results) { listBox.Items.Add(result); }
        }

        public static async Task YoloUpdatePackages(System.Windows.)
        {
            firmwareYoloPackage.Items.Clear();
            List<string> results = await downloadWebsiteIndex(YOLOWEBSITE + firmwareYoloProduct.SelectionBoxItem + "/" + firmwareYoloVersion.SelectionBoxItem + "/" + firmwareYoloDist.SelectionBoxItem + "/");
            foreach (string result in results) { firmwareYoloPackage.Items.Add(result); }
        }

        public static async void firmwareYoloUnsecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloUnsecureB.IsEnabled = false;
            firmwareYoloUnsecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_unsecure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if (firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("lochsa")) { filename = result; } } }
            await downloadAndSend(website, filename);
            firmwareYoloUnsecureB.IsEnabled = true;
            firmwareYoloUnsecureB.Content = "Convert to unsecure";
        }
        public static async void firmwareYoloSecureB_Click(object sender, RoutedEventArgs e)
        {
            firmwareYoloSecureB.IsEnabled = false;
            firmwareYoloSecureB.Content = "Working";
            string website = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/release/harish/yolo/convert_to_secure/";
            List<string> results = await downloadWebsiteIndex(website);
            string filename = "";
            if (firmwareYoloProduct.Text == "yoshino_dist") { foreach (string result in results) { if (result.Contains("yoshino")) { filename = result; } } }
            else { foreach (string result in results) { if (result.Contains("locsha")) { filename = result; } } }
            await downloadAndSend(website, filename);

            firmwareYoloSecureB.IsEnabled = true;
            firmwareYoloSecureB.Content = "Convert to secured";
        }
        #endregion
        #region Dune Firmware
        //Dune
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        const string DUNEUTILITY = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";

        #region DuneUI
        public static void firmwareDuneVersions_DropDownOpened(object sender, EventArgs e)
        {
            firmwareDuneModels.Items.Clear();
            firmwareDunePackages.Items.Clear();
        }
        public static async void firmwareDuneModels_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdateModels();
        }
        public static async void firmwareDunePackages_DropDownOpened(object sender, EventArgs e)
        {
            await firmwareDuneUpdatePackages();
        }

        public static void firmewareDuneUnsecure_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void firmewareDuneSecure_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void firmewareDuneReset_Click(object sender, RoutedEventArgs e)
        {
            // todo WORK ONM THIS
        }
        #endregion Dune UI
        public static async Task firmwareDuneUpdateRevisons()
        {
            await populateComboBox(firmwareDuneVersions, DUNEWEBSITE + "/?C=M;O=D");
            await firmwareDuneUpdateModels();

        }
        public static async Task firmwareDuneUpdateModels()
        {
            if (firmwareDuneVersions.Text == "") { return; }
            await populateComboBox(firmwareDuneModels, DUNEWEBSITE + firmwareDuneVersions.Text);
            if (firmwareDuneModels.Items.Count == 0) { MessageBox.Show("Version doesn't exist "); return; }
            await firmwareDuneUpdatePackages();

        }
        public static async Task firmwareDuneUpdatePackages()
        {
            await populateComboBox(firmwareDunePackages, DUNEWEBSITE + firmwareDuneVersions.Text + firmwareDuneModels.Text);
            foreach (string package in firmwareDunePackages.Items)
            {
                if (package.Contains("fhx")) { firmwareDunePackages.Items.Remove(package); }
            }
        }
        public static async void firmwareDuneSend(object sender, RoutedEventArgs e)
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
            await downloadAndSend(fullpath, filename);
            duneInstallButton.IsEnabled = true;
            duneInstallButton.Content = "Download & Install";
        }

        #endregion
          

        

        public static async Task downloadAndSend(string filename, string website, Logger logger, System.Windows.Controls.Button button)
        {
            button.IsEnabled = false;
            button.Content = "Proccessing..";
            logger.Log("Downloading " + website + filename);
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgressHandler);
            client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(downloadEndHandler);
            await client.DownloadFileTaskAsync(website, filename);
            logger.Log("Download success.");
            logger.Log("Sending firmware to printer");
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
            catch { }

            button.IsEnabled = true; ;
            button.Content = "Download and Send";
        }
                
        public  void firmwareUSBDestroy(object sender, RoutedEventArgs e)
        {
            try
            {
                usbsend.Kill();
            }
            catch
            {

            }
        }
        s

    }
}
