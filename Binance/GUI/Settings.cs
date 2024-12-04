using System;
using System.Windows.Forms;
using UAPI;

namespace Binance
{
	public partial class Settings : Form
	{
		Timer StatsQueryUpdateTimer = new Timer();
		public bool ShuttingDown = false;

		bool ShowDiagnostics = true;

		public Settings()
		{
			InitializeComponent();
			Text = $"Updata {FeedConfig.Name()} Settings";
		}

		void FillSettingsControls()
		{
			tbBaseUrl.Text = Binance.Constants.BaseUrl;
			tbApiKey.Text = Binance.Constants.APIKey;
		}

		private void Settings_Load(object sender, EventArgs e)
		{
			try
			{
				tsVersion.Text = string.Format("Version {0}", Binance.I.Version);
				cbGeneralLogLevel.SelectedIndex = cbGeneralLogLevel.FindString(Log.Level.ToString());

#if DEBUG
				ShowDiagnostics = true;
#endif

				if (ShowDiagnostics)
				{
					tbSettingsTabs.SelectedIndex = tbSettingsTabs.TabCount - 1;
				}
				else
				{
					var DiagnosticsTabPage = tbSettingsTabs.TabPages["tpDiagnostics"];
					if (DiagnosticsTabPage != null)
						tbSettingsTabs.TabPages.Remove(DiagnosticsTabPage);
				}

				FillSettingsControls();

				StatsQueryUpdateTimer_Tick(this, new EventArgs());
				StatsQueryUpdateTimer.Tick += StatsQueryUpdateTimer_Tick;
				StatsQueryUpdateTimer.Interval = 1000;
				StatsQueryUpdateTimer.Start();
			}
			catch (Exception x)
			{
				tsVersion.Text = x.Message;
			}
		}

		void StatsQueryUpdateTimer_Tick(object sender, EventArgs e)
		{
			lvStats.Items[1].SubItems[1].Text = $"Connected {DateTime.Now}";
		}

		private void Settings_FormClosing(object sender, FormClosingEventArgs e)
		{			
			if (cbGeneralLogLevel.SelectedItem != null)
			{
				if (Enum.TryParse<LogLevel>(cbGeneralLogLevel.Items[cbGeneralLogLevel.SelectedIndex].ToString(), out var Level))
					Log.Level = Level;
			}

			if (!ShuttingDown)
			{
				Hide();
				e.Cancel = true;
			}
			else
			{
				StatsQueryUpdateTimer.Stop();
			}

			Binance.I.SaveSettings();
		}

		private void cbGeneralLogLevel_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbGeneralLogLevel.SelectedItem != null)
			{
				if (Enum.TryParse<LogLevel>(cbGeneralLogLevel.Items[cbGeneralLogLevel.SelectedIndex].ToString(), out var Level))
					Log.Level = Level;
			}
		}

		private void btRestartFeed_Click(object sender, EventArgs e)
		{
			Binance.I.raise_OnRestartMe();
		}

		private void btApply_Click(object sender, EventArgs e)
		{
			Binance.Constants.BaseUrl = tbBaseUrl.Text;
			Binance.Constants.APIKey = tbApiKey.Text;
		}
	}
}
