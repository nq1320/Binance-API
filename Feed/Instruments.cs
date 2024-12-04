using System;
using System.Collections.Concurrent;
using UAPI;
using UAPI.Collections;

namespace Binance
{
	public partial class Binance : UAPI.UAPI
	{
		ConcurrentDictionary<InstrumentIdentifier, Instrument> Instruments = new ConcurrentDictionary<InstrumentIdentifier, Instrument>();

		/// <returns>true when the instrument already exists. Even if a new one was created it still returns false</returns>
		public Response GetOrCreateInstrument(InstrumentIdentifier InId, out Instrument Instrument, bool CreateIfNotExists = true)
		{
			if (Instruments.TryGetValue(InId, out Instrument))
				return true;

			if (!CreateIfNotExists)
				return "No such instrument exists";

			Instrument = new Instrument() { InId = InId };
			Instruments[InId] = Instrument;

			return true;
		}

		public class Instrument
		{
			public InstrumentIdentifier InId = null;
			public InstrumentInfo Info = null;
			public UpdateFlags SubscriptionFlags = UpdateFlags.None;
			public DateTime LastUpdatedUtc = default(DateTime);
			public bool UpdateRequested = false;
		}
	}
}
