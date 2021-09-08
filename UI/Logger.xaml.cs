using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for Log.xaml
	/// </summary>
	public partial class Logger : UserControl
	{

		private int lineCount = 0;
		public string fileName = "";
		private string fileLoc = @"Data\Logs\Temp\";
		string linestowrite = "";
		public Logger(string fileName)
		{
			InitializeComponent();
			this.fileName = fileName;
			Timer writeClk = new Timer(5000); // write every 5 seconds
			writeClk.Elapsed += WriteClk_Elapsed;
			writeClk.Start();

		}

		private void WriteClk_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				File.AppendAllTextAsync($"{fileLoc}Log{fileName}.txt", linestowrite);
			}
			catch
			{

			}
		}



		public async Task Log(string result)
		{

			if (result is null or "" or "\n" or "\r" or "\r\n" or "\n\r") { return; } // removing empty lines
			lineCount++;
			result = Regex.Replace(result, "(\x9B|\x1B\\[)[0-?]*[ -\\/]*[@-~]", ""); //remove ansi

			result = Regex.Replace(result, "(\\0)", " "); //replace null

			//fixing malformed newlines.
			result = Regex.Replace(result, "(\r\r)", "\r", RegexOptions.Multiline);
			result = Regex.Replace(result, "(\n\n)", "\n", RegexOptions.Multiline);
			result = Regex.Replace(result, "(\n\r)", "\r\n", RegexOptions.Multiline);
			result = Regex.Replace(result, "(\r\r\n\n)", "\r\n", RegexOptions.Multiline);



			if (!result.Contains("\n")) { result += "\r\n"; }


			LogBox.Dispatcher.Invoke(new Action(() =>
			{
				if (lineCount > 1500)
				{
					LogBox.Text = "";
					lineCount = 0;
				}
				LogBox.AppendText(result);
			}));

			Scroller.Dispatcher.Invoke(new Action(() =>
			{
				Scroller.ScrollToBottom();
			}));

			linestowrite += result;

		}
	}
}
