using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Prodright
{
    public sealed class MsalAccessTokenProvider : IAccessTokenProvider
    {
        private readonly IPublicClientApplication _pca;
        private readonly string[] _scopes;


        public MsalAccessTokenProvider(IOptions<MsalOptions> opts)
        {
            var o = opts.Value;

            if (string.IsNullOrWhiteSpace(o.ClientId) || string.IsNullOrWhiteSpace(o.Authority))
                throw new InvalidOperationException("MSAL settings missing: ClientId/Authority must be configured.");

            _scopes = (o.Scopes?.Length > 0) ? o.Scopes : throw new InvalidOperationException("MSAL settings missing: Scopes.");

            _pca = PublicClientApplicationBuilder
                .Create(o.ClientId)
                .WithAuthority(o.Authority)
                .WithDefaultRedirectUri()
                .Build();
        }


        public async Task<string> GetTokenAsync(CancellationToken ct = default)
        {
            // Try silent first (SSO / cached token)
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            var first = accounts.FirstOrDefault();

            try
            {
                var silent = await _pca
                    .AcquireTokenSilent(_scopes, first)
                    .ExecuteAsync(ct)
                    .ConfigureAwait(false);

                return silent.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Interactive login fallback
                var interactive = await _pca
                    .AcquireTokenInteractive(_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(ct)
                    .ConfigureAwait(false);

                return interactive.AccessToken;
            }
        }
    }
}