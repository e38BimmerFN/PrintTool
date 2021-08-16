using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
	class Firmware
	{
		public static async Task DLAndSend(string filename, string website, Printer printer, System.Windows.Controls.Button button, System.Threading.CancellationToken token)
		{
			Process usbsend = new();
			button.IsEnabled = false;
			button.Content = "Proccessing..";
			await printer.Log("Downloading " + website + filename);
			await Helper.DownloadOrCopyFile(filename, website);
			await printer.Log("Download success.");
			await printer.Log("Sending firmware to printer");
			usbsend.StartInfo.FileName = "Services\\USBSend.exe";
			usbsend.StartInfo.Arguments = filename;
			usbsend.StartInfo.CreateNoWindow = true;
			usbsend.StartInfo.RedirectStandardOutput = true;
			usbsend.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => { await printer.Log(e.Data); });
			usbsend.Start();
			await usbsend.WaitForExitAsync(token);
			if (usbsend.ExitCode == 0)
			{
				MessageBox.Show("Firmware upgrade success!");
			}
			else
			{
				MessageBox.Show("Firmware upgrade error / canceled. Check USB Connection.");
			}
			File.Delete(filename);
			try { usbsend.Close(); }
			catch { }
			button.IsEnabled = true; ;
			button.Content = "Download and Send";
		}
	}
}
