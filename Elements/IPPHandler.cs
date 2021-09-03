using SharpIpp;
using SharpIpp.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
	public class IPPHandler
	{
		SharpIppClient cli = new();
		private Uri uri;
		public string Ip { get { return uri.ToString(); } set { uri = new($"ipp://{value}:631"); } }
		private Uri lastJobUri;


		public async Task<PrintJobResponse> SendJob(string file, string media, string duplex, string mediaSource, string outputTray, int copies, Finishings finish, string collate, string mediatype)
		{

			List<IppAttribute> ja = new();
			//Media source

			ja.Add(new IppAttribute(Tag.BegCollection, "media-col", ""));
			ja.Add(new IppAttribute(Tag.MemberAttrName, "", "media-source"));
			ja.Add(new IppAttribute(Tag.Keyword, "", mediaSource));


			ja.Add(new IppAttribute(Tag.EndCollection, "", ""));


			ja.Add(new IppAttribute(Tag.Keyword, "output-bin", outputTray));
			ja.Add(new IppAttribute(Tag.Keyword, "sides", duplex));
			ja.Add(new IppAttribute(Tag.Keyword, "multiple-document-handling", collate));
			if (finish != Finishings.None)
			{
				ja.Add(new IppAttribute(Tag.Enum, "finishings", (int)finish));
			}

			MemoryStream ms = new(File.ReadAllBytes(file));

			List<IppAttribute> aoa = new();
			aoa.Add(new IppAttribute(Tag.Boolean, "ipp-attribute-fidelity", true));

			PrintJobRequest req = new()
			{
				NewJobAttributes = new NewJobAttributes()
				{
					Copies = copies,
					JobName = "PTJob",
					Media = media,

					AdditionalJobAttributes = ja,

				},
				PrinterUri = new Uri(Ip),
				Document = ms,
				AdditionalOperationAttributes = aoa

			};
			PrintJobResponse res = new();
			try
			{
				res = await cli.PrintJobAsync(req);
				lastJobUri = new Uri(res.JobUri);
				return res;

			}
			catch
			{
				MessageBox.Show("Error. Server Busy");
			}

			return null;
		}

		public async Task<GetPrinterAttributesResponse> GetPrinterDetails()
		{
			GetPrinterAttributesRequest req = new()
			{
				PrinterUri = new Uri(Ip)
			};
			try
			{
				GetPrinterAttributesResponse res = await cli.GetPrinterAttributesAsync(req);
				return res;
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Data.ToString());

			}
			return null;
		}

		public async Task<GetJobAttributesResponse> GetJob(Uri jobUri)
		{
			GetJobAttributesRequest req = new()
			{
				PrinterUri = new Uri(Ip),
				JobUrl = jobUri,
			};

			return await cli.GetJobAttributesAsync(req);
		}


		public async Task CancelJob()
		{
			if (lastJobUri is null) { return; }
			CancelJobRequest req = new()
			{
				PrinterUri = new Uri(Ip),
				JobUrl = lastJobUri
			};
			await cli.CancelJobAsync(req);
		}
	}
}

