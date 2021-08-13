﻿using PrimS.Telnet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
namespace PrintTool
{
	public class TelnetConnection
	{
		private System.Threading.CancellationTokenSource tokenSource = new();
		private Client cli;
		TextBox box { get; set; }
		public string ip { get; set; }
		public int port { get; set; }
		public string fileloc { get; set; }

		public int timeToGetData = 500;

		public TelnetConnection(string ip, int port, string fileloc, TextBox box)
		{
			Directory.CreateDirectory(fileloc);
			this.box = box;
			this.ip = ip;
			this.port = port;
			var token = tokenSource.Token;
			cli = new Client(ip, port, token);
			cli.TryLoginAsync("root", "", 1000);
			Timer timer = new(timeToGetData);
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			string result = await cli.TerminatedReadAsync(">");
			result = Regex.Replace(result, "(\u001b\\[1;34m)", "");
			result = Regex.Replace(result, "(\u001b\\[1;36m)", "");
			result = Regex.Replace(result, "(\u001b\\[m)", "");
			await Log(result);
		}

		public async Task Write(string command)
		{
			await cli.WriteLine(command);

		}

		private async Task Log(string log)
		{
			if (!log.Contains("\n") || !log.Contains("\r")) { log += "\n"; }
			//logging to textbox
			box.Dispatcher.Invoke(new Action(() =>
			{
				box.AppendText(log);
				box.ScrollToEnd();
			}));

			//Logging to file
			await File.AppendAllTextAsync(fileloc + port + ".txt", log);
		}


		public void Close()
		{
			cli.Dispose();
		}

		public static List<int> getAvaliable()
		{
			List<int> connections = new();
			connections.Add(8108);


			return connections;
		}
	}
}
