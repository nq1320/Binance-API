using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using UAPI.Collections;
using UAPI;

namespace Binance
{
    public partial class Binance : UAPI.UAPI
    {
        enum ConnectKind { None, Initialize, Authenticate, Authenticated, Connected, Disconnected, Shutdown }

        RestClient Client = null;
        StateActions<ConnectKind> ConnectState = null;

        // Binance API credentials
        private const string ApiKey = "YOUR_BINANCE_API_KEY";
        private const string ApiSecret = "YOUR_BINANCE_SECRET_KEY";

        public void InitializeConnection()
        {
            ConnectState = new StateActions<ConnectKind>(ConnectKind.None)
            {
                //ChangeStateBeforeEnterState = true
            };

            ConnectState.MapState(ConnectKind.Initialize);
            ConnectState.MapState(ConnectKind.Authenticated);
            ConnectState.MapState(ConnectKind.Authenticate, Authenticate_State);
            ConnectState.MapState(ConnectKind.Connected, Connected_State);
            ConnectState.MapState(ConnectKind.Disconnected);
            ConnectState.MapState(ConnectKind.Shutdown, null, true);

            ConnectState.EnterState += ConnectState_EnterState;
            ConnectState.FrequencyMs = 1000;
            ConnectState.Start();

            ConnectState.SetState(ConnectKind.Initialize);
        }

        public void ShutdownConnection()
        {
            ConnectState.SetState(ConnectKind.Shutdown);
        }

        private void ConnectState_EnterState(
            ConnectKind PreviousState, ConnectKind CurrentState)
        {
            switch (CurrentState)
            {
                case ConnectKind.Initialize:
                    Client = new RestClient(Constants.BaseUrl);
                    ConnectState.SetState(ConnectKind.Authenticate);
                    break;
                case ConnectKind.Authenticate:
                    // Authenticated state will be set in Authenticate_State method after signing request
                    break;
                case ConnectKind.Authenticated:

                    break;
                case ConnectKind.Connected:

                    break;
                case ConnectKind.Disconnected:

                    break;
                case ConnectKind.Shutdown:

                    break;
            }

            raise_OnUpdateRequestStatus(ConnectState.State == ConnectKind.Connected, ConnectState.State.ToString());
        }

        void Authenticate_State()
        {
            // Add API Key to client default headers for all requests
            Client.AddDefaultHeader("X-MBX-APIKEY", ApiKey);

            // Set to Connected if API key is added successfully
            ConnectState.SetState(ConnectKind.Connected);
        }

        void Connected_State()
        {
            // Code for handling connected state, such as making API requests
        }

        // Method to sign requests with HMAC SHA256
        private string CreateSignature(string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Example method to send an authenticated request to Binance API
        public async Task<IRestResponse> SendSignedRequest(string endpoint, Dictionary<string, string> parameters)
        {
            var request = new RestRequest(endpoint, Method.Get);

            // Add query parameters
            foreach (var param in parameters)
                request.AddParameter(param.Key, param.Value);

            // Generate timestamp and add to parameters
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            request.AddParameter("timestamp", timestamp);

            // Concatenate parameters for signature
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}")) + $"&timestamp={timestamp}";
            var signature = CreateSignature(queryString);
            request.AddParameter("signature", signature);

            return await Client.ExecuteAsync(request);
        }
    }
}
