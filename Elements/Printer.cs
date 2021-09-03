using SharpIpp;
using SharpIpp.Exceptions;
using SharpIpp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace PrintTool
{
	public class Printer
	{

		//Connections
		public string PrinterIp { get; set; } = "15.86.118.0";
		public string DartIp { get; set; } = "15.86.118.0";

		// saved printer data
		public string Model { get; set; } = "";
		public string Id { get; set; } = "";
		public string Engine { get; set; } = "";
		public string Type { get; set; } = "";


		//supported job att
		public string SupportedMedia { get; set; } = "";
		public string DuplexOptioons { get; set; } = "";
		public string SourceTray { get; set; } = "";
		public string OutputBin { get; set; } = "";
		public string CollateOptions { get; set; } = "";
		public string PaperTypes { get; set; } = "";
		public string Finishings { get; set; } = "";


		//printer attributes


	}
}
