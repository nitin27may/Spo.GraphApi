using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Spo.GraphApi.Models;
using System.Text;
using System.Text.Json;
using System.Threading;

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

    public async Task<SiteDetails> GetSiteId(string siteName, CancellationToken cancellationToken = default)
    {
        var siteIdByteArray = await _distributedCache.GetAsync(siteName, cancellationToken);
        if (siteIdByteArray?.Length > 0)
        {
            return JsonSerializer.Deserialize<SiteDetails>(Encoding.UTF8.GetString(siteIdByteArray));
        }

        var siteDetails = await GetAsync<SiteDetails>($"/sites/{_graphApiOptions.BaseSpoSiteUri}:/sites/{siteName}");

        DistributedCacheEntryOptions cacheEntryOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(new TimeSpan(1, 0, 0, 0));
        _distributedCache.Set(siteName, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(siteDetails)), cacheEntryOptions);

        return siteDetails;
    }

    public async Task<List<Drive>> GetDrives(string siteName)
    {
        var siteDetails = await GetSiteId(siteName);
        return (await GetAsync<DriveDetails>($"/sites/{siteDetails.id}/drives?$select=id,name,description,webUrl")).value;
    }

    private async Task<Drive?> GetDrive(string siteName, string DriveName, CancellationToken cancellationToken = default)
    {
        var driveDetailsByteArray = await _distributedCache.GetAsync(siteName + DriveName, cancellationToken);
        if (driveDetailsByteArray?.Length > 0)
        {
            return JsonSerializer.Deserialize<Drive>(Encoding.UTF8.GetString(driveDetailsByteArray));
        }

        var siteDetails = await GetSiteId(siteName);
        var drives = (await GetAsync<DriveDetails>($"/sites/{siteDetails.id}/drives?$select=id,name,description,webUrl")).value;
        var driveDetail = drives?.FirstOrDefault(x => x.name == DriveName);

        DistributedCacheEntryOptions cacheEntryOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(new TimeSpan(1, 0, 0, 0));
        _distributedCache.Set(siteName + DriveName, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(driveDetail)), cacheEntryOptions);

        return driveDetail;
    }

    public async Task<List<Drive>> GetDriveItems(string siteId, string driveId)
    {
        return (await GetAsync<DriveDetails>($"/sites/{siteId}/drives/{driveId}/items/root/children?$select=id,name,description,webUrl")).value;
    }

    public async Task<FileResponse> UploadFile(string siteName, string driveName, CustomFile customFile)
    {
        var driveDetals = await GetDrive(siteName, driveName);

        await using var msStream = new MemoryStream();
        await customFile.File.CopyToAsync(msStream);
        return await UploadAsync<FileResponse>($"drives/{driveDetals.id}/items/root:/{customFile.Name}:/content?@microsoft.graph.conflictBehavior=rename", msStream.ToArray()); ;
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