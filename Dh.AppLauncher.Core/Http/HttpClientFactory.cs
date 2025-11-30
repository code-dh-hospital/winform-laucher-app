using System;
using System.Net;
using System.Net.Http;

namespace Dh.AppLauncher.Http
{
    public static class HttpClientFactory
    {
        private static readonly Lazy<HttpClient> _clientLazy = new Lazy<HttpClient>(Create);
        public static HttpClient Client { get { return _clientLazy.Value; } }

        private static HttpClient Create()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(60);
            return client;
        }
    }
}
