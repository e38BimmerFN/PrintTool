using SharpIpp;
using SharpIpp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace PrintTool
{
	class Printer
	{
		public struct SupplyInfo
		{
			public SupplyInfo(string supplyname, int percent)
			{
				this.supplyname = supplyname;
				this.percent = percent;
			}
			public string supplyname { get; set; }
			public int percent { get; set; }
			override
			public string ToString()
			{
				return $"{supplyname} is at {percent}%";
			}
		}

		SharpIppClient ippCli = new();
		public string Nickname { get; set; } = "Test";

		Uri IPPUri;
		public string IP { get; set; }
		List<PrintJob> jobsSent = new();

		//Printer State
		public string IPPName { get; set; } = "N\\A";
		public string IPPPNameInfo { get; set; } = "N\\A";
		public string IPPFirmwareInstalled { get; set; } = "N\\A";
		public string IPPPPM { get; set; } = "N\\A";
		public string IPPColorSupported { get; set; } = "N\\A";
		public string IPPPrinterState { get; set; } = "N\\A";
		public string IPPPrinterStateMessage { get; set; } = "N\\A";
		public List<SupplyInfo> IPPSupplyValues { get; set; } = new();
		public string IPPUUID { get; set; } = "N\\A";
		public string IPPLocation { get; set; } = "N\\A";
		//SupportedJobAttributes
		public List<string> IPPSupportedMedia { get; set; } = new();
		public List<string> IPPSupportedMediaSource { get; set; } = new();
		public List<string> IPPSupportedMediaType { get; set; } = new();
		public List<string> IPPSuppportedOutputBin { get; set; } = new();
		public List<string> IPPSupportedCollate { get; set; } = new();
		public List<Finishings> IPPSupportedFinishings { get; set; } = new();
		public List<string> IPPSupportedSides { get; set; } = new();

		public Printer() //dont use
		{
		
		}

		public Printer(string ip, string nickname)
		{
			this.IP = ip;
			this.IPPUri = new($"ipp://{ip}:631");
			this.Nickname = nickname;
			
		}

		

		public async Task<bool> RefreshValues()
		{
			if (!await Helper.CheckIP(IP)) { return false; }
			GetPrinterAttributesRequest req = new()
			{
				PrinterUri = new($"ipp://{IP}:631"),
			};

			GetPrinterAttributesResponse response = await ippCli.GetPrinterAttributesAsync(req);
			if (response is null) { return false; }
			IPPSupportedMedia.Clear();
			IPPSupportedMediaSource.Clear();
			IPPSupportedMediaType.Clear();
			IPPSuppportedOutputBin.Clear();
			IPPSupportedCollate.Clear();
			IPPSupportedFinishings.Clear();
			IPPSupportedSides.Clear();
			IPPSupplyValues.Clear();

			foreach (IppAttribute at in response.Sections[1].Attributes)
			{
				switch (at.Name)
				{
					case "media-supported":
						IPPSupportedMedia.Add(at.Value.ToString());
						break;
					case "media-source-supported":
						IPPSupportedMediaSource.Add(at.Value.ToString());
						break;
					case "media-type-supported":
						IPPSupportedMediaType.Add(at.Value.ToString());
						break;
					case "output-bin-supported":
						IPPSuppportedOutputBin.Add(at.Value.ToString());
						break;
					case "sides-supported":
						IPPSupportedSides.Add(at.Value.ToString());
						break;
					case "finishings-supported":
						IPPSupportedFinishings.Add((Finishings)at.Value);
						break;
					case "multiple-document-handling-supported":
						IPPSupportedCollate.Add(at.Value.ToString());
						break;



					case "printer-name":
						IPPName = at.Value.ToString();
						break;
					case "printer-make-and-model":
						IPPPNameInfo = at.Value.ToString();
						break;
					case "printer-firmware-version":
						IPPFirmwareInstalled = at.Value.ToString();
						break;
					case "pages-per-minute":
						IPPPPM = at.Value.ToString();
						break;
					case "color-supported":
						IPPColorSupported = at.Value.ToString();
						break;
					case "printer-uuid":
						IPPUUID = at.Value.ToString();
						break;
					case "printer-location":
						IPPLocation = at.Value.ToString();
						break;

					case "printer-state":
						IPPPrinterState = ((PrinterState)at.Value).ToString();
						break;
					case "printer-state-reasons":
						IPPPrinterStateMessage = at.Value.ToString();
						break;
					case "printer-supply":
						SupplyInfo info = new();
						foreach (string temp in at.Value.ToString().Split(";").ToList())
						{
							try
							{
								if (temp.Contains("level")) { info.percent = int.Parse(temp.Split("=")[1]); }
								else if (temp.Contains("colorantname")) { info.supplyname = temp.Split("=")[1]; } //the[1] gets the value after the =
							}
							catch
							{

							}
						}
						IPPSupplyValues.Add(info);
						break;

					default:
						break;
				}

			}
			return true;
		}

		public async Task<bool> TrySendJob9100(FileInfo file)
		{
			System.Net.Sockets.TcpClient tcpClient = new();
			try
			{
				await tcpClient.ConnectAsync(IP, 9100);
			}
			catch
			{
				return false;
			}
			await tcpClient.GetStream().WriteAsync(File.ReadAllBytes(file.FullName));
			tcpClient.Close();
			return true;
		}

		public async Task<PrintJob> TrySendJob(FileInfo file, PrintJob.JobParams param)
		{
			List<IppAttribute> ja = new();
			ja.Add(new IppAttribute(Tag.BegCollection, "media-col", ""));
			ja.Add(new IppAttribute(Tag.MemberAttrName, "", "media-source"));
			ja.Add(new IppAttribute(Tag.Keyword, "", param.sourceTray));
			ja.Add(new IppAttribute(Tag.EndCollection, "", ""));
			ja.Add(new IppAttribute(Tag.Keyword, "output-bin", param.outputTray));
			ja.Add(new IppAttribute(Tag.Keyword, "sides", param.duplexing));
			ja.Add(new IppAttribute(Tag.Keyword, "multiple-document-handling", param.collation));
			if (param.finishing != Finishings.none)
			{
				ja.Add(new IppAttribute(Tag.Enum, "finishings", (int)param.finishing));
			}
			MemoryStream ms = new(File.ReadAllBytes(file.FullName));
			PrintJobRequest req = new()
			{
				NewJobAttributes = new NewJobAttributes()
				{
					Copies = param.copies,
					JobName = "PTJob",
					Media = param.media,
					AdditionalJobAttributes = ja,
				},
				PrinterUri = new($"ipp://{IP}:631"),
				Document = ms,
			};

			
			try
			{
				var res = await ippCli.PrintJobAsync(req);
				var temp = new PrintJob(ippCli, new(res.JobUri), new($"ipp://{IP}:631"));
				jobsSent.Add(temp);
				return temp;
			}
			catch(Exception e)
			{
				System.Windows.MessageBox.Show("Error, server busy or another error.");
				return null;
				
			}
		}

		public async Task<bool> CancelJobs()
		{
			foreach (PrintJob pj in jobsSent)
			{				
				await pj.TryCancel(IPPUri); //try sending job, no matter if it succeeds or not we will wait til next clock to try again				
			}
			return true;
		}

		public static FileInfo SavePrinter(DirectoryInfo directoryToSaveTo, Printer printerToSave)
		{
			string json = JsonSerializer.Serialize(printerToSave);
			FileInfo file = new(directoryToSaveTo.FullName + "\\" + printerToSave.Nickname);
			File.WriteAllText(file.FullName, json);
			return file;
		}

		public static Printer ReadFromFile(FileInfo file)
		{
			string json = File.ReadAllText(file.FullName);
			return JsonSerializer.Deserialize<Printer>(json);	
			
		}
	}
}
