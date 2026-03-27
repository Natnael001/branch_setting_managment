using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SystemController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SystemController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly FtpHelper _ftpHelper;
    private readonly IConfiguration _configuration;

    public SystemController(AppDbContext context, ILogger<SystemController> logger, IWebHostEnvironment webHostEnvironment, FtpHelper ftpHelper, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _ftpHelper = ftpHelper;
        _configuration = configuration;
    }


    private bool IsCurrentUserAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return false;

        try
        {
            return _context.UserRoleMappers
                .Where(urm => urm.UserId == userId)
                .Join(_context.ConsigneeUnits,
                      urm => urm.RoleId,
                      role => role.id,
                      (urm, role) => new { urm.ExpiryDate, role.Name })
                .Any(r => r.Name == "Administrator" && (r.ExpiryDate == null || r.ExpiryDate > DateTime.Now));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin check failed for user {UserId}", userId);
            return false;
        }
    }

    private bool IsCurrentUserSystemAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return false;

        try
        {
            return _context.UserRoleMappers
                .Where(urm => urm.UserId == userId)
                .Join(_context.ConsigneeUnits,
                      urm => urm.RoleId,
                      role => role.id,
                      (urm, role) => new { urm.ExpiryDate, role.Name })
                .Any(r => r.Name == "System Administrator" && (r.ExpiryDate == null || r.ExpiryDate > DateTime.Now));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin check failed for user {UserId}", userId);
            return false;
        }
    }


    [HttpGet]
    public IActionResult DeviceManagement()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Verification");

        return View();
    }

    private async Task PrepareViewBag()
    {
        ViewBag.Roles = await _context.ConsigneeUnits
            .Where(r => r.Type == 1725 && r.IsActive)
            .Select(r => new { r.id, r.Name })
            .ToListAsync();
    }


    private string GenerateEmployeeCode()
    {
        string datePart = DateTime.Now.ToString("yyyyMMdd");
        string prefix = "EMP" + datePart;

        var lastCode = _context.Consignees
            .Where(c => c.Code.StartsWith(prefix))
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(lastCode))
        {
            return prefix + "001";
        }

        string lastSeq = lastCode.Substring(lastCode.Length - 3);
        if (int.TryParse(lastSeq, out int seq))
        {
            seq++;
            return prefix + seq.ToString("D3");
        }

        return prefix + "001";
    }


    [Route("")]
    [Route("System/Verification")]
    [AllowAnonymous]
    public IActionResult Verification()
    {
        if (User.Identity.IsAuthenticated && HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Parameters");
        }
        return View();
    }


    public IActionResult Parameters()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        _logger.LogInformation("Parameters page accessed. UserId from session: {UserId}", userId);

        //if (userId == null || userId == 0)
        //{
        //    _logger.LogWarning("No valid UserId in session, redirecting to Login");
        //    return RedirectToAction("Login");
        //}

        var companyId = HttpContext.Session.GetInt32("CompanyId");
        var tin = HttpContext.Session.GetString("VerifiedTin");

        _logger.LogInformation("CompanyId from session: {CompanyId}, TIN: {Tin}", companyId, tin);

        if (companyId == null && !string.IsNullOrEmpty(tin))
        {
            var company = _context.Consignees
                .Where(c => c.Tin != null && c.GslType == 1)
                .Select(c => new { c.Id, c.Tin })
                .FirstOrDefault();

            if (company != null)
            {
                companyId = company.Id;
                HttpContext.Session.SetInt32("CompanyId", company.Id);
                _logger.LogInformation("Recovered CompanyId from TIN: {CompanyId}", companyId);
            }
        }

       
        bool isAdmin = IsCurrentUserAdmin();
        bool isSystemAdmin = IsCurrentUserSystemAdmin();

        ViewBag.Tin = tin;
        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsSystemAdmin = isSystemAdmin;


        return View();
    }



    public IActionResult ParametersWithTin(string tin)
    {
        ViewBag.Tin = tin;
        return View("Parameters");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var tin = TempData["VerifiedTin"]?.ToString();
        if (string.IsNullOrEmpty(tin))
        {
            tin = HttpContext.Session.GetString("VerifiedTin");
        }
        else
        {
            HttpContext.Session.SetString("VerifiedTin", tin);
        }

        if (string.IsNullOrEmpty(tin))
        {
            _logger.LogWarning("No verified TIN found, redirecting to Verification");
            return RedirectToAction("Verification");
        }

        TempData.Keep("VerifiedTin");
        ViewBag.Tin = tin;

        _logger.LogInformation("Login page accessed with TIN: {Tin}", tin);
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", model?.Username);

            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "Username and password are required." });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == model.Username.ToLower() && u.IsActive);

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.UserName, user.PasswordHash, user.Salt))
            {
                return Json(new { success = false, message = "Invalid username or password." });
            }

            var allowedRoles = new[] { "System Administrator", "Administrator" };

            var hasAllowedRole = await _context.UserRoleMappers
                .Where(urm => urm.UserId == user.Id)
                .Join(_context.ConsigneeUnits,
                      urm => urm.RoleId,
                      role => role.id,
                      (urm, role) => new { urm.ExpiryDate, role.Name })
                .AnyAsync(r => allowedRoles.Contains(r.Name) && (r.ExpiryDate == null || r.ExpiryDate > DateTime.Now));

            if (!hasAllowedRole)
            {
                _logger.LogWarning("Login denied for user {Username}: insufficient role", model.Username);
                return Json(new { success = false, message = "Access denied. You do not have permission to log in." });
            }

            user.LastLoginAt = DateTime.Now;
            user.FirstLoginAt ??= DateTime.Now;
            user.LoggedInStatus = 1390;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim("UserId", user.Id.ToString()),
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.UserName ?? "");

            var verifiedCompanyId = HttpContext.Session.GetInt32("VerifiedCompanyId");
            if (verifiedCompanyId.HasValue)
            {
                HttpContext.Session.SetInt32("CompanyId", verifiedCompanyId.Value);
            }
            else
            {
                var tin = HttpContext.Session.GetString("VerifiedTin");
                if (!string.IsNullOrEmpty(tin))
                {
                    var companyEntity = await _context.Consignees
                        .FirstOrDefaultAsync(c => c.Tin == tin && c.GslType == 1);

                    if (companyEntity != null)
                    {
                        HttpContext.Session.SetInt32("CompanyId", companyEntity.Id);
                    }
                }
            }

            await HttpContext.Session.CommitAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Parameters") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return Json(new { success = false, message = $"CRASH: {ex.Message}" });
        }
    }


    [HttpGet]
    public IActionResult GetBranchSettingsPartial(int branchId)
    {
        _logger.LogInformation("GetBranchSettingsPartial called for branchId: {BranchId}", branchId);
        const int pointer = 992;
        const string reference = "Company";

        string GetValue(string attribute)
        {
            return _context.Configurations
                .Where(c => c.Pointer == pointer &&
                            c.Reference == reference &&
                            c.ConsigneeUnitId == branchId &&
                            c.Attribute == attribute)
                .Select(c => c.CurrentValue)
                .FirstOrDefault() ?? string.Empty;
        }

        var settings = new BranchSettings
        {
            BranchId = branchId,
            SourceNumber = GetValue("Source Number"),
            ClientId = GetValue("Client ID"),
            ClientSecret = GetValue("Client Secret"),
            ApiKey = GetValue("API Key"),
            DigitalCertificatePath = GetValue("Digital Certificate Path"),
            PrivateKeyPath = GetValue("Private Key Path")
        };

        return PartialView("~/Views/System/_BranchSettings.cshtml", settings);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyTin(string tin)
    {
        try
        {
            _logger.LogInformation("Verifying TIN: {Tin}", tin);

            if (string.IsNullOrWhiteSpace(tin))
            {
                return Json(new
                {
                    success = false,
                    verified = false,
                    message = "TIN cannot be empty"
                });
            }

            var consignees = _context.Consignees
                .Where(c => c.Tin != null && c.Tin == tin && c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.SecondName,
                    c.Tin,
                    c.GslType
                })
                .ToList();

            if (!consignees.Any())
            {
                return Json(new
                {
                    success = false,
                    verified = false,
                    message = "Invalid TIN or consignee not found"
                });
            }

            var company = consignees.FirstOrDefault(c => c.GslType == 1);

            if (company == null)
            {
                return Json(new
                {
                    success = false,
                    verified = false,
                    message = "TIN is not eligible for verification"
                });
            }

            var hasBranches = _context.ConsigneeUnits
                .Any(u => u.ConsigneeId == company.Id && u.IsActive && u.Type == 1719);

            string consigneeName = $"{company.FirstName ?? ""} {company.SecondName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(consigneeName))
                consigneeName = "Company";

            TempData["VerifiedTin"] = tin;
            HttpContext.Session.SetString("CompanyName", consigneeName);
            HttpContext.Session.SetString("VerifiedTin", tin);

            HttpContext.Session.SetInt32("VerifiedCompanyId", company.Id);

            _logger.LogInformation("TIN verified successfully: {Tin}, Company ID: {CompanyId}", tin, company.Id);

            return Json(new
            {
                success = true,
                verified = true,
                hasBranches = hasBranches,
                consigneeId = company.Id,
                consigneeName = consigneeName,
                tin = company.Tin,
                message = hasBranches
                    ? "TIN verified successfully"
                    : "TIN verified but no branches found",
                redirectUrl = Url.Action("Login")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TIN: {Tin}", tin);
            return Json(new
            {
                success = false,
                verified = false,
                message = "An error occurred during verification"
            });
        }
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetBranches(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
        {
            return Json(new { success = false, message = "TIN cannot be empty" });
        }

        var consignee = _context.Consignees
            .Where(c => c.Tin != null && c.Tin == tin && c.IsActive)
            .Select(c => new { c.Id })
            .FirstOrDefault();

        if (consignee == null)
        {
            return Json(new { success = false, message = "Consignee not found" });
        }

        var branches = _context.ConsigneeUnits
            .Where(u => u.ConsigneeId == consignee.Id && u.IsActive && u.Type == 1719)
            .Select(u => new
            {
                id = u.id,
                name = u.Name,
                hasSettings = _context.Configurations.Any(c =>
                    c.Pointer == 992 &&
                    c.Reference == "Company" &&
                    c.ConsigneeUnitId == u.id)
            })
            .ToList();

        return Json(new
        {
            success = true,
            data = branches,
            message = branches.Any() ? "Branches found" : "No branches found"
        });
    }

    [HttpGet]
    public IActionResult GetBranchSettingsData(int branchId)
    {
        try
        {
            const int pointer = 992;
            const string reference = "Company";

            string GetValue(string attribute)
            {
                return _context.Configurations
                    .Where(c => c.Pointer == pointer &&
                                c.Reference == reference &&
                                c.ConsigneeUnitId == branchId &&
                                c.Attribute == attribute)
                    .Select(c => c.CurrentValue)
                    .FirstOrDefault() ?? string.Empty;
            }

            var settings = new BranchSettings
            {
                BranchId = branchId,
                SourceNumber = GetValue("Source Number"),
                ClientId = GetValue("Client ID"),
                ClientSecret = GetValue("Client Secret"),
                ApiKey = GetValue("API Key"),
                DigitalCertificatePath = GetValue("Digital Certificate Path"),
                PrivateKeyPath = GetValue("Private Key Path")
            };

            return Json(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBranchSettings([FromForm] BranchSettings model)
    {
        try
        {
            if (model == null || model.BranchId <= 0)
                return Json(new { success = false, message = "Invalid branch data." });

            const int pointer = 992;
            const string reference = "Company";

            var allowedCertificateExtensions = new[] { ".cer", ".crt", ".pem" };
            var allowedKeyExtensions = new[] { ".pem", ".key" };

            var ftpBaseUrl = _configuration["Ftp:BaseUrl"]?.TrimEnd('/');
            var ftpUsername = _configuration["Ftp:Username"];
            var ftpPassword = _configuration["Ftp:Password"];

            var tin = HttpContext.Session.GetString("VerifiedTin");
            if (string.IsNullOrEmpty(tin))
                return Json(new { success = false, message = "Company TIN not found in session." });


            //debug
            string testPath = $"{ftpBaseUrl}/test_{Guid.NewGuid()}.txt";
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("test");
            using (var ms = new MemoryStream(testData))
            {
                bool uploaded = await _ftpHelper.UploadFileAsync(testPath, ms);
                if (uploaded) await _ftpHelper.DeleteFileAsync(testPath);
            }



            string relativePath = $"{tin}/GslProfile/{model.BranchId}/DigitalCertificate";
            string fullFtpBase = $"{ftpBaseUrl}/{relativePath}";

            bool dirCreated = await _ftpHelper.EnsureDirectoryExistsAsync(relativePath);
            if (!dirCreated)
                _logger.LogWarning("Could not create FTP directory: {Path}", fullFtpBase);

            string digitalCertPath = null;
            if (model.DigitalCertificate != null && model.DigitalCertificate.Length > 0)
            {
                string fileExtension = Path.GetExtension(model.DigitalCertificate.FileName).ToLowerInvariant();
                if (!allowedCertificateExtensions.Contains(fileExtension))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Invalid certificate file type. Allowed types: {string.Join(", ", allowedCertificateExtensions)}"
                    });
                }

                if (model.DigitalCertificate.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Certificate file size exceeds 10MB limit." });
                }

                string fileName = $"{Guid.NewGuid()}_{model.BranchId}_cert{fileExtension}";
                string remoteFilePath = $"{fullFtpBase}/{fileName}";

                using (var fileStream = model.DigitalCertificate.OpenReadStream())
                {
                    bool uploaded = await _ftpHelper.UploadFileAsync(remoteFilePath, fileStream);
                    if (!uploaded)
                        return Json(new { success = false, message = "Failed to upload certificate." });
                }

                digitalCertPath = $"{relativePath}/{fileName}";
                _logger.LogInformation("Digital Certificate uploaded: {Path}", digitalCertPath);
            }

            string privateKeyPath = null;
            if (model.PrivateKey != null && model.PrivateKey.Length > 0)
            {
                string fileExtension = Path.GetExtension(model.PrivateKey.FileName).ToLowerInvariant();
                if (!allowedKeyExtensions.Contains(fileExtension))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Invalid private key file type. Allowed types: {string.Join(", ", allowedKeyExtensions)}"
                    });
                }

                if (model.PrivateKey.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Private key file size exceeds 10MB limit." });
                }

                string fileName = $"{Guid.NewGuid()}_{model.BranchId}_key{fileExtension}";
                string remoteFilePath = $"{fullFtpBase}/{fileName}";

                using (var fileStream = model.PrivateKey.OpenReadStream())
                {
                    bool uploaded = await _ftpHelper.UploadFileAsync(remoteFilePath, fileStream);
                    if (!uploaded)
                        return Json(new { success = false, message = "Failed to upload private key." });
                }

                privateKeyPath = $"{relativePath}/{fileName}";
                _logger.LogInformation("Private Key uploaded: {Path}", privateKeyPath);
            }

            var textAttributes = new Dictionary<string, string>
            {
                { "Source Number", model.SourceNumber },
                { "Client ID", model.ClientId },
                { "Client Secret", model.ClientSecret },
                { "API Key", model.ApiKey }
            };

            foreach (var attr in textAttributes)
            {
                var safeValue = attr.Value ?? "";

                var existing = _context.Configurations
                    .FirstOrDefault(c => c.Pointer == pointer &&
                                         c.Reference == reference &&
                                         c.ConsigneeUnitId == model.BranchId &&
                                         c.Attribute == attr.Key);

                if (existing != null)
                {
                    existing.PreviousValue = existing.CurrentValue;
                    existing.CurrentValue = safeValue;
                }
                else
                {
                    var config = new Configuration
                    {
                        Pointer = pointer,
                        Reference = reference ?? "Company",
                        ConsigneeUnitId = model.BranchId,
                        Attribute = attr.Key ?? "Unknown",
                        CurrentValue = safeValue,
                        PreviousValue = safeValue,
                        Remark = null
                    };
                    _context.Configurations.Add(config);
                }
            }

            if (!string.IsNullOrEmpty(digitalCertPath))
            {
                var existingCert = _context.Configurations
                    .FirstOrDefault(c => c.Pointer == pointer &&
                                         c.Reference == reference &&
                                         c.ConsigneeUnitId == model.BranchId &&
                                         c.Attribute == "Digital Certificate Path");

                if (existingCert != null)
                {
                    // Delete old file from FTP
                    if (!string.IsNullOrEmpty(existingCert.CurrentValue))
                    {
                        string oldRemotePath = $"{ftpBaseUrl}/{existingCert.CurrentValue}";
                        await _ftpHelper.DeleteFileAsync(oldRemotePath);
                    }

                    existingCert.PreviousValue = existingCert.CurrentValue;
                    existingCert.CurrentValue = digitalCertPath;
                }
                else
                {
                    var config = new Configuration
                    {
                        Pointer = pointer,
                        Reference = reference,
                        ConsigneeUnitId = model.BranchId,
                        Attribute = "Digital Certificate Path",
                        CurrentValue = digitalCertPath,
                        PreviousValue = digitalCertPath,
                        Remark = null
                    };
                    _context.Configurations.Add(config);
                }
            }

            if (!string.IsNullOrEmpty(privateKeyPath))
            {
                var existingKey = _context.Configurations
                    .FirstOrDefault(c => c.Pointer == pointer &&
                                         c.Reference == reference &&
                                         c.ConsigneeUnitId == model.BranchId &&
                                         c.Attribute == "Private Key Path");

                if (existingKey != null)
                {
                    // Delete old file from FTP
                    if (!string.IsNullOrEmpty(existingKey.CurrentValue))
                    {
                        string oldRemotePath = $"{ftpBaseUrl}/{existingKey.CurrentValue}";
                        await _ftpHelper.DeleteFileAsync(oldRemotePath);
                    }

                    existingKey.PreviousValue = existingKey.CurrentValue;
                    existingKey.CurrentValue = privateKeyPath;
                }
                else
                {
                    var config = new Configuration
                    {
                        Pointer = pointer,
                        Reference = reference,
                        ConsigneeUnitId = model.BranchId,
                        Attribute = "Private Key Path",
                        CurrentValue = privateKeyPath,
                        PreviousValue = privateKeyPath,
                        Remark = null
                    };
                    _context.Configurations.Add(config);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Settings saved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving branch settings for branch {BranchId}", model?.BranchId);
            if (ex.InnerException != null)
                _logger.LogError(ex.InnerException, "Inner exception details");
            return Json(new { success = false, message = $"An error occurred while saving: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult CheckSession()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var companyId = HttpContext.Session.GetInt32("CompanyId");
        var userName = HttpContext.Session.GetString("UserName");
        var verifiedTin = HttpContext.Session.GetString("VerifiedTin");

        return Json(new
        {
            UserId = userId,
            CompanyId = companyId,
            UserName = userName,
            VerifiedTin = verifiedTin,
            SessionId = HttpContext.Session.Id,
            SessionKeys = HttpContext.Session.Keys.ToList()
        });
    }


    [HttpGet]
    public async Task<IActionResult> UserManagement()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login");

        if (!IsCurrentUserAdmin())
        {
            _logger.LogWarning("Unauthorized access attempt to UserManagement by UserId: {UserId}", userId);
            TempData["ErrorMessage"] = "Access denied. Administrator privileges required.";
            return RedirectToAction("Parameters");
        }

        try
        {
            var employees = await _context.Consignees
                .Where(c => c.GslType == 26 && c.IsActive == true)
                .Select(c => new EmployeeViewModel
                {
                    Id = c.Id,
                    FirstName = c.FirstName ?? "",
                    SecondName = c.SecondName ?? "",
                    FullName = (c.FirstName ?? "") + " " + (c.SecondName ?? ""),
                    HasUser = _context.Users.Any(u => u.PersonId == c.Id)
                })
                .ToListAsync();

            var roles = await _context.ConsigneeUnits
                .Where(r => r.Type == 1725 && r.IsActive)
                .Select(r => new { r.id, r.Name })
                .ToListAsync();

            var users = await _context.Users
                .Select(u => new UserManagementViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "Unknown",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt ?? DateTime.Now,
                    LastLoginAt = u.LastLoginAt,
                    EmployeeName = u.Person == null
                        ? "No Employee Linked"
                        : (u.Person.FirstName ?? "") + " " + (u.Person.SecondName ?? ""),
                    Roles = _context.UserRoleMappers
                        .Where(urm => urm.UserId == u.Id && urm.Role != null)
                        .Select(urm => urm.Role.Name)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.Employees = employees;
            ViewBag.Roles = roles;
            ViewBag.Users = users;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserManagement: {Message}", ex.Message);
            TempData["ErrorMessage"] = "Technical error: " + ex.Message;
            ViewBag.Employees = new List<EmployeeViewModel>();
            ViewBag.Users = new List<UserManagementViewModel>();
            ViewBag.Roles = new List<dynamic>();
            return View();
        }
    }


    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        if (!IsCurrentUserAdmin())
        {
            _logger.LogWarning("Unauthorized access attempt to CreateUser by UserId: {UserId}", userId);
            TempData["ErrorMessage"] = "Access denied. Administrator privileges required.";
            return RedirectToAction("Parameters");
        }

        try
        {
            var roles = await _context.ConsigneeUnits
                .Where(r => r.Type == 1725 && r.IsActive)
                .Select(r => new { r.id, r.Name })
                .ToListAsync();

            ViewBag.Roles = roles;
            return View(new UserCreationViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create user page");
            TempData["ErrorMessage"] = "An error occurred while loading the page.";
            return RedirectToAction("UserManagement");
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserCreationViewModel model)
    {
        try
        {
            var currentSessionUserId = HttpContext.Session.GetInt32("UserId");
            if (currentSessionUserId == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                await PrepareViewBag();
                return View(model);
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.UserName.ToLower() == model.Username.ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                await PrepareViewBag();
                return View(model);
            }

            var currentTime = DateTime.Now;

            var employee = new Consignee
            {
                Code = GenerateEmployeeCode(),
                GslType = 26,
                FirstName = model.FirstName,
                SecondName = model.SecondName,
                Preference = 2,
                IsActive = true,
                IsPerson = true,
                CreatedOn = currentTime,
                LastModified = currentTime
            };

            _context.Consignees.Add(employee);
            await _context.SaveChangesAsync();

            var (hash, salt) = PasswordHelper.HashPassword(model.Password, model.Username);


            var user = new User
            {
                PersonId = employee.Id,
                UserName = model.Username,
                PasswordHash = hash,
                Salt = salt,
                IsActive = model.IsActive,
                CreatedAt = currentTime,
                LoggedInStatus = 1390,
                Remark = model.Remark
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userRole = new UserRoleMapper
            {
                UserId = user.Id,
                RoleId = model.RoleId,
                ExpiryDate = DateTime.Now.AddYears(50),
                Remark = "Assigned"
            };

            _context.UserRoleMappers.Add(userRole);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{model.Username}' created successfully.";
            return RedirectToAction("UserManagement");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");

            string errorMessage = ex.Message;
            if (ex.InnerException != null) errorMessage += $" | Inner: {ex.InnerException.Message}";

            ModelState.AddModelError("", $"System Error: {errorMessage}");
            await PrepareViewBag();
            return View(model);
        }
    }


    //GET Edit User
    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        if (!IsCurrentUserAdmin())
        {
            TempData["ErrorMessage"] = "Access denied. Administrator privileges required.";
            return RedirectToAction("Parameters");
        }

        try
        {
            _logger.LogInformation("Loading edit user page for UserId: {Id}", id);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("UserManagement");
            }

            var person = await _context.Consignees.FirstOrDefaultAsync(c => c.Id == user.PersonId);

            var userRole = await _context.UserRoleMappers
                .Where(urm => urm.UserId == user.Id)
                .Select(urm => urm.RoleId)
                .FirstOrDefaultAsync();

            var roles = await _context.ConsigneeUnits
                .Where(r => r.Type == 1725 && r.IsActive)
                .Select(r => new { r.id, r.Name })
                .ToListAsync();

            var model = new UserCreationViewModel
            {
                FirstName = person?.FirstName ?? "",
                SecondName = person?.SecondName,
                Username = user.UserName ?? "",
                IsActive = user.IsActive,
                Remark = user.Remark,
                RoleId = userRole,
                Password = "",
                ConfirmPassword = ""
            };

            ViewBag.Roles = roles;
            ViewBag.UserId = user.Id;
            ViewBag.EmployeeId = user.PersonId;

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit user page for UserId: {Id}", id);
            TempData["ErrorMessage"] = $"An error occurred while loading the page: {ex.Message}";
            return RedirectToAction("UserManagement");
        }
    }


    // POST: Edit User
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, UserCreationViewModel model)
    {
        try
        {
            var currentSessionUserId = HttpContext.Session.GetInt32("UserId");
            if (currentSessionUserId == null) return RedirectToAction("Login");

            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Administrator privileges required.";
                return RedirectToAction("Parameters");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("UserManagement");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                await PrepareViewBag(id);
                return View(model);
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Id != id && u.UserName.ToLower() == model.Username.ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                await PrepareViewBag(id);
                return View(model);
            }

            var person = await _context.Consignees.FirstOrDefaultAsync(c => c.Id == user.PersonId);
            if (person != null)
            {
                person.FirstName = model.FirstName;
                person.SecondName = model.SecondName;
                person.LastModified = DateTime.Now;
            }

            bool usernameChanged = user.UserName.ToLower() != model.Username.ToLower();
            user.UserName = model.Username;
            user.IsActive = model.IsActive;
            user.Remark = model.Remark;
            user.ModifiedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var (hash, salt) = PasswordHelper.HashPassword(model.Password, model.Username);
                user.PasswordHash = hash;
                user.Salt = salt;
            }
            else if (usernameChanged)
            {
                ModelState.AddModelError("Password", "You must provide a password if you change the username.");
                await PrepareViewBag(id);
                return View(model);
            }

            var existingRole = await _context.UserRoleMappers
                .FirstOrDefaultAsync(urm => urm.UserId == user.Id);

            if (existingRole != null)
            {
                existingRole.RoleId = model.RoleId;
                if (existingRole.ExpiryDate == null || existingRole.ExpiryDate <= DateTime.Now)
                    existingRole.ExpiryDate = DateTime.Now.AddYears(50);
            }
            else
            {
                _context.UserRoleMappers.Add(new UserRoleMapper
                {
                    UserId = user.Id,
                    RoleId = model.RoleId,
                    ExpiryDate = DateTime.Now.AddYears(50),
                    Remark = "Assigned by administrator"
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{model.Username}' updated successfully.";
            return RedirectToAction("UserManagement");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing user {Id}", id);
            ModelState.AddModelError("", $"Update failed: {ex.Message}");
            await PrepareViewBag(id);
            return View(model);
        }
    }

    private async Task PrepareViewBag(int? id = null)
    {
        var roles = await _context.ConsigneeUnits
            .Where(r => r.Type == 1725 && r.IsActive)
            .Select(r => new { r.id, r.Name })
            .ToListAsync();
        ViewBag.Roles = roles;
        if (id.HasValue) ViewBag.UserId = id.Value;
    }


    // POST: Toggle User Status
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleStatusModel model)
    {
        try
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsActive = !user.IsActive;
            user.ModifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = user.IsActive,
                message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    // GET: Check if username exists
    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username, int? excludeId = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }

            var exists = await query.AnyAsync(u => u.UserName.ToLower() == username.ToLower());

            return Json(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username");
            return Json(new { exists = false, error = true });
        }
    }

    public class ToggleStatusModel
    {
        public int Id { get; set; }
    }


    [HttpGet]
    public async Task<IActionResult> DownloadCertificate(string filePath)
    {
        var tin = HttpContext.Session.GetString("VerifiedTin");
        if (string.IsNullOrEmpty(tin) || !filePath.StartsWith($"{tin}"))
            return Unauthorized();

        string fullRemotePath = $"{_configuration["Ftp:BaseUrl"].TrimEnd('/')}/{filePath}";
        var stream = await _ftpHelper.DownloadFileAsync(fullRemotePath);
        if (stream == null) return NotFound();

        return File(stream, "application/octet-stream", Path.GetFileName(filePath));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        HttpContext.Session.Clear();

        return RedirectToAction("Login", "System");
    }

}

public class UserManagementViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string EmployeeName { get; set; }
    public List<string> Roles { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class EmployeeViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string SecondName { get; set; }
    public string FullName { get; set; }
    public bool HasUser { get; set; }
}