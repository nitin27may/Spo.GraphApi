using Spo.GraphApi.Models;

namespace Spo.GraphApi;

public interface IGraphApiCient
{
    Task<SiteDetails> GetSiteId(string siteName);

    Task<List<Drive>> GetDrives(string siteId);

    Task<FileResponse> UploadFile(CustomFile customFile);
}