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
        public IPAddress ip {get; set;}
        public bool usingSerial { get; set; }
        public List<SerialConnection> serialPorts { get; set; }
        public Dart dart;
        public PrintQueue queue;

        public Printer()
        {
            dart = new Dart();
            usingSerial = false;       
            dart.isEnabled = false;
            ip = IPAddress.Parse("0.0.0.0");
        }


        public void Save()
        {
            List<string> data = new();
            data.Add(model);
            data.Add(id);
            data.Add(engineType);
            data.Add(ip.ToString());
            data.Add(usingSerial.ToString());
            data.Add(dart.isEnabled.ToString());
            StreamWriter myFile = File.CreateText(location + data[0]);
            foreach (string entry in data)
            {
                myFile.WriteLine(entry);
            }
            myFile.Close();
        }

        public void Load(string filename)
        {
            if(filename == "" || filename == null) { return; }
            StreamReader file = File.OpenText(location +filename);
            List<string> data = new();

            model = file.ReadLine();
            id = file.ReadLine();
            engineType = file.ReadLine();
            ip = IPAddress.Parse(file.ReadLine());
            usingSerial = bool.Parse(file.ReadLine());
            dart.isEnabled = bool.Parse(file.ReadLine());
            file.Close();   
        }
    }
}
