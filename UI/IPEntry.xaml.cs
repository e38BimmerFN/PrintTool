using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for IPEntry.xaml
	/// </summary>
	public partial class IPEntry : Window
	{
		public IPEntry()
		{
			InitializeComponent();
		}

		string ip { get { return ipaddress.Text; } }
		
	

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private async void doneButton_Click(object sender, RoutedEventArgs e)
		{
			doneButton.IsEnabled = false;
			if (await Helper.CheckIP(ipaddress.Text))
			{
				DialogResult = true;
			}
			else
			{
				MessageBox.Show("Cannot communicate with printer. IP Might have changed or printer is off.", "Cannot communicate with printer", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			doneButton.IsEnabled = true;
		}
	

		private void ipaddress_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (ipaddress.Text.Contains("Input"))
			{
				ipaddress.Text = "";
			}
		}
	}
}
