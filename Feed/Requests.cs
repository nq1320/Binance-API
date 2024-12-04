using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UAPI.Extensions;
using UAPI;

namespace Binance
{
	public partial class Binance : UAPI.UAPI
	{
		public class SymbolSearchRequest : IRequest
		{
			public List<Instrument> Results = new List<Instrument>();

			public string Keywords { get; set; }
			public override string Url { get { return "Your API Search Endpoint with Parameters"; } }

			public override void ProcessResponse(dynamic Response)
			{
				if (Response == null)
				{
					Result = "No Response";
					return;
				}

				// Adjust this loop to fit your API returned results:

				foreach (var Result in Response["Results"])
				{
					InstrumentIdentifier InId;					

					// Insert your API returned values here:

					string 
						Symbol = "",
						LongName = "",
						Exchange = "",
						AssetType = "",
						Currency = "";

					TimeZoneInfo TimeZone = TimeZoneInfo.Utc;

					int MarketOpenTimeInSeconds = 0;
					int MarketCloseTimeInSeconds = 86400;

					////////////////////////////////////////

					if (string.IsNullOrEmpty(Symbol))
						continue;
										
					var MetaIds = new Dictionary<string, string>()
					{
						{ "TYPE", AssetType },
						// Add any other MetaIds as needed for unique instrument classification
					};

					InId = InstrumentIdentifier.Build(FeedConfig.FeedName(), Symbol, Currency, MetaIds);

					var Instrument = new Instrument()
					{
						InId = InId,
						Info = new InstrumentInfo()
						{
							LongName = LongName,
							AssetType = AssetType,
							Exchange = Exchange,
							TimeZone = TimeZone,
							StartTime = MarketOpenTimeInSeconds,
							EndTime = MarketCloseTimeInSeconds,
							InfoDateTime = DateTime.UtcNow,
						}
					};

					Results.Add(Instrument);
				}
			}
		}

		public class InfoRequest : IRequest
		{
			public Instrument Instrument = null;

			public override string Url { get { return "Your API Instrument Info lookup Endpoint with Parameters"; } }

			public override void ProcessResponse(dynamic Response)
			{
				if (Response == null)
				{
					Result = "No Data Returned";
					return;
				}
				
				// Insert your API returned values here:

				string LongName = "";
				string AssetType = "";
				TimeZoneInfo TimeZone = TimeZoneInfo.Utc;
				int MarketOpenTimeInSeconds = 0;
				int MarketCloseTimeInSeconds = 86400;

				////////////////////////////////////////

				Instrument.Info.LongName = LongName;
				Instrument.Info.AssetType = AssetType;
				Instrument.Info.TimeZone = TimeZone;
				Instrument.Info.StartTime = MarketOpenTimeInSeconds;
				Instrument.Info.EndTime = MarketCloseTimeInSeconds;

				Instrument.Info.InfoDateTime = DateTime.UtcNow;
			}
		}

		public class QuoteRequest : IRequest
		{
			public long AsyncId { get; set; }
			public Instrument Instrument { get; set; }

			public override string Url
			{
				get
				{
					switch (Instrument.InId["TYPE"])
					{
						default:
							return "Your API Quote lookup Endpoint with Parameters appropriate for the Instrument AssetType";
					}
				}
			}

			public override void ProcessResponse(dynamic Response)
			{
				if (Response == null)
				{
					Result = "No Data Returned";
					I.raise_OnRequestFinished(AsyncId, Result);
					return;
				}

				if (Instrument.Info == null)
				{
					var InfoRequest = new InfoRequest() { Instrument = Instrument };
					I.Request(InfoRequest);
					if (!InfoRequest.Wait())
					{
						Result = $"{Instrument.InId} Failed to get Instrument Details";
						I.raise_OnRequestFinished(AsyncId, Result);
						return;
					}
				}

				// Insert your API returned values here:

				DateTime QuoteDateTime = DateTime.UtcNow;

				double
					Bid = 0,
					Ask = 0,
					BidSize = 0,
					AskSize = 0,
					Open = 0,
					High = 0,
					Low = 0,
					Price = 0,
					Volume = 0,
					CumulativeVolume = 0,
					PrevClose = 0;

				/////////////////////////////////////////

				QuoteDateTime = QuoteDateTime.Add(DateTime.UtcNow.TimeOfDay);
				if (QuoteDateTime.TimeOfDay.TotalSeconds > 0 && Instrument.Info.TimeZone != null && Instrument.Info.TimeZone.BaseUtcOffset.TotalSeconds > 0)
					QuoteDateTime = TimeZoneInfo.ConvertTimeFromUtc(QuoteDateTime, Instrument.Info.TimeZone);

				Instrument.Info.QuoteDateTime = QuoteDateTime;

				Instrument.Info.Bid = Bid;
				Instrument.Info.Ask = Ask;
				Instrument.Info.BidSize = BidSize;
				Instrument.Info.AskSize = AskSize;

				Instrument.Info.Open = Open;
				Instrument.Info.High = High;
				Instrument.Info.Low = Low;
				Instrument.Info.Mid = Instrument.Info.Last = Price;
				Instrument.Info.LastVolume = Volume;
				Instrument.Info.CumulativeVolume = CumulativeVolume;

				Instrument.Info.PreviousClose = PrevClose;

				I.raise_OnRequestInstrumentInfoFinished(AsyncId, Instrument.Info);
			}
		}

		public class UpdateRequest : IRequest
		{
			public Instrument Instrument { get; set; }

			public override string Url
			{
				get
				{
					switch (Instrument.InId["TYPE"])
					{
						default:
							return "Your API Quote lookup Endpoint with Parameters appropriate for the Instrument AssetType";
					}
				}
			}

			public override void ProcessResponse(dynamic Response)
			{
				if (Response == null)
					return;				
				
				// Insert your API returned values here:

				DateTime QuoteDateTime = DateTime.UtcNow;

				double
					Bid = 0,
					Ask = 0,
					BidSize = 0,
					AskSize = 0,
					Price = 0,
					Volume = 0,
					CumulativeVolume = 0;

				/////////////////////////////////////////

				Instrument.Info.QuoteDateTime = QuoteDateTime;

				Instrument.Info.Bid = Bid;
				Instrument.Info.Ask = Ask;
				Instrument.Info.BidSize = BidSize;
				Instrument.Info.AskSize = AskSize;
				Instrument.Info.Mid = Instrument.Info.Last = Price;
				Instrument.Info.LastVolume = Volume;
				Instrument.Info.CumulativeVolume = CumulativeVolume;

				var Updates = new List<Price>();

				Updates.Add(new Price(PriceField._BID, Instrument.Info.Bid, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._ASK, Instrument.Info.Ask, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._BID_SIZE, Instrument.Info.BidSize, Instrument.Info.QuoteUpdateDate,  (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._ASK_SIZE, Instrument.Info.AskSize, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._MID, Instrument.Info.Mid, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._VOLUME, Instrument.Info.LastVolume, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));
				Updates.Add(new Price(PriceField._CUM_VOLUME, Instrument.Info.CumulativeVolume, Instrument.Info.QuoteUpdateDate, (uint)Instrument.Info.QuoteUpdateTime, Instrument.SubscriptionFlags));

				Instrument.LastUpdatedUtc = DateTime.UtcNow;
				I.raise_OnPriceUpdates(Instrument.InId, Updates);

				Instrument.UpdateRequested = false;
			}
		}

		public class TimeSeriesRequest : IRequest
		{
			public long AsyncId { get; set; }
			public Instrument Instrument { get; set; }
			public TimeSeriesParameters Params { get; set; }

			public override string Url
			{
				get
				{
					// Make the appropriate Time Series request based on the instrument type:

					switch (Instrument.InId["TYPE"])
					{
						default:
							switch (Params.TimeFrame.Kind)
							{
								default:
								case TimeFrame.TimeFrameKind.Days:
									return "Your API Time Series Endpoint for Daily Data with Parameters";
								case TimeFrame.TimeFrameKind.Minutes:
									return "Your API Time Series Endpoint for Intraday Minute Data with Parameters";
								case TimeFrame.TimeFrameKind.Ticks:
									return "Your API Time Series Endpoint for Tick Data with Parameters";
							}
					}
				}
			}

			public override void ProcessResponse(dynamic Response)
			{
				var Values = Params.TimeFrame.CreateValues();

				if (Response == null)
				{
					Result = "No Data Returned";
					I.raise_OnRequestFinished(AsyncId, Result);
					return;
				}

				if (Params.TimeFrame.Candles)
				{
					// Adjust this loop to fit your API returned results:

					foreach (var Candle in Response["TimeSeries"])
					{
						// Insert your API returned values here:

						DateTime DateTimeStamp = DateTime.UtcNow;
						double
							Open = 0,
							High = 0,
							Low = 0,
							Close = 0,
							Volume = 0;

						////////////////////////////////////////

						var DataPoint = new DataPoint()
						{
							Date = DateTimeStamp.ToISODate(),
							Time = (uint)DateTimeStamp.TimeOfDay.TotalSeconds,
							Open = Open,
							High = High,
							Low = Low,
							Close = Close,
							Volume = Volume
						};

						Values.AddCandle(DataPoint);
					}
				}
				else // Ticks
				{
					// Adjust this loop to fit your API returned results:

					foreach (var TickValue in Response["TimeSeries"])
					{
						// Insert your API returned values here:

						DateTime DateTimeStamp = DateTime.UtcNow;
						double
							Price = 0,
							Volume = 0;

						////////////////////////////////////////

						var Tick = new Tick()
						{
							Field = PriceField._LAST,
							TimeStamp = DateTimeStamp,
							Price = Price,
							Volume = Volume
						};

						Values.AddTick(Tick);
					}
				}

				// Mark the Time Series as Complete when no more back history will be returned

				Values.Complete = true;

				///////////////////////////////////////////////////////////////////////////////
				
				I.raise_OnRequestTimeSeriesFinished(AsyncId, Values);
			}
		}
	}
}