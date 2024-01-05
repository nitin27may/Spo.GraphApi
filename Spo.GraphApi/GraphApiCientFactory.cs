using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spo.GraphApi.Handler;
using Spo.GraphApi.Models;

namespace Spo.GraphApi;

public class GraphApiCientFactory : IGraphApiCientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<GraphApiOptions> _options;
    private readonly IDistributedCache _distributedCache;

    public GraphApiCientFactory(IOptions<GraphApiOptions> options, ILoggerFactory loggerFactory, IDistributedCache distributedCache)
    {
        _loggerFactory = loggerFactory;
        _options = options;
        _distributedCache = distributedCache;
    }

    public IGraphApiCient Create()
    {
        GraphApiAuthenticationHandler authHandler = new GraphApiAuthenticationHandler(_distributedCache, _loggerFactory.CreateLogger<GraphApiAuthenticationHandler>(), _options);
        HttpClient httpClient = new HttpClient(authHandler);

        return new GraphApiCient(httpClient, _options.Value, _loggerFactory.CreateLogger<GraphApiCient>());
    }
}