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
        
        public static async Task PrintGenerator(List<string> args)
        {
            
            string start = await PJLStart(args);
            string middle = await printGeneratorCaller(args);
            string end = await PJLEnd(args);


            string alltogether = start + middle + end;
            if (File.Exists(filename)) { File.Delete(filename); }
            StreamWriter tempFile = File.CreateText(filename);
            tempFile.Write(alltogether);
            tempFile.Close();
            return filename;


        }

        private static async Task<string> PJLStart(List<string> args)
        {
            List<string> returnargs = new();
            string duplexOnOrOff = "OFF";
            string duplexBinding = "SHORTEDGE";

            
            
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "\%-12345X";
            pjlPart = pjlPart + escapeSequence + "@PJL\r\n"
                + "@PJL RESET\r\n"
                + "@PJL JOB NAME = " + jobname + "\r\n"
                + "@PJL SET JOBNAME = " + jobname + "\r\n"
                + "@PJL SET DUPLEX = " + duplexOnOrOff + "\r\n"
                + "@PJL SET BINDING = " + duplexBinding + "\r\n"
                + "@PJL SET COPIES = " + printCopies.Text + "\r\n"
                + "@PJL SET PAPER = " + paperTypeSelection.Text + "\r\n"
                + "@PJL SET OUTBIN = " + printOutputTray.Text + "\r\n"
                + "@PJL SET MEDIASOURCE = " + printSourceTray.Text + "\r\n";
            return pjlPart;
        }

        private static async Task<string> PJLEnd(string jobname)
        {
            char escapeCharacter = (char)27;
            string escapeSequence = escapeCharacter + "%-12345X";
            string endpart = escapeSequence + "@PJL\r\n"
                + "@PJL EOJ NAME = " + jobname + "\r\n"
                + escapeSequence + "\r\n";

            return endpart;
        }

        

        private static async Task<string> CreatePCL(int pages)
        {
            return "";
        }
        private static async Task<string> CreatePS(int pages)
        {
            string output = "@PJL ENTER LANGUAGE=POSTSCRIPT \r\n" + "/Times-Roman findfont 14 scalefont setfont \r\n";

            for (int i = 0; i < pages; i++)
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

        private static async Task<string> CreateESCP(int pages)
        {
            return "";
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


    }
}
