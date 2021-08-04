using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PrintTool
{
    static class Connections
    {
        public static void checkIP(System.Windows.Controls.TextBox entry)
        {
            string regexmatch = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$";
            var myRegex = Regex.Match(entry.Text, regexmatch);
            if (myRegex.Success)
            {
                entry.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                entry.Background = System.Windows.Media.Brushes.PaleVioletRed;
            }
        }
        
        public static void SaveDefaults(List<string> data)
        {
            if (data[0] == "")
            {
                MessageBox.Show("Please select something");
                return;
            }
            StreamWriter myFile = File.CreateText("Data\\Connections\\" + data[0]);
            foreach(string entry in data)
            {
                myFile.WriteLine(entry);
            }
            myFile.Close();
        }

        public static List<string> LoadDefaults(System.Windows.Controls.ListBox listBox)
        {
            List<string> data = new();
            if (listBox.SelectedItem == null) { MessageBox.Show("Please select item"); return data; }
            StreamReader myFile = File.OpenText(listBox.SelectedItem.ToString());
            while(myFile.EndOfStream == false)
            {
                data.Add(myFile.ReadLine());
            }
            myFile.Close();
            return data;
            
        }
    }
}
