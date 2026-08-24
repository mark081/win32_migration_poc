using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ToolLending.AppServer
{
    internal sealed class ApiKeyHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage r,
            CancellationToken t
        )
        {
            if (
                !r.Headers.TryGetValues("X-Api-Key", out var v)
                || !Equal(v.FirstOrDefault(), ConfigurationManager.AppSettings["ApiKey"])
            )
                return Task.FromResult(
                    r.CreateErrorResponse(
                        HttpStatusCode.Unauthorized,
                        "A valid X-Api-Key header is required."
                    )
                );
            return base.SendAsync(r, t);
        }

        static bool Equal(string a, string b)
        {
            if (a == null || b == null)
                return false;
            var d = a.Length ^ b.Length;
            for (var i = 0; i < a.Length && i < b.Length; i++)
                d |= a[i] ^ b[i];
            return d == 0;
        }
    }
}
