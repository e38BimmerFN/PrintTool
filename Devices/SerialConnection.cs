using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace PrintTool
{
    public class SerialConnection
    {
        private SerialPort port;
        public List<string> data = new();
        private List<System.Windows.Controls.TextBox> textBoxes;
        Logger logs;

        public SerialConnection(string PortName, Logger logsin)
        {
            port = new SerialPort();
            port.PortName = PortName;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            logs = logsin;
        }
        public void Connect()
        {
            port.Open();
        }

        public void ClosePort()
        {
            port.Close();
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            
        }

        public void AddTextBox(System.Windows.Controls.TextBox box)
        {
            textBoxes.Add(box);
        }

        public void SendData(string data)
        {
            port.WriteLine(data);
        }

        public async Task<string> WriteToFile()
        {
            var file = System.IO.File.CreateText(port.PortName);
            foreach(string line in data) { await file.WriteLineAsync(line); }
            return port.PortName;
        }

        public static string[] GetPorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
