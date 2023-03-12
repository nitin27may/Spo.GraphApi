using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Spo.GraphApi.Models;
using System.Text;
using System.Text.Json;

namespace Spo.GraphApi;

internal class GraphApiCient : IGraphApiCient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphApiCient> _logger;
    private readonly GraphApiOptions _graphApiOptions;
    private readonly IDistributedCache _distributedCache;

    public GraphApiCient(HttpClient httpClient, GraphApiOptions graphApiOptions, ILogger<GraphApiCient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _graphApiOptions = graphApiOptions;
        _distributedCache = distributedCache;
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage responseMessage = await _httpClient
            .GetAsync($"{_graphApiOptions.BaseUri}/{endpoint}", cancellationToken);
        var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("Error calling Endpoint: GET {@Endpoint}. Response: {@Response}.",
                responseMessage.RequestMessage.RequestUri, content);

            throw new GraphApiException(responseMessage.StatusCode, content);
        }

        return JsonSerializer.Deserialize<T>(content);
    }

    private async Task<TOut> PostAsync<TIn, TOut>(string endpoint, TIn data, CancellationToken cancellationToken = default)
    {
        var stringContent = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        HttpResponseMessage responseMessage = await _httpClient
            .PostAsync($"{_graphApiOptions.BaseUri}/{endpoint}", stringContent, cancellationToken);
        var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var requestMessage = await responseMessage.RequestMessage.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error calling Endpoint: POST {@Endpoint}. Request: {@Request}, Response: {@Response}.",
                responseMessage.RequestMessage.RequestUri, requestMessage, content);

            throw new GraphApiException(responseMessage.StatusCode, content);
        }

        return JsonSerializer.Deserialize<TOut>(content);
    }

    private async Task<T> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage responseMessage = await _httpClient
            .DeleteAsync($"{_graphApiOptions.BaseUri}/{endpoint}", cancellationToken);
        var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var requestMessage = await responseMessage.RequestMessage.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error calling Endpoint: POST {@Endpoint}. Request: {@Request}, Response: {@Response}.",
                responseMessage.RequestMessage.RequestUri, requestMessage, content);

            throw new GraphApiException(responseMessage.StatusCode, content);
        }

        return JsonSerializer.Deserialize<T>(content);
    }
}
