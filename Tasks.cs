using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
namespace PrintTool
{
	static class Tasks
	{


		public static void runStartUp()
		{
			Directory.CreateDirectory("Data\\Printers\\");
			Directory.CreateDirectory("Data\\Logs\\");
			Directory.CreateDirectory("Data\\Jobs\\");
			InstallOrUpdate();
		}

		public static bool checkHPStatus()
		{
			return Directory.Exists(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\");
		}

		private static void InstallOrUpdate()
		{
			string from = @"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\";
			string to = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\";
			int version = int.Parse(File.ReadAllLines(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\versionAndNotes.txt")[0]);
			if (Directory.GetCurrentDirectory().Contains(@"\DevScratch\Derek\PrintTool"))
			{
				MessageBox.Show("Now updating or installing into " + to);
				string[] files = Directory.GetFiles(from);
				foreach (string file in files)
				{
					string filename = Path.GetFileName(file);
					if (File.Exists(to + filename)) { File.Delete(to + filename); }
					File.Copy(file, to + filename);
				}
				MessageBox.Show("Updated / Installed succesfull.");
				Process process = new Process();
				process.StartInfo.FileName = "explorer.exe";
				process.StartInfo.Arguments = to;
				process.Start();
				Application.Current.MainWindow.Close();
			}
			else
			{
				if (Settings.Default.Version < version)
				{
					MessageBox.Show("This program is out of date. Please run the installer");
					Process process = new Process();
					process.StartInfo.FileName = "explorer.exe";
					process.StartInfo.Arguments = from;
					process.Start();
					Application.Current.MainWindow.Close();
					return;
				}
			}
		}

		public static void RunEndTasks()
		{

		}

		public static void ListBoxDelete(System.Windows.Controls.ListBox listBox)
		{
			if (listBox.SelectedItem == null) { MessageBox.Show("Please select something first."); return; }
			string path = listBox.SelectedItem.ToString();
			File.Delete(path);
		}

		public async static void PopulateListBox(System.Windows.Controls.ListBox listBox, string site, string filter = "")
		{
			listBox.Items.Clear();
			var results = await getListings(site, filter);
			foreach (string result in results)
			{
				listBox.Items.Add(result);
			}
		}

		public static async Task PopulateComboBox(System.Windows.Controls.ComboBox comboBox, string site, string filter = "")
		{
			comboBox.Items.Clear();
			var results = await getListings(site, filter);
			foreach (string result in results)
			{
				comboBox.Items.Add(result);
			}
		}

		public static async Task<string> DownloadOrCopyFile(string filename, string location)
		{
			if (location.Contains("http"))
			{
				WebClient webClient = new();

				await webClient.DownloadFileTaskAsync(location + filename, filename);
				return filename;

			}
			else if (location.Contains(@"\") || location.Contains(@"C:"))
			{
				File.Copy(location +@"\" + filename, filename);
				return filename;
			}
			
			else
			{
				MessageBox.Show("Invalid location");
				return "";
			}
		}

		public static async Task<List<string>> getListings(string uri, string filter = "")
		{
			List<string> results = new();
			if (uri == "") { results.Add("Invalid URI"); return results; }
			if (uri.Contains("http"))
			{
				WebClient client = new WebClient();
				string webData = "";
				try { webData = await client.DownloadStringTaskAsync(uri); }
				catch { results.Add("Invalid URL"); return results; }
				MatchCollection matches = new Regex("(?<=<a href=\")(.*)(?=\\/a><\\/td>)").Matches(webData);
				foreach (Match match in matches)
				{
					Match submatch = Regex.Match(match.Value, "(?<=\">)(.*)(?=<)");
					if (submatch.Success) { results.Add(submatch.Value); }
				}
				results.RemoveAt(0);
			}
			else if (uri.Contains(@"\"))
			{
				List<string> files = new();
				try { files = Directory.GetFiles(uri).ToList(); }
				catch { results.Add("Invalid path."); return results; ; }
				foreach (string file in files)
				{
					results.Add(Path.GetFileName(file));
				}
			}
			List<string> filteredResults = new();
			foreach (string result in results)
			{
				if (result.EndsWith(filter)) { filteredResults.Add(result); }
			}

			if (filteredResults.Count > 100) { filteredResults.RemoveRange(100 - 1, filteredResults.Count - 100); }
			if (filteredResults.Count == 0) { filteredResults.Add("Nothing Found"); return filteredResults; }
			return filteredResults;
		}


		public async static Task<bool> CheckIP(string ip)
		{
			string regexmatch = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$";
			var myRegex = Regex.Match(ip, regexmatch);
			if (myRegex.Success)
			{
				Ping pingSender = new Ping();
				PingReply reply = await pingSender.SendPingAsync(ip);
				if(reply.Status == IPStatus.Success) { return true; }
			}
			return false;
		}
	}
}
