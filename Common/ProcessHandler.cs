using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
namespace PrintTool
{
	public class ProcessHandler
	{

		private string fileToStart = "";
		private string args = "";
		private Logger outputDisplay;
		private Process pc;

		public ProcessHandler(string fileToStart, string args, Logger outputDisplay)
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

		public void WriteString(string command)
		{
			pc.StandardInput.WriteLine(command);
		}

		private async void DataReceived(object sender, DataReceivedEventArgs e)
		{
			await outputDisplay.Log(e.Data + "\r\n");
		}

		private void WriteData(string data)
		{
			pc.StandardInput.WriteLine(data);
		}
	}
}
