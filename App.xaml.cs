using System.Windows;

namespace PrintTool
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App() : base()
		{
			this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
		}

		private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show("Unhandled Event occured :\n" + e.Exception.Message);
		}
	}
}
