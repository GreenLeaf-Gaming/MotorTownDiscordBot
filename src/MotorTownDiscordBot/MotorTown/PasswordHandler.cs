
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace MotorTownDiscordBot.MotorTown
{
    internal class PasswordHandler : DelegatingHandler
    {
        private string? password;

        public PasswordHandler(string? password) : this(new HttpClientHandler(), password)
        {
        }

        public PasswordHandler(HttpMessageHandler innerHandler, string? password) : base(innerHandler)
        {
            this.password = password;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (password != null)
            {
                addPasword(request);
            }

            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (password != null)
            {
                addPasword(request);
            }

            return base.SendAsync(request, cancellationToken);
        }
        private void addPasword(HttpRequestMessage request)
        {
            var uriBuilder = new UriBuilder(request.RequestUri!);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query.Add("password", password);

            uriBuilder.Query = toQS(query);

            request.RequestUri = uriBuilder.Uri;
        }

        private string toQS(NameValueCollection query)
        {
            if (query.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            string?[] keys = query.AllKeys;
            for (int i = 0; i < query.Count; i++)
            {
                string? key = keys[i];
                string[]? values = query.GetValues(key);
                if (values != null)
                {
                    foreach (string value in values)
                    {
                        if (!string.IsNullOrEmpty(key))
                        {
                            sb.Append(key).Append('=');
                        }
                        sb.Append(Uri.EscapeDataString(value)).Append('&');
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "";
        }
    }
}