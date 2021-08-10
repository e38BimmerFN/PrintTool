using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;

namespace PrintTool
{
	public class Logger
	{
		string NAME = "Application";
		string FILENAME;
		string FILELOC = @"Data\Logs\Temp\";
		List<TextBox> boxes = new();
		
		

		public Logger(string name = "Application")
		{			
			NAME = name;
			FILENAME = NAME + "_Log.txt";
			if (File.Exists(FILELOC + FILENAME))
			{
				File.Delete(FILELOC + FILENAME);
			}
		}
		public void AddTextBox(TextBox box)
		{
			boxes.Add(box);
		}		

		public async void Log(string log)
		{			
			string output = "[" + DateTime.Now.ToShortTimeString() + "] " + log + "\n";
			await File.AppendAllTextAsync(FILELOC + FILENAME, output);				
			foreach(TextBox box in boxes)
			{
				box.Dispatcher.Invoke(new Action(() =>
				{
					box.AppendText(output);
					box.ScrollToEnd();
				}));
			}					
		}
		
		public void SaveLogs(string topath)
		{
			File.Copy(FILELOC + FILENAME, topath + FILENAME);
		}

	}
}
