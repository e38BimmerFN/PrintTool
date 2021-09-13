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
	class Helper
	{

		private WebClient cli = new();
		public System.Windows.Controls.ProgressBar progressBar = null;

		public Helper(System.Windows.Controls.ProgressBar progressBar)
		{
			this.progressBar = progressBar;
			cli.DownloadProgressChanged += Cli_DownloadProgressChanged;
			cli.DownloadFileCompleted += Cli_DownloadFileCompleted;
			cli.DownloadStringCompleted += Cli_DownloadStringCompleted;
		}

		private void Cli_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			progressBar.Dispatcher.Invoke(new Action(() =>
			{
				progressBar.Visibility = Visibility.Collapsed;
			}));
		}

		private void Cli_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			progressBar.Dispatcher.Invoke(new Action(() =>
			{
				progressBar.Visibility = Visibility.Collapsed;
			}));
		}

		private void Cli_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			progressBar.Dispatcher.Invoke(new Action(() =>
			{
				progressBar.Visibility = Visibility.Visible;
				progressBar.Value = e.ProgressPercentage;
			}));
		}

		public static bool ConnectedToHP()
		{
			return Directory.Exists(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\");
		}

		public static void InstallOrUpdate()
		{
			string from = @"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\";
			string to = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\PrintTool\";
			if (ConnectedToHP())
			{
				if (Directory.GetCurrentDirectory().Contains(@"\DevScratch\Derek\PrintTool"))
				{
					MessageBox.Show("Now updating or installing into " + to);
					Directory.CreateDirectory(to);

					foreach (string dirPath in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
					{
						Directory.CreateDirectory(dirPath.Replace(from, to));
					}

					//Copy all the files & Replaces any files with the same name
					foreach (string newPath in Directory.GetFiles(from, "*.*", SearchOption.AllDirectories))
					{
						File.Copy(newPath, newPath.Replace(from, to), true);
					}

					MessageBox.Show("Updated / Installed succesfull.");
					Process process = new();
					process.StartInfo.FileName = "explorer.exe";
					process.StartInfo.Arguments = to;
					process.Start();
					Application.Current.MainWindow.Close();
				}
				else
				{
					int version = int.Parse(File.ReadAllLines(@"\\jedibdlbroker.boi.rd.hpicorp.net\DevScratch\Derek\PrintTool\versionAndNotes.txt")[0]);
					if (Settings.Default.Version < version)
					{
						MessageBox.Show("This program is out of date. Please run the installer");
						Process process = new();
						process.StartInfo.FileName = "explorer.exe";
						process.StartInfo.Arguments = from;
						process.Start();

						return;
					}
				}
			}
		}


		public async Task<bool> TryDownloadOrCopyFile(string filename, string location)
		{
			if (location.Contains("http"))
			{
				try
				{
					await cli.DownloadFileTaskAsync(location + filename, filename);
					return true;
				}
				catch
				{					
					return false;					
				}
			}
			else if (location.Contains(@"\") || location.Contains(@"C:"))
			{
				if (File.Exists(filename)) { File.Delete(filename); }
				FileStream readFile = File.OpenRead(location + "\\" + filename);
				FileStream copyFile = File.Create(filename);
				await readFile.CopyToAsync(copyFile);
				readFile.Close();
				copyFile.Close();
				return true;
			}
			else
			{
				return false;
			}
		}

		public async static Task<List<string>> PopulateFromPathOrSite(string path, string filter = "", bool flip = false)
		{
			List<string> results = new();
			if (path.Contains("C:\\") || path.Contains("\\\\"))
			{
				DirectoryInfo directory;
				try
				{
					directory = new(path);
					foreach (FileInfo file in directory.EnumerateFiles())
					{
						if (!file.Name.Contains(filter)) { continue; }
						results.Add(file.Name);
					}
					foreach (DirectoryInfo folder in directory.EnumerateDirectories())
					{
						if (!folder.Name.Contains(filter)) { continue; }
						results.Add(folder.Name);
					}
				}
				catch
				{
					results.Add($"{path} does not exist.");
					return results;
				}
			}
			else if (path.Contains("http"))
			{
				WebClient webClient = new();
				try
				{
					string webData = await webClient.DownloadStringTaskAsync(path);
					MatchCollection matches = new Regex("(?<=<a href=\")(.*)(?=\\/a><\\/td>)").Matches(webData);
					foreach (Match match in matches)
					{
						Match submatch = Regex.Match(match.Value, "(?<=\">)(.*)(?=<)");
						if (submatch.Success && submatch.Value.Contains(filter) && submatch.Value != "Parent Directory") 
						{ 
							results.Add(submatch.Value); 
						}
					}
				}
				catch 
				{
					results.Add("Invalid URL"); 
					return results; 
				}				
			}
			else
			{
				results.Add($"{path} is not valid.");
				return results;
			}


			if(results.Count > 99)
			{
				results.RemoveRange(100, results.Count - 101);
			}
			if(results.Count == 0)
			{
				results.Add("Nothing Found");
			}
			if (flip == true)
			{
				results.Reverse();
			}
			return results;	

		}


		public async static Task<bool> CheckIP(string ip)
		{
			try
			{
				string regexmatch = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$";
				var myRegex = Regex.Match(ip, regexmatch);
				if (myRegex.Success)
				{
					Ping pingSender = new();
					PingReply reply = await pingSender.SendPingAsync(ip, 1000);
					if (reply.Status == IPStatus.Success) { return true; }
				}
				return false;
			}
			catch
			{
				return false;
			}
		}


		public static void OpenPath(string uri)
		{
			Process.Start("explorer", uri);
		}


		public async Task DLAndSend(string filename, string website, Logger logger, System.Windows.Controls.Button button, System.Threading.CancellationToken token)
		{
			Process usbsend = new();

			button.IsEnabled = false;
			button.Content = "Proccessing..";
			await logger.Log("Downloading " + website + filename);
			if (!await TryDownloadOrCopyFile(filename, website))
			{
				
			}
			
			await logger.Log("Download success.");
			await logger.Log("Sending firmware to printer");

			progressBar.Dispatcher.Invoke(new Action(() =>
			{
				progressBar.Visibility = Visibility.Visible;
				progressBar.IsIndeterminate = true;
			}));

			usbsend.StartInfo.FileName = "Services\\USBSend.exe";
			usbsend.StartInfo.Arguments = filename;
			usbsend.StartInfo.CreateNoWindow = true;
			usbsend.StartInfo.RedirectStandardOutput = true;
			usbsend.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => { await logger.Log(e.Data); });
			usbsend.Start();
			usbsend.BeginOutputReadLine();
			await usbsend.WaitForExitAsync(token);
			if (usbsend.ExitCode == 0)
			{
				MessageBox.Show("Firmware upgrade success!");
				await logger.Log("Firmware upgrade success!");
			}
			else
			{
				MessageBox.Show("Firmware upgrade error / canceled. Check USB Connection.");
				await logger.Log("Firmware upgrade error / canceled. Check USB Connection.");
			}
			try { usbsend.Kill(); }
			catch { MessageBox.Show("Unable to close USBSend"); }
			try { File.Delete(filename); }
			catch { MessageBox.Show($"Unable to delete {filename}"); }
			button.IsEnabled = true; ;
			button.Content = "Download and Send";

			progressBar.Dispatcher.Invoke(new Action(async () =>
			{
				progressBar.Visibility = Visibility.Collapsed;
				progressBar.IsIndeterminate = false;
			}));
		}
	}
}
