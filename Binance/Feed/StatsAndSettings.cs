namespace Binance
{
	public partial class Binance : UAPI.UAPI
	{
		public class Constants
		{
			public static string BaseUrl = "API Base URL";
			public static string APIKey = "API Key Loaded From Settings";
			public static int InstrumentUpdateFrequencySeconds = 60;
			
			// Any other needed settings for your Integration
		}

		public void LoadSettings()
		{
			// Read Settings from file into your Constants above
		}

		public void SaveSettings()
		{
			// Save Constants to file
		}
	}
}
