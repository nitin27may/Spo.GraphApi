using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Spo.GraphApi.Handler
{
    internal class GraphApiAuthenticationHandler : DelegatingHandler
    {

        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<GraphApiAuthenticationHandler> _logger;

        public GraphApiAuthenticationHandler(
            IDistributedCache distributedCache,
            ILogger<GraphApiAuthenticationHandler> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string accessToken = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var tenantId = "tenant_name.onmicrosoft.com";
            var clientId = "aad_app_id";
            var clientSecret = "client_secret";
            var clientSecretCredential = new ClientSecretCredential(
                            tenantId, clientId, clientSecret);
            var tokenRequestContext = new TokenRequestContext(scopes);
            var tokenDetails = await clientSecretCredential.GetTokenAsync(tokenRequestContext, cancellationToken);
            return tokenDetails.Token;
        }
    }
}
