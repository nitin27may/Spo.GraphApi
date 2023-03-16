using Microsoft.AspNetCore.Mvc;
using Spo.GraphApi;
using Spo.GraphApi.Models;

namespace Spo.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GraphApiController : ControllerBase
{
    private readonly ILogger<GraphApiController> _logger;
    private readonly IGraphApiCientFactory _graphApiCientFactory;

    public GraphApiController(ILogger<GraphApiController> logger, IGraphApiCientFactory graphApiCientFactory)
    {
        _logger = logger;
        _graphApiCientFactory = graphApiCientFactory;
    }

    [HttpGet]
    [Route("site/{siteName}")]
    public async Task<SiteDetails> GetSiteId(string siteName)
    {
        var _graphApiCient = _graphApiCientFactory.Create();
        return await _graphApiCient.GetSiteId(siteName);
    }

    [HttpGet]
    [Route("site/{siteId}/drives")]
    public async Task<List<Drive>> GetDrives(string siteId)
    {
        var _graphApiCient = _graphApiCientFactory.Create();
        return await _graphApiCient.GetDrives(siteId);
    }

    [HttpPost, DisableRequestSizeLimit]
    [Route("site/{driveId}/Upload")]
    public async Task<FileResponse> Upload([FromForm] CustomFile customFile)
    {
        var _graphApiCient = _graphApiCientFactory.Create();
        return await _graphApiCient.UploadFile(customFile);
    }
}