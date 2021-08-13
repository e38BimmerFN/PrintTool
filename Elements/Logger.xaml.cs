using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for Log.xaml
	/// </summary>
	public partial class Logger : UserControl
	{
		public string fileName = "";
		public string fileLoc = @"Data\Logs\Temp\";
		public Logger(string fileName)
		{
			InitializeComponent();
			this.fileName = fileName;
		}


		public async Task Log(string log)
		{
			if (!log.Contains("\n") || !log.Contains("\r")) { log += "\n"; }
			LogBox.AppendText(log);
			Scroller.ScrollToBottom();

			await File.AppendAllTextAsync(fileLoc + fileName + ".txt", log);
		}
	}
}
