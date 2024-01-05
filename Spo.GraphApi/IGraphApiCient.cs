using Spo.GraphApi.Models;

namespace Spo.GraphApi;

public interface IGraphApiCient
{
    Task<SiteDetails> GetSiteId(string siteName, CancellationToken cancellationToken = default);

    Task<List<Drive>> GetDrives(string siteName);

    Task<FileResponse> UploadFile(string siteName, string driveName, CustomFile customFile);
}