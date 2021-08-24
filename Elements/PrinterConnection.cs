using SharpIpp;
using SharpIpp.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
	public class PrinterConnection
	{
		SharpIppClient cli = new();
		Uri ip;
		public PrinterConnection(string ip)
		{
			this.ip = new Uri($"ipp://{ip}:631");
		}

		public async Task<PrintJobResponse> SendJob(string file, string media = "na_letter_8.5x11in", Sides sides = Sides.OneSided, string mediaSource = "auto", string outputTray = "face-down")
		{
			FileStream fs = File.OpenRead(file);
			List<IppAttribute> ja = new();
			//Media source
			ja.Add(new IppAttribute(Tag.BegCollection, "media-col", ""));
			ja.Add(new IppAttribute(Tag.MemberAttrName, "", "media-source"));
			ja.Add(new IppAttribute(Tag.Keyword, "", mediaSource));
			ja.Add(new IppAttribute(Tag.EndCollection, "", ""));
			//Output Trray
			ja.Add(new IppAttribute(Tag.Keyword, "output-bin", outputTray));
			PrintJobRequest req = new()
			{
				NewJobAttributes = new NewJobAttributes()
				{
					Copies = 1,
					JobName = "PrintTool",
					Media = media,
					Sides = sides,
					AdditionalJobAttributes = ja,

				},
				PrinterUri = ip,
				Document = fs
			};
			var res = await cli.PrintJobAsync(req);
			fs.Close();
			return res;

		}

		public async Task<GetPrinterAttributesResponse> GetPrinterDetails()
		{
			GetPrinterAttributesRequest req = new()
			{
				PrinterUri = ip
			};
			GetPrinterAttributesResponse res = await cli.GetPrinterAttributesAsync(req);

			return res;

		}


		public static async Task SendIP(string ip, string file)
		{
			byte[] data = Array.Empty<byte>();
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
				TcpClient client = new();
				await client.ConnectAsync(ip, 9100);
				NetworkStream stream = client.GetStream();
				await stream.WriteAsync(data);

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


		public static string CreateJob(List<string> args)
		{
			char escapeCharacter = (char)27;
			string escapeSequence = escapeCharacter + @"%-12345X";
			string output = "";
			output += escapeSequence;
			output += $"@PJL RESET\r\n";
			output += $"@PJL JOB NAME = {args[0]}\r\n";
			output += $"@PJL SET JOBNAME = {args[1]}\r\n";
			output += $"@PJL SET COPIES = {args[8]}\r\n";
			output += $"@PJL SET DUPLEX =  {args[3]}\r\n";
			if (args[5] != "") { output += $"@PJL SET BINDING = {args[4]}\r\n"; }
			output += $"@PJL SET PAPER = {args[5]}\r\n";
			if (args[7] != "Default") { output += $"@PJL SET MEDIASOURCE = {args[6]}\r\n"; }
			if (args[8] != "Default") { output += $"@PJL SET OUTBIN = {args[7]}\r\n"; }


			output += "@PJL ENTER LANGUAGE=POSTSCRIPT \r\n" + "/Times-Roman findfont 14 scalefont setfont \r\n";

			for (int i = 0; i < int.Parse(args[2]); i++)
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

			
			output += escapeSequence + "@PJL\r\n"
				+ "@PJL EOJ NAME = " + args[1] + "\r\n"
				+ escapeSequence + "\r\n";
			File.WriteAllText(args[0], output);
			return args[0];
		}




	}

	
}

