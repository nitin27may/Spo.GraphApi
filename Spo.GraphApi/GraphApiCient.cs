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

    public GraphApiCient(HttpClient httpClient, GraphApiOptions graphApiOptions, ILogger<GraphApiCient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _graphApiOptions = graphApiOptions;
    }

    public async Task<SiteDetails> GetSiteId(string siteName)
    {
        return await GetAsync<SiteDetails>($"/sites/{_graphApiOptions.BaseSpoSiteUri}:/sites/{siteName}");
    }

    public async Task<List<Drive>> GetDrives(string siteId)
    {
        return (await GetAsync<DriveDetails>($"/sites/{siteId}/drives?$select=id,name,description,webUrl")).value;
    }

    public async Task<FileResponse> UploadFile(CustomFile customFile)
    {
        await using var msStream = new MemoryStream();
        await customFile.File.CopyToAsync(msStream);
        return await UploadAsync<FileResponse>($"drives/{customFile.DriveId}/items/root:/{customFile.Name}:/content?@microsoft.graph.conflictBehavior=rename", msStream.ToArray()); ;
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage responseMessage = await _httpClient
            .GetAsync($"{_graphApiOptions.BaseGraphUri}/{endpoint}", cancellationToken);
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
            .PostAsync($"{_graphApiOptions.BaseGraphUri}/{endpoint}", stringContent, cancellationToken);
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

    private async Task<TOut> PutAsync<TIn, TOut>(string endpoint, TIn data, CancellationToken cancellationToken = default)
    {
        var stringContent = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        HttpResponseMessage responseMessage = await _httpClient
            .PutAsync($"{_graphApiOptions.BaseGraphUri}/{endpoint}", stringContent, cancellationToken);
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
            .DeleteAsync($"{_graphApiOptions.BaseGraphUri}/{endpoint}", cancellationToken);
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

    private async Task<TOut> UploadAsync<TOut>(string endpoint, byte[] data, CancellationToken cancellationToken = default)
    {
        var multiPartData = new ByteArrayContent(data);
        _httpClient.DefaultRequestHeaders.Add("ContentType", "application/octet-stream");
        HttpResponseMessage responseMessage = await _httpClient
            .PutAsync($"{_graphApiOptions.BaseGraphUri}/{endpoint}", multiPartData, cancellationToken);
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
}