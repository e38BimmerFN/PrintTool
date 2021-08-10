using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace PrintTool
{
	public class Logger
	{
		string NAME { get; set; }
		string FILENAME { get; set; }
		string FILELOC { get; set; }
		List<TextBox> boxes { get; set; }


		public Logger(string name = "Application", string location = @"Data\Logs\Temp\")
		{
			FILELOC = location;
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
			foreach (TextBox box in boxes)
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