using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
	class Firmware
	{
		public static async Task DLAndSend(string filename, string website, Logger logger, System.Windows.Controls.Button button, System.Threading.CancellationToken token)
		{
			Process usbsend = new();

			button.IsEnabled = false;
			button.Content = "Proccessing..";
			await logger.Log("Downloading " + website + filename);
			await Helper.DownloadOrCopyFile(filename, website);
			await logger.Log("Download success.");
			await logger.Log("Sending firmware to printer");
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

		}
	}
}
