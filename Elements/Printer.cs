﻿using SharpIpp;
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

		// saved printer data
		public string model { get; set; } = "";
		public string id { get; set; } = "";
		public string engine { get; set; } = "";
		public string type { get; set; } = "";

		

		//Connections
		public string printerIp { get; set; } = "15.86.118.0";
		public string dartIp { get; set; } = "15.86.118.0";
		public bool enableSerial { get; set; } = false;
		public bool enableDart { get; set; } = false;
		public bool enableTelnet { get; set; } = false;
		public bool enablePrinterStatus { get; set; } = false;


	}
}
