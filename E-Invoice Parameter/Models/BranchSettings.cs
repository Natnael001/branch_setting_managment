using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

public class BranchSettings
{
    public int BranchId { get; set; }  

    public string SourceNumber { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ApiKey { get; set; }

    [JsonIgnore]
    public IFormFile DigitalCertificate { get; set; }

    [JsonIgnore]
    public IFormFile PrivateKey { get; set; }

    public string DigitalCertificatePath { get; set; }
    public string PrivateKeyPath { get; set; }
}