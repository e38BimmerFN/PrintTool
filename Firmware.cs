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
    class Firmware
    {
        
        const string YOLOWEBSITE = "http://sgpfwws.ijp.sgp.rd.hpicorp.net/cr/bpd/sh_release/yolo_sgp/";
        const string DUNEWEBSITE = "https://dunebdlserver.boi.rd.hpicorp.net/media/published/daily_builds/";
        const string DUNEUTILITY = @"\\jedifiles01.boi.rd.hpicorp.net\Oasis\Dune\Builds\Utility";

       
          

        

        public static async Task downloadAndSendUSB(string filename, string website, Logger logger, System.Windows.Controls.Button button, System.Threading.CancellationToken token)
        {
            Process usbsend = new();
            button.IsEnabled = false;
            button.Content = "Proccessing..";
            logger.Log("Downloading " + website + filename);
            await Tasks.downloadFile(filename, website);
            logger.Log("Download success.");
            logger.Log("Sending firmware to printer");
            usbsend.StartInfo.FileName = "USBSend.exe";
            usbsend.StartInfo.Arguments = filename;
            usbsend.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            usbsend.StartInfo.RedirectStandardOutput = true;
            usbsend.Start();
            logger.AddOutputStream(usbsend.StandardOutput);
            await usbsend.WaitForExitAsync(token);
            if (usbsend.ExitCode == 0)
            {
                MessageBox.Show("Firmware upgrade success!");
            }
            else
            {
                MessageBox.Show("Firmware upgrade error / canceled");
            }
            File.Delete(filename);
            try { usbsend.Close(); }
            catch { }

            button.IsEnabled = true; ;
            button.Content = "Download and Send";
            
        }
                
        
        

    }
}
