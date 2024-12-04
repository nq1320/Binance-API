using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using UAPI;
using UAPI.Collections;
using UAPI.Extensions;

namespace Binance
{
	public partial class Binance : UAPI.UAPI
	{
		public abstract class IRequest
		{
			ManualResetEvent Complete = new ManualResetEvent(false);
			public void SetComplete() { Complete.Set(); }
			public bool Wait(int TimeOutMs = 10000) { return Complete.WaitOne(TimeOutMs); }
			public virtual string Url { get { return ""; } }
			public virtual string JsonBody { get { return ""; } }
			public virtual void ProcessResponse(dynamic Response) { }
			public Response Result { get; set; } = true;
		}

		CancellationTokenSource RunProcessors = null;
		RequestPriorityQueue<IRequest> Requests = new RequestPriorityQueue<IRequest>();

		public void Request(IRequest Request, PriorityKind Priority = PriorityKind.Default)
		{
			Requests.Enqueue(Priority, Request);
		}

		void InitializeProcessors(int ProcessorCount = 1, bool PollUpdates = true)
		{
			if (RunProcessors != null)
				RunProcessors.Cancel();

			RunProcessors = new CancellationTokenSource();

			for (int i = 0; i < ProcessorCount; ++i)
				StartProcessor(RequestProcessor, $"RequestProcessor {i}");

			if (PollUpdates)
				StartProcessor(ProcessPolledUpdates, "UpdatesProcessor");

			void StartProcessor(ThreadStart ProcessHandler, string Name)
			{
				var Processor = new Thread(ProcessHandler)
				{
					IsBackground = true,
					Name = Name
				};

				Processor.SetApartmentState(ApartmentState.STA);
				Processor.Start();
			}
		}

		void ShutdownRequestProcessors()
		{
			RunProcessors.Cancel();
		}

		void RequestProcessor()
		{
			var Run = RunProcessors.Token;
			try
			{
				while (!Run.IsCancellationRequested)
				{
					if (!Requests.TryDequeue(out var Request))
					{
						Thread.Sleep(250);
						continue;
					}

					ProcessRequest(Request, Run);
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				Log.Err(e.ToString());
			}
		}

		async void ProcessRequest(IRequest Request, CancellationToken Run)
		{
			var RestRequest = new RestRequest(Request.Url);

			string JsonBody = Request.JsonBody;
			if (!string.IsNullOrEmpty(JsonBody))
				RestRequest.AddJsonBody(JsonBody);

			var Response = await Client.GetAsync<dynamic>(RestRequest, Run);
			
			try
			{
				Request.ProcessResponse(Response);
			}
			catch (Exception e)
			{
				Log.Err(e.ToString());
			}
			finally
			{
				Request.SetComplete();
			}
		}

		void ProcessPolledUpdates()
		{
			var Run = RunProcessors.Token;
			try
			{
				while (!Run.IsCancellationRequested)
				{
					var InIds = Instruments.Keys;
					if (InIds.Count <= 0)
					{
						Thread.Sleep(1000);
						continue;
					}

					DateTime Now = DateTime.UtcNow;
					foreach (var InId in InIds)
					{
						if (Instruments.TryGetValue(InId, out var Instrument) && Instrument.SubscriptionFlags.HasAny(UpdateFlags.Quotes | UpdateFlags.Charts))
						{
							if (Instrument.Info == null)
								continue;

							var SecondsSinceLastRequest = (Now - Instrument.LastUpdatedUtc).TotalSeconds;
							if (!Instrument.UpdateRequested || SecondsSinceLastRequest >= (2 * Constants.InstrumentUpdateFrequencySeconds))
							{
								if (SecondsSinceLastRequest >= Constants.InstrumentUpdateFrequencySeconds)
								{
									Instrument.UpdateRequested = true;
									I.Request(new UpdateRequest() { Instrument = Instrument });
								}
							}
						}
					}

					Thread.Sleep(250);
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				Log.Err(e.ToString());
			}
		}
	}
}
