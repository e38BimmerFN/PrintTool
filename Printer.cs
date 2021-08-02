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
    class Printer
    {
        
        public static async Task<string> PrintGenerator(List<string> args)
        {
            
            string start = await PJLStart(args);
            string lang = "";
            switch (args[3]){
                case "1":
                    lang = await CreatePS(args);
                    break;
                case "2":
                    lang = await CreatePCL(args);
                    break;
                case "3":
                    lang = await CreateESCP(args);
                    break;

            }
            string end = await PJLEnd(args);
            string alltogether = start + lang + end;
            if (File.Exists(args[0])) { File.Delete(args[0]); }
            StreamWriter tempFile = File.CreateText(args[0]);
            tempFile.Write(alltogether);
            tempFile.Close();
            return args[0];
        }

        public static async Task SendIP(string ip, string file)
        {
            byte[] data = new byte[0];
            try
            {
                data = File.ReadAllBytes(file);
            }
            catch
            {
                MessageBox.Show("File read error");
                return;
            }

            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(ip, 9100);
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                client.Close();
            }
            catch
            {
                MessageBox.Show("Incorrect IP");
                return;
            }
            return;
        }
        public static async Task SendUSB(string file)
        {
            Process usbsend = new();
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = file;
            usbsend.StartInfo.CreateNoWindow = true;
            usbsend.Start();
            await usbsend.WaitForExitAsync();
        }


        private static async Task<string> PJLStart(List<string> args)
        {
            string returnString = "";
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + @"\%-12345X";
            returnString = returnString + escapeSequence + "@PJL\r\n"
                + "@PJL RESET\r\n"
                + "@PJL JOB NAME = " + args[0]+ "\r\n"
                + "@PJL SET JOBNAME = " + args[1] + "\r\n"
                + "@PJL SET COPIES = " + args[4] + "\r\n"
                + "@PJL SET DUPLEX = " + args[5] + "\r\n"
                + "@PJL SET BINDING = " + args[6] + "\r\n"
                + "@PJL SET PAPER = " + args[7] + "\r\n"
                + "@PJL SET MEDIASOURCE = " + args[8] + "\r\n"
                + "@PJL SET OUTBIN = " + args[9] + "\r\n";
                
            return returnString;
        }


        

        

        private static async Task<string> CreatePCL(List<string> args)
        {
            return "";
        }
        private static async Task<string> CreatePS(List<string> args)
        {
            string output = "@PJL ENTER LANGUAGE=POSTSCRIPT \r\n" + "/Times-Roman findfont 14 scalefont setfont \r\n";

            for (int i = 0; i < int.Parse(args[10]); i++)
            {
                output = output
                    + "clippath stroke\r\n"
                    + "15 60 moveto\r\n"
                    + "(PrintTool) show\r\n"
                    + "20 40 moveto \r\n"
                    + "( Page number = " + (i + 1) + " ) show\r\n"
                     + "20 20 moveto \r\n"
                    + "( Source PC name  = " + Environment.MachineName + " ) show\r\n"
                    + "showpage\r\n";

            }
            return output;
        }

        private static async Task<string> CreateESCP(List<string> args)
        {
            return "";
        }

        private static async Task<string> PJLEnd(List<string> args)
        {
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "%-12345X";
            string endpart = escapeSequence + "@PJL\r\n"
                + "@PJL EOJ NAME = " + args[1] + "\r\n"
                + escapeSequence + "\r\n";

            return endpart;
        }

        

    }
}
