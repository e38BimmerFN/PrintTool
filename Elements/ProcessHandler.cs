using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.Threading;
namespace PrintTool
{
	public class ProcessHandler
	{

		string fileToStart = "";
		string args = "";
		TextBox outputDisplay;
		Process pc;

		public ProcessHandler(string fileToStart, string args, TextBox outputDisplay)
		{
			this.fileToStart = fileToStart;
			this.args = args;
			this.outputDisplay = outputDisplay;
		} 

		public async Task Start(CancellationToken token)
		{
			pc = new();
			pc.StartInfo.FileName = fileToStart;
			pc.StartInfo.Arguments = args;
			pc.StartInfo.CreateNoWindow = true;
			pc.StartInfo.RedirectStandardOutput = true;			
			pc.StartInfo.RedirectStandardInput = true;
			pc.OutputDataReceived += DataReceived;
			pc.Start();
			pc.BeginOutputReadLine();			
			await pc.WaitForExitAsync(token);
		}

		private async void DataReceived(object sender, DataReceivedEventArgs e)
		{
			outputDisplay.Dispatcher.Invoke(new Action(() =>
			{				
				outputDisplay.AppendText(e.Data+"\n");
				outputDisplay.ScrollToEnd();
			}));
		}

		private void WriteData(string data)
		{
			pc.StandardInput.WriteLine(data);
		}
	}
}
