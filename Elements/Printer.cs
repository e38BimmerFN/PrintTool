using SharpIpp;
using SharpIpp.Exceptions;
using SharpIpp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace PrintTool
{
	public class Printer
	{

		// saved printer data
		public string model = "";
		public string id = "";
		public string engine = "";
		public string type = "";

		// gathred printer data
		public string printerInfo { get; set; }
		public string printerState { get; set; }
		public string printerError { get; set; }
		public string pagesPerMin { get; set; }
		public string queuedJobCount { get; set; }

		//Connections
		public string printerIp = "0.0.0.0";
		public string dartIp = "0.0.0.0";
		public bool enableSerial = false;
		public bool enableDart = false;
		public bool enableTelnet = false;
		public bool enablePrinterStatus = false;
		public List<SerialConnection> serialConnections = new();
		public List<TelnetConnection> telnetConnections = new();
		public bool connected = false;


		public TextBox box { get; set; }
		public string loggingLocation = @"Data\Logs\Temp\";


		public Printer()
		{
			Directory.CreateDirectory(@"Data\Jobs\");
			Directory.CreateDirectory(@"Data\Printers\");
			Directory.CreateDirectory(@"Data\Logs\Temp\");
		}

		public async Task GetPrinterStatus()
		{
			SharpIppClient client = new();
			List<string> data = new();

			Uri uri = new("ipp://" + printerIp + ":631");
			GetPrinterAttributesRequest request = new() { PrinterUri = uri };
			GetPrinterAttributesResponse response = new();
			try
			{
				response = await client.GetPrinterAttributesAsync(request);
			}
			catch
			{
				return;
			}
			printerInfo = response.PrinterInfo;
			printerState = response.PrinterState.ToString();
			printerError = response.PrinterStateReasons[0];
			pagesPerMin = response.PagesPerMinute.ToString();
			queuedJobCount = response.QueuedJobCount.ToString();
		}
		public async Task GetJobStatus()
		{
			SharpIppClient client = new();
			List<string> data = new();

			Uri uri = new("ipp://" + printerIp + ":631");
			GetJobAttributesRequest request = new() { PrinterUri = uri, JobId = 1 };
			GetJobAttributesResponse response = new();
			try
			{
				response = await client.GetJobAttributesAsync(request);
			}
			catch
			{

			}

		}



		public void SaveConfig()
		{
			List<string> data = new();
			data.Add(model);
			data.Add(id);
			data.Add(engine);
			data.Add(printerIp.ToString());
			data.Add(enableDart.ToString());
			data.Add(enableTelnet.ToString());
			data.Add(dartIp.ToString());
			data.Add(enableSerial.ToString());
			data.Add(type.ToString());
			StreamWriter myFile = File.CreateText(@"Data\Printers\" + model);
			foreach (string entry in data)
			{
				myFile.WriteLine(entry);
			}
			myFile.Close();
		}
		public void LoadConfig(string filename)
		{

			if (!File.Exists(filename)) { MessageBox.Show(filename + " Doesnt exist"); return; }
			StreamReader file = File.OpenText(filename);

			model = file.ReadLine();
			id = file.ReadLine();
			engine = file.ReadLine();
			printerIp = file.ReadLine();
			enableDart = bool.Parse(file.ReadLine());
			enableTelnet = bool.Parse(file.ReadLine());
			dartIp = file.ReadLine();
			enableSerial = bool.Parse(file.ReadLine());
			type = file.ReadLine();
			file.Close();
		}


		public async Task Log(string log)
		{
			if (!log.Contains("\n") || !log.Contains("\r")) { log += "\n"; }
			//logging to textbox
			box.Dispatcher.Invoke(new Action(() =>
			{
				box.AppendText(log);
				box.ScrollToEnd();
			}));

			//Logging to file
			await File.AppendAllTextAsync(loggingLocation + "PrintToolLog.txt", log);
		}
	}
}
