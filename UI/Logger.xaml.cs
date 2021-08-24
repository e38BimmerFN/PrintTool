using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for Log.xaml
	/// </summary>
	public partial class Logger : UserControl
	{

		int lineCount = 0;
		public string fileName = "";
		public string fileLoc = @"Data\Logs\Temp\";
		public Logger(string fileName)
		{
			InitializeComponent();
			this.fileName = fileName;
		}


		public async Task Log(string result)
		{
			
			if (result is null or "" or "\n" or "\r" or "\r\n" or "\n\r") { return; } // removing empty lines
			lineCount++;
			result = Regex.Replace(result, "(\x9B|\x1B\\[)[0-?]*[ -\\/]*[@-~]", "");

			result = Regex.Replace(result, "(\\0)", "");
			result = Regex.Replace(result, "(\n\r)", "\r\n");
			if(!result.Contains("\n")) { result += "\r\n"; }


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

			await File.AppendAllTextAsync(fileLoc + "Log" + fileName + ".txt", result);
		}
	}
}
