using Microsoft.AspNetCore.Mvc;
using Spo.GraphApi;
using Spo.GraphApi.Models;

namespace Spo.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GraphApiController : ControllerBase
{
    private readonly ILogger<GraphApiController> _logger;
    private readonly IGraphApiCient _graphApiCient;

    public GraphApiController(ILogger<GraphApiController> logger, IGraphApiCientFactory graphApiCientFactory)
    {
        _logger = logger;
        _graphApiCient = graphApiCientFactory.Create();
    }

    [HttpGet]
    [Route("site/{siteName}")]
    public async Task<SiteDetails> GetSiteId(string siteName)
    {
        return await _graphApiCient.GetSiteId(siteName);
    }

    [HttpGet]
    [Route("site/{siteName}/drives")]
    public async Task<List<Drive>> GetDrives(string siteName)
    {
        return await _graphApiCient.GetDrives(siteName);
    }

    [HttpPost, DisableRequestSizeLimit]
    [Route("site/{driveId}/Upload")]
    public async Task<FileResponse> Upload([FromForm] CustomFile customFile)
    {
        return await _graphApiCient.UploadFile(customFile);
    }
}