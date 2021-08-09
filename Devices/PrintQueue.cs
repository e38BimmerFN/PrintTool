using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
	public class PrintQueue
	{
		public PrintQueue(string ip)
		{

		}

		public static async Task SendIP(string ip, string file)
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

		public static async Task SendUSB(string file)
		{
			Process usbsend = new();
			usbsend.StartInfo.FileName = "Services\\USBSend.exe";
			usbsend.StartInfo.Arguments = file;
			usbsend.StartInfo.CreateNoWindow = true;
			usbsend.Start();
			await usbsend.WaitForExitAsync();
		}


		public static string PrintGenerator(List<string> args)
		{
			string start = PJLStart(args);
			string lang = "";
			switch (args[2])
			{
				case "1":
					lang = CreatePS(args);
					break;
				case "2":
					lang = CreatePCL(args);
					break;
				case "3":
					lang = CreateESCP(args);
					break;

			}
			string end = PJLEnd(args);
			string alltogether = start + lang + end;
			if (File.Exists(args[0])) { File.Delete(args[0]); }
			StreamWriter tempFile = File.CreateText(args[0]);
			tempFile.Write(alltogether);
			tempFile.Close();
			return args[0];
		}

		private static string PJLStart(List<string> args)
		{


			char escapeCharacter = (char)27;
			string escapeSequence = escapeCharacter + @"%-12345X";
			List<string> list = new();
			list.Add(escapeSequence);
			list.Add("@PJL RESET");
			list.Add("@PJL JOB NAME = " + args[0]);
			list.Add("@PJL SET JOBNAME = " + args[1]);
			list.Add("@PJL SET COPIES = " + args[9]);
			list.Add("@PJL SET DUPLEX = " + args[4]);
			if (args[5] != "") { list.Add("@PJL SET BINDING = " + args[5]); }
			list.Add("@PJL SET PAPER = " + args[6]);
			if (args[7] != "Default") { list.Add("@PJL SET MEDIASOURCE = " + args[7]); }
			if (args[8] != "Default") { list.Add("@PJL SET OUTBIN = " + args[8]); }

			string returnstring = "";
			foreach (string item in list)
			{
				returnstring = returnstring + item + "\r\n";
			}
			return returnstring;
		}

		private static string CreatePCL(List<string> args)
		{
			return "";
		}
		private static string CreatePS(List<string> args)
		{
			string output = "@PJL ENTER LANGUAGE=POSTSCRIPT \r\n" + "/Times-Roman findfont 14 scalefont setfont \r\n";

			for (int i = 0; i < int.Parse(args[3]); i++)
			{
				output = output
					+ "clippath stroke\r\n"
					+ "15 60 moveto\r\n"
					+ "(PrintTool) show\r\n"
					+ "20 40 moveto \r\n"
					+ "( Page number = " + (i + 1) + " ) show\r\n"
					 + "20 20 moveto \r\n"
					+ "( Source PC name  = " + Environment.MachineName + " ) show\r\n"
					+ "showpage\r\n";

			}
			return output;
		}

		private static string CreateESCP(List<string> args)
		{
			return "";
		}

		private static string PJLEnd(List<string> args)
		{
			char escapeCharacter = (char)27;
			string escapeSequence = escapeCharacter + "%-12345X";
			string endpart = escapeSequence + "@PJL\r\n"
				+ "@PJL EOJ NAME = " + args[1] + "\r\n"
				+ escapeSequence + "\r\n";

			return endpart;
		}



	}

}

