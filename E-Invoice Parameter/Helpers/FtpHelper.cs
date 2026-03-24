using System.Net;

public class FtpHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FtpHelper> _logger;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    public FtpHelper(IConfiguration configuration, ILogger<FtpHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["Ftp:BaseUrl"];
        _username = configuration["Ftp:Username"];
        _password = configuration["Ftp:Password"];
    }
    public async Task<bool> CreateDirectoryAsync(string path)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create(path);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(_username, _password);
            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                return response.StatusCode == FtpStatusCode.PathnameCreated;
            }
        }
        catch (WebException ex)
        {
            var response = (FtpWebResponse)ex.Response;
            if (response != null && response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                // Directory already exists
                return true;
            }
            _logger.LogError(ex, "Failed to create FTP directory {Path}", path);
            return false;
        }
    }

    public async Task<bool> EnsureDirectoryExistsAsync(string relativePath)
    {
        string currentPath = _baseUrl.TrimEnd('/');
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            currentPath += "/" + part;
            bool success = await CreateDirectoryAsync(currentPath);
            if (!success) return false;
        }
        return true;
    }

    public async Task<bool> UploadFileAsync(string remotePath, Stream fileStream)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create(remotePath);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_username, _password);
            using (var requestStream = request.GetRequestStream())
            {
                await fileStream.CopyToAsync(requestStream);
            }
            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                return response.StatusCode == FtpStatusCode.ClosingData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to {Path}", remotePath);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create(remotePath);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(_username, _password);
            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                return response.StatusCode == FtpStatusCode.FileActionOK;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {Path}", remotePath);
            return false;
        }
    }

    public async Task<Stream?> DownloadFileAsync(string remotePath)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create(remotePath);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(_username, _password);
            var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.GetResponseStream();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from {Path}", remotePath);
            return null;
        }
    }
}