using System;
using System.Collections.Generic;
using UAPI;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using RequestParameters = UAPI.RequestParameters;

namespace Binance
{
	public partial class Binance : UAPI.UAPI
	{
		public static Binance I { get; private set; } = null;

		public string Version { get; private set; } = "";

		public override string Source { get { return FeedConfig.Source(); } }

		Settings Settings = new Settings();

		#region Setup

		public Binance() : base(FeedConfig.FeedName())
		{
			I = this;

			try
			{
				var VersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
				Version = VersionInfo.FileVersion;
			}
			catch (Exception e)
			{
				Log.Err("Failed to retrieve version info - {0}", e.ToString());
			}

#if DEBUG
			Log.Level = LogLevel.Verbose;
#else
			// Recommended to load/save this from a settings file
			Log.Level = LogLevel.Info;
#endif

			Log.Dbg($"Starting {FeedConfig.Name()} - Version {Version}");
		}
		#endregion

		#region Startup

		public override Response Initialize(out InitializeParameters Params)
		{
			Log.Vbs("Initialize");

			Params = new InitializeParameters()
			{
				Config = new Configuration()
				{
					Description = Source,
					CandleTimeStampStyle = CandleTimeStampStyleKind.StartOfCandle,
					AdjustFinalCandleToQuote = false,
					IsTradable = true
				},

				TimeFrameSupported = new TimeFrameSupport(),
				InstrumentSelectorControl = new Selector(),
				TradeDialog = new TradeDialog(),
				DailyRefreshTimeUTC = new TimeSpan(1, 0, 0),
			};

			Params.TimeFrameSupported.Support(
				new TimeFrame[]
				{
					TimeFrame.IntervalKind.Minute,
					new TimeFrame(TimeFrame.IntervalKind.Minute, 5),
					new TimeFrame(TimeFrame.IntervalKind.Minute, 15),
					new TimeFrame(TimeFrame.IntervalKind.Minute, 30),
					new TimeFrame(TimeFrame.IntervalKind.Minute, 60),
					TimeFrame.IntervalKind.Day,
				});

			LoadSettings();
			InitializeProcessors();
			InitializeConnection();

			return true;
		}

		public override void ClientConnected()
		{
			Log.Vbs("Connected to Updata");
		}

		public override Command RequestStatus()
		{
			if (ConnectState.State == ConnectKind.Connected)
				return new Command(Command.Kind.AbleToRequest, $"{FeedConfig.Name()} is ready to return data.");
			else
				return new Command(Command.Kind.UnableToRequest, $"{FeedConfig.Name()} is {ConnectState.State}.");
		}

		#endregion

		#region Data Requests

		public override Response OpenInstrument(RequestParameters Request, InstrumentIdentifier InId, ref InstrumentInfo Info)
		{
			Log.Vbs("OpenInstrument({0}, {1}, {2})", Request, InId, Info);

			var Response = GetOrCreateInstrument(InId, out var Instrument);
			if (!Response)
				return Response;

			if (!Instrument.SubscriptionFlags.HasFlag(UpdateFlags.Quotes) || Instrument.Info == null)
			{
				if (Info.InfoDateTime != default(DateTime))
				{
					Instrument.Info = new InstrumentInfo();
					Instrument.Info.SetInfo(Info);
				}

				Instrument.SubscriptionFlags |= UpdateFlags.Quotes;

				QuoteRequest QuoteRequest = new QuoteRequest()
				{
					AsyncId = Request.AsyncId,
					Instrument = Instrument
				};

				this.Request(QuoteRequest, Request.Priority);

				return Response.Kind.Async;
			}

			Info.SetQuote(Instrument.Info);
			return true;
		}

		public override Response OpenInstruments(Tuple<RequestParameters, InstrumentIdentifier, InstrumentInfo>[] OpenRequests, ConstituentsKind ConstituentsKind)
		{
			Log.Vbs("OpenInstruments(Count = {0})", OpenRequests.Length);
			foreach (var OpenRequest in OpenRequests)
			{
				var Info = OpenRequest.Item3;
				if (OpenInstrument(OpenRequest.Item1, OpenRequest.Item2, ref Info) == Response.Kind.Success && Info != null)
					OpenRequest.Item3.SetQuote(Info);
			}
			return true;
		}

		public override Response OpenInstrumentWithTimeSeries(RequestParameters OpenInstrumentRequest, InstrumentIdentifier InId, RequestParameters TimeSeriesRequest, TimeSeriesParameters Params, ref InstrumentInfo Info, ref ITimeSeriesValues ReturnValues, out Response TimeSeriesResponse)
		{
			Log.Vbs("OpenInstrumentWithTimeSeries({0}, {1}, {2}, {3}, {4})", OpenInstrumentRequest, InId, TimeSeriesRequest, Params, Info);

			var Response = GetOrCreateInstrument(InId, out var Instrument);
			if (!Response)				
			{
				TimeSeriesResponse = Response;
				return Response;
			}

			Instrument.SubscriptionFlags |= UpdateFlags.Charts;

			TimeSeriesResponse = RequestTimeSeries(TimeSeriesRequest, InId, Params, ref ReturnValues);

			return OpenInstrument(OpenInstrumentRequest, InId, ref Info); ;
		}

		public override Response OpenTimeSeries(RequestParameters Request, InstrumentIdentifier InId, TimeFrame TimeFrame)
		{
			Log.Vbs("OpenTimeSeries({0}, {1}, {2})", Request, InId, TimeFrame);

			var Response = GetOrCreateInstrument(InId, out var Instrument, false);
			if (!Response)
				return Response;

			Instrument.SubscriptionFlags |= UpdateFlags.Charts;
			return true;
		}

		public override Response RequestTimeSeries(RequestParameters Request, InstrumentIdentifier InId, TimeSeriesParameters Params, ref ITimeSeriesValues ReturnValues)
		{
			Log.Vbs("RequestTimeSeries({0}, {1}, {2})", Request, InId, Params);

			var Response = GetOrCreateInstrument(InId, out var Instrument);
			if (!Response)
				return Response;

			TimeSeriesRequest RestRequest = new TimeSeriesRequest()
			{
				AsyncId = Request.AsyncId,
				Params = Params,
				Instrument = Instrument,
			};

			this.Request(RestRequest, Request.Priority);

			return Response.Kind.Async;
		}

		public override Response CloseInstrument(InstrumentIdentifier InId, UpdateFlags Flags)
		{
			Log.Vbs("CloseInstrument({0}, {1})", InId, Flags);

			if (GetOrCreateInstrument(InId, out var Instrument, false))
			{
				Instrument.SubscriptionFlags &= ~Flags;
				if (Instrument.SubscriptionFlags == UpdateFlags.None)
					Instruments.TryRemove(InId, out var Removed);
			}

			return true;
		}

		#endregion

		#region Trading

		public override Response SubmitOrder(Order Order, ref Order.Result Result)
		{
			return base.SubmitOrder(Order, ref Result);
		}

		#endregion

		#region Maintenance

		public override void OnDailyRefresh()
		{
			Log.Vbs("OnDailyRefresh()");

			raise_OnReRequest();
		}
		
		public override void ClientDisconnected()
		{
			Log.Vbs("Disconnected from Updata");
		}

		#endregion

		#region Shutdown

		public override Response Shutdown()
		{
			Log.Vbs("Shutdown()");

			SaveSettings();
			ShutdownRequestProcessors();
			ShutdownConnection();

			Settings.ShuttingDown = true;
			Settings.Close();

			return true;
		}
		#endregion

		#region Settings
		public override void SettingsDialog()
		{
			Log.Vbs("SettingsDialog()");

			try
			{
				if (!Settings.Visible)
					Settings.Show();
			}
			catch { }
		}
		#endregion
	}
}
