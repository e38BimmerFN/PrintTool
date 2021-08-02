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
    static class Tasks
    {
        

        public static async Task runStartUp()
        {
            if (checkHPStatus())
            {

                checkExePath(); 
                checkForUpdates();   
                

            }
            MessageBox.Show("This program is in alpha, report any errors or changes you want made to derek.hearst@hp.com", "PrintTool");
            Directory.CreateDirectory("Data\\Connections\\");       
            Directory.CreateDirectory("Data\\Logs\\");
            Directory.CreateDirectory("Data\\Jobs\\");
        }



        public static bool checkHPStatus()
        {
            if (Directory.Exists(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static void checkForUpdates()
        {

            StreamReader sr = File.OpenText(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\versionAndNotes.txt");

            int version = int.Parse(sr.ReadLine());
            sr.Close();
            if (Settings.Default.Version < version)
            {
                MessageBox.Show(@"Please update this tool, navigate to \DevScratch\Derek\PrintTool and run the executable.");
            }
        }
        private static void checkExePath()
        {

            if (Directory.GetCurrentDirectory().Contains(@"\DevScratch\Derek\PrintTool"))
            {
                var result = MessageBox.Show("This program will now install itself into : " + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\", "Please click yes or no", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    MessageBox.Show("Exiting.");
                    Application.Current.MainWindow.Close();
                }
                string copyfrompath = @"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\";

                string copytopath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\";
                Directory.CreateDirectory(copytopath);
                List<string> filestoCopy = new();

                filestoCopy.Add("PrintTool.exe");
                filestoCopy.Add("USBSend.exe");
                foreach (string file in filestoCopy)
                {
                    if (File.Exists(copytopath + file)) { File.Delete(copytopath + file); }
                    File.Copy(copyfrompath + file, copytopath + file);
                }
                MessageBox.Show("Sucessfully installed. now closing.");
                Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\");
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\PrintTool.exe");
                Application.Current.MainWindow.Close();
            }
        }
      


        public static void RunEndTasks()
        {
            
        }

        public static void ListBoxDelete(System.Windows.Controls.ListBox listBox)
        {
            if(listBox.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
            string path = listBox.SelectedItem.ToString();
            File.Delete(path);
        }

        public static void PopulateListBox(System.Windows.Controls.ListBox listBox, string path)
        {
            listBox.Items.Clear();
            string[] results =  Directory.GetFiles(path);
            foreach(string result in results)
            {
                listBox.Items.Add(result);
            }
        }

        
        public static async Task PopulateComboBox(System.Windows.Controls.ComboBox comboBox, string site)
        {
            comboBox.Items.Clear();

            //allowing both http scraping or directory
            if (site.Contains("http"))
            {
                List<string> results = await DownloadWebsiteIndex(site);
                foreach (string result in results) { comboBox.Items.Add(result + "/"); }
            }
            else if (site.Contains(@"\\") || site.Contains(@"C:"))
            {
                string[] results = Directory.GetFiles(site);
                if (results.Length == 0) { comboBox.Items.Add("Nothing found"); }
                foreach (string result in results) { comboBox.Items.Add(result.Substring(result.LastIndexOf("\\")) + "\\"); }
            }
            else
            {
                MessageBox.Show(site + " Is not valid");
            }
        }

        public static async Task<string> downloadFile(string filename, string location)
        {
            if (location.Contains("http"))
            {
                WebClient webClient = new();
                await webClient.DownloadFileTaskAsync(location, filename);
                
            }
            else if(location.Contains(@"\\")|| location.Contains(@"C:"))
            {
                File.Copy(location + filename, filename);
            }
            else
            {
                MessageBox.Show("Invalid location");
                return "";
            }
            return filename;
        }


        public static async Task<List<string>> DownloadWebsiteIndex(string website)
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
            }
            MatchCollection matches = regexATag.Matches(websiteData);
            foreach (Match match in matches) { resultsPartial.Add(match.Value); }
            foreach (string match in resultsPartial) { results.Add(regexText.Match(match).Value); }

            results.RemoveAt(0);

            int resultstokeep = 100;
            try { results.RemoveRange(resultstokeep, results.Count - resultstokeep); }
            catch { }
            if (results.Count == 0)
            {
                results.Add("Found no listings for current selection.");
            }
            return results;
        }

    }
}
