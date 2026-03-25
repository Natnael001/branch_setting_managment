using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class FtpHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FtpHelper> _logger;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _enableSsl;

    public FtpHelper(IConfiguration configuration, ILogger<FtpHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["Ftp:BaseUrl"];
        _username = configuration["Ftp:Username"];
        _password = configuration["Ftp:Password"];
        _enableSsl = configuration.GetValue<bool>("Ftp:EnableSsl", false); // optional setting

        _logger.LogDebug("FtpHelper initialized: BaseUrl={BaseUrl}, Username={Username}, Ssl={Ssl}",
            _baseUrl, _username, _enableSsl);
    }

    private FtpWebRequest CreateRequest(string uri, string method)
    {
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Credentials = new NetworkCredential(_username, _password);
        request.Method = method;
        request.UsePassive = true;
        request.EnableSsl = _enableSsl;

        // Ignore certificate errors (for development only!)
        if (_enableSsl)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        return request;
    }

    public async Task<bool> CreateDirectoryAsync(string path)
    {
        try
        {
            var request = CreateRequest(path, WebRequestMethods.Ftp.MakeDirectory);
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
            _logger.LogError(ex, "Failed to create FTP directory {Path}. Status: {Status}", path, response?.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating FTP directory {Path}", path);
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
            if (!success)
            {
                _logger.LogWarning("Failed to create directory {Path}", currentPath);
                return false;
            }
        }
        return true;
    }


    public async Task<bool> UploadFileAsync(string remotePath, Stream fileStream)
    {
        try
        {

            //upload the file
            var request = CreateRequest(remotePath, WebRequestMethods.Ftp.UploadFile);
            using (var requestStream = request.GetRequestStream())
            {
                await fileStream.CopyToAsync(requestStream);
            }
            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                bool success = response.StatusCode == FtpStatusCode.ClosingData;
                if (!success)
                {
                    _logger.LogError("Upload failed with status {StatusCode}: {StatusDesc}", response.StatusCode, response.StatusDescription);
                }
                return success;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Path}. Exception: {Message}", remotePath, ex.Message);
            if (ex is WebException webEx && webEx.Response is FtpWebResponse ftpResp)
            {
                _logger.LogError("FTP status code: {StatusCode}, status description: {StatusDesc}",
                    ftpResp.StatusCode, ftpResp.StatusDescription);
            }
            return false;
        }
    }
   

    public async Task<bool> DeleteFileAsync(string remotePath)
    {
        try
        {
            var request = CreateRequest(remotePath, WebRequestMethods.Ftp.DeleteFile);
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
            var request = CreateRequest(remotePath, WebRequestMethods.Ftp.DownloadFile);
            var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.GetResponseStream();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from {Path}", remotePath);
            return null;
        }
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            var request = CreateRequest(_baseUrl, WebRequestMethods.Ftp.PrintWorkingDirectory);
            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP connection test failed");
            return false;
        }
    }

    public async Task<bool> HasWritePermissionAsync()
    {
        string testFolder = $"{_baseUrl.TrimEnd('/')}/temp_test_{Guid.NewGuid():N}";
        try
        {
            var createRequest = CreateRequest(testFolder, WebRequestMethods.Ftp.MakeDirectory);
            using (var createResponse = (FtpWebResponse)await createRequest.GetResponseAsync())
            {
            }

            var deleteRequest = CreateRequest(testFolder, WebRequestMethods.Ftp.RemoveDirectory);
            using (var deleteResponse = (FtpWebResponse)await deleteRequest.GetResponseAsync())
            {
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Write permission test failed");
            return false;
        }
    }
}