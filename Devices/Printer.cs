using System.Collections.Generic;
using System.IO;
using System.Windows;
using SharpIpp;
using SharpIpp.Exceptions;
using SharpIpp.Model;
using System.Net;
using System;
using System.Threading.Tasks;
namespace PrintTool
{
	public class Printer
	{
		private string location = @"Data\Printers\";
		public string model { get; set; }
		public string id { get; set; }
		public string engineType { get; set; }

		//Connections
		public string ip { get; set; }
		public bool usingSerial { get; set; }
		public List<SerialConnection> serialPorts { get; set; }
		public Dart dart;
		public PrintQueue queue;

		//printer data
		public string printerInfo { get; set; }
		public string printerState { get; set; }
		public string printerError { get; set; }
		public string pagesPerMin { get; set; }
		public string queuedJobCount { get; set; }


		//log 
		public Logger log { get; set; }

		public Printer()
		{
			serialPorts = new();
			dart = new();
			usingSerial = false;
			dart.isEnabled = false;
			ip = "0.0.0.0";
		}

		public async Task getPrinterStatus()
		{
			SharpIppClient client = new();
			List<string> data = new();
			
			Uri uri = new Uri("ipp://" + ip + ":631");
			GetPrinterAttributesRequest request = new() { PrinterUri = uri };
			GetPrinterAttributesResponse response = new();
			try
			{
				response = await client.GetPrinterAttributesAsync(request);
			}
			catch
			{
				MessageBox.Show("Couldn't communicate with printer over IPP, check if its enabled");
			}
			printerInfo = response.PrinterInfo;
			printerState = response.PrinterState.ToString();
			printerError = response.PrinterStateReasons[0];
			pagesPerMin = response.PagesPerMinute.ToString();
			queuedJobCount = response.QueuedJobCount.ToString();
		}

		public async Task getJobStatus()
		{
			SharpIppClient client = new();
			List<string> data = new();

			Uri uri = new Uri("ipp://" + ip + ":631");
			GetJobsRequest request = new() { PrinterUri = uri, RequestingUserName="PrintTool(name)"};
			GetJobsResponse response = new();
			try
			{
				response = await client.GetJobsAsync(request);
			}
			catch(Exception e)
			{

				MessageBox.Show("Couldn't communicate with printer over IPP, check if its enabled");
			}
			
		}


		public void ConnectToSerial()
		{
			foreach (string name in SerialConnection.GetPorts())
			{
				SerialConnection serial = new SerialConnection(new Logger(name));
				serial.Connect(name);
				serialPorts.Add(serial);
			}
		}

		public void DisconnectSerial()
		{
			foreach (SerialConnection serialConnection in serialPorts)
			{
				serialConnection.Close();
			}
			serialPorts.Clear();
		}

		public void SaveLogs(string pathToSaveTo)
		{
			foreach (SerialConnection serial in serialPorts)
			{
				serial.log.SaveLogs(pathToSaveTo);
			}
		}


		public void Save()
		{
			List<string> data = new();
			data.Add(model);
			data.Add(id);
			data.Add(engineType);
			data.Add(ip.ToString());
			data.Add(dart.isEnabled.ToString());
			data.Add(dart.usingPorts.ToString());
			data.Add(dart.ip.ToString());
			data.Add(usingSerial.ToString());
			StreamWriter myFile = File.CreateText(location + model);
			foreach (string entry in data)
			{
				myFile.WriteLine(entry);
			}
			myFile.Close();
		}

		public void Load(string filename)
		{

			if (!File.Exists(filename)) { MessageBox.Show(filename + " Doesnt exist"); return; }
			StreamReader file = File.OpenText(filename);

			model = file.ReadLine();
			id = file.ReadLine();
			engineType = file.ReadLine();
			ip = file.ReadLine();
			dart.isEnabled = bool.Parse(file.ReadLine());
			dart.usingPorts = bool.Parse(file.ReadLine());
			dart.ip = file.ReadLine();
			usingSerial = bool.Parse(file.ReadLine());
			file.Close();
		}
	}
}
