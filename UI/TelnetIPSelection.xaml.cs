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
    /// Interaction logic for TelnetIPSelection.xaml
    /// </summary>
    public partial class TelnetIPSelection : Window
    {
        public bool skipdart = false;
        public bool skipprint = false;


        public TelnetIPSelection()
        {
            InitializeComponent();
        }

        private async void OkayButton_Click(object sender, RoutedEventArgs e)
        {                 
            if (await Helper.CheckIP(PrinterIPEntry.Text))
            {
                
            }
            else
            {
                if(MessageBox.Show("Cannot communicate with printer IP.  Press OK to not connect, cancel to re-enter ip.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                {
                    skipprint = true;
                }
                else
                {
                    return;
                }
            }            
            if (await Helper.CheckIP(DartIpEntry.Text))
            {
                
            }
            else
            {
               if( MessageBox.Show("Cannot communicate with Dart. Press OK to not connect, cancel to re-enter ip.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                {
                    skipdart = true;
                }
                else
                {
                    return;
                }
            }
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DartIpEntry_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (DartIpEntry.Text.Contains("Input"))
            {
                DartIpEntry.Text = "";
            }
        }

        private void PrinterIPEntry_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (PrinterIPEntry.Text.Contains("Input"))
            {
                PrinterIPEntry.Text = "";
            }
        }
    }
}
