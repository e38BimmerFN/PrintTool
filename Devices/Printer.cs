using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO;

namespace PrintTool
{
    public class Printer
    {
        private string location = @"Data\Printers\";
        public string model { get; set; }
        public string id { get; set; }
        public string engineType { get; set; }

        //Connections
        public string ip {get; set;}
        public bool usingSerial { get; set; }
        public List<SerialConnection> serialPorts { get; set; }
        public Dart dart;
        public PrintQueue queue;

        public Printer()
        {
            dart = new Dart();
            usingSerial = false;       
            dart.isEnabled = false;
            ip = "0.0.0.0";
        }


        public void Save()
        {
            List<string> data = new();
            data.Add(model);
            data.Add(id);
            data.Add(engineType);
            data.Add(ip.ToString());
            data.Add(dart.isEnabled.ToString());
            data.Add(dart.usingPorts.ToString());
            data.Add(dart.ip.ToString());
            data.Add(usingSerial.ToString());            
            StreamWriter myFile = File.CreateText(location + model);
            foreach (string entry in data)
            {
                myFile.WriteLine(entry);
            }
            myFile.Close();
        }

        public void Load(string filename)
        {
            
            if(!File.Exists(filename)) { MessageBox.Show(filename + " Doesnt exist"); return; }
			StreamReader file = File.OpenText(filename); 
            
            model = file.ReadLine();
            id = file.ReadLine();
            engineType = file.ReadLine();
            ip = file.ReadLine();
            dart.isEnabled = bool.Parse(file.ReadLine());
            dart.usingPorts = bool.Parse(file.ReadLine());
            dart.ip = file.ReadLine();
            usingSerial = bool.Parse(file.ReadLine());            
            file.Close();   
        }
    }
}
