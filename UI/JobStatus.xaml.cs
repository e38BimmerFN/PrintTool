using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;


namespace PrintTool
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class JobStatus : UserControl
	{

		readonly Timer pingTimer = new(500); //every 3 seconds should be good
		IPPHandler cli;
		string jobUri;
		List<int> pagesCompleted = new();
		List<int> timebetween = new();
		int ppm = 0;
		public JobStatus(string jobUri, IPPHandler cli)
		{
			InitializeComponent();
			this.cli = cli;
			this.jobUri = jobUri;
			pingTimer.Elapsed += PingTimer_Elapsed;
			pingTimer.Start();
		}

		private async void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
		{

			var jobStatus = await cli.GetJob(new Uri(jobUri));

			timebetween.Clear();
			pagesCompleted.Add(jobStatus.JobAttributes.JobImpressionsCompleted.Value);

			for (int i = 0; i < pagesCompleted.Count - 1; i++)
			{
				timebetween.Add(pagesCompleted[i + 1] - pagesCompleted[i]);
			}
			if (timebetween.Count > 2)
			{
				double average = timebetween.Average();
				average = average / 0.5;
				ppm = (int)(average * 60);
			}

			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				jobIDBox.Text = jobStatus.JobAttributes.JobId.ToString();
				jobStatusBox.Text = jobStatus.JobAttributes.JobState.ToString();
				jobStatusMessageBox.Text = jobStatus.JobAttributes.JobStateReasons[0].ToString();
				startedAtBox.Text = jobStatus.JobAttributes.TimeAtProcessing.Value.ToLongTimeString();
				if (jobStatus.JobAttributes.TimeAtCompleted is not null)
				{
					endedAtBox.Text = jobStatus.JobAttributes.TimeAtCompleted.Value.ToLongTimeString();
					timeToCompleteBox.Text = jobStatus.JobAttributes.TimeAtCompleted.Value.Subtract(jobStatus.JobAttributes.TimeAtProcessing.Value).TotalSeconds.ToString() + " Seconds";
				}


				pagesCompleteBox.Text = jobStatus.JobAttributes.JobImpressionsCompleted.ToString();
				ppmBox.Text = ppm.ToString();
			}));

			if (jobStatus.JobAttributes.JobState.ToString() is "Completed" or "Canceled")
			{
				pingTimer.Stop();
			}
		}
	}
}
