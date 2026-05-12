using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Prodright
{
    public sealed class SapBearerTokenHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _tokens;

        public SapBearerTokenHandler(IAccessTokenProvider tokens)
        {
            _tokens = tokens;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _tokens.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}