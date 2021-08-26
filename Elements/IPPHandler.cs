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
	public class IPPHandler
	{
		SharpIppClient cli = new();
		Uri ip;
		public IPPHandler(string ip)
		{
			this.ip = new Uri($"ipp://{ip}:631");
		}

		public async Task<PrintJobResponse> SendJob(string file, string media = "na_letter_8.5x11in", string duplex = "one-sided", string mediaSource = "auto", string outputTray = "face-down", int copies = 1)
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
			ja.Add(new IppAttribute(Tag.Keyword, "sides", duplex));
			PrintJobRequest req = new()
			{
				NewJobAttributes = new NewJobAttributes()
				{
					Copies = copies,
					JobName = "PrintTool",
					Media = media,
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
			try
			{
				GetPrinterAttributesResponse res = await cli.GetPrinterAttributesAsync(req);
				return res;
			}
			catch
			{
				return null;
			}

		}

		public async Task<GetJobAttributesResponse> GetJob(Uri jobUri)
		{
			GetJobAttributesRequest req = new()
			{
				PrinterUri = ip,
				JobUrl = jobUri,
			};

			return await cli.GetJobAttributesAsync(req);
		}

		public async Task<GetJobsResponse> GetJobs()
		{
			GetJobsRequest req = new()
			{
				PrinterUri = ip,
			};
			return await cli.GetJobsAsync(req);
		}
	}
}

