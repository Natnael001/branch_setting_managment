using E_Invoice_Parameter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace E_Invoice_Parameter.Controllers
{
    [Authorize]
    public class DeviceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(AppDbContext context, ILogger<DeviceController> logger)
        {
            _context = context;
            _logger = logger;
        }
        private async Task<string> GenerateFixedAssetCode()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string prefix = "AST" + datePart;

            var lastArticle = await _context.Articles
                .Where(a => a.LocalCode.StartsWith(prefix))
                .OrderByDescending(a => a.LocalCode)
                .Select(a => a.LocalCode)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastArticle))
            {
                return prefix + "001";
            }

            string lastSeq = lastArticle.Substring(lastArticle.Length - 3);
            if (int.TryParse(lastSeq, out int seq))
            {
                seq++;
                return prefix + seq.ToString("D3");
            }

            return prefix + "001";
        }

        private async Task LoadDropdownData()
        {
            // Categories - Type "Device" with Id 599
            ViewBag.Categories = await _context.SystemConstants

                //.Where(c => c.Type == "Device" && c.Id == 599)
                .Where(c => c.Type == "Device")
                .ToListAsync();

            // Connection Types - Category "Connection Type"
            ViewBag.ConnectionTypes = await _context.SystemConstants
                .Where(c => c.Category == "Connection Type" )
                .ToListAsync();

            //// Suppliers - Category "Fiscal Printer Supplier"
            //ViewBag.Suppliers = await _context.SystemConstants
            //    .Where(c => c.Category == "Fiscal Printer Supplier")

            //    .ToListAsync();

            // Models - Category "Fiscal Printer Model"
            ViewBag.Model_two = await _context.SystemConstants
                .Where(c => c.Category == "Fiscal Printer Model")
                .ToListAsync();

            //// Prefixes - Category "MRC Prefix" (for device value)
            //ViewBag.Prefixes = await _context.SystemConstants
            //    .Where(c => c.Category == "MRC Prefix" )

            //    .ToListAsync();

            // 1. Get the CompanyId from the session
            var companyId = HttpContext.Session.GetInt32("CompanyId");

            // 2. Fetch the branches using the specific filters you want
            var branches = await _context.ConsigneeUnits
                .Where(u => u.ConsigneeId == companyId && u.IsActive && u.Type == 1719)
                .ToListAsync();

            // 3. Assign it to the ViewBag
            ViewBag.Branches = branches;
            ////ViewBag.fixedAssetCode = await GenerateFixedAssetCode();
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Verification", "System");

            try
            {
                var device = await _context.Device
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                {
                    TempData["ErrorMessage"] = "Device not found.";
                    return RedirectToAction("Index");
                }

                // Get article details if needed
                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.Id == device.Article);

                var model = new DeviceDetailsViewModel
                {
                    Id = device.Id,
                    DeviceName = device.MachineName.Trim(),
                    Category = GetCategoryName(device.Type),
                    ConnectionType = GetConnectionTypeName(device.ConnectionType),
                    Supplier = article?.DefaultSupplier?.ToString() ?? "N/A",
                    Model = device.Description ?? "N/A",
                    FixedAssetCode = device.Article.ToString(),
                    DeviceValue = device.DeviceValue ?? "N/A",
                    IsActive = device.IsActive,
                    Remark = device.Remark,
                    CreatedAt = device.CreatedOn ?? DateTime.Now,
                    ModifiedAt = device.LastModified,
                    BranchName = GetBranchName(device.ConsigneeUnit)
                };

                return View("~/Views/System/DeviceDetails.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading device details for ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading device details.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Verification", "System");
            try
            {
                var devices = await (
            from d in _context.Device
            join cat in _context.SystemConstants
                on d.Type equals cat.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            join article in _context.Articles
                on d.Article equals article.Id into articleJoin
            from art in articleJoin.DefaultIfEmpty()
            join branch in _context.ConsigneeUnits
                on d.ConsigneeUnit equals branch.id into branchJoin
            from branchUnit in branchJoin.DefaultIfEmpty()
            orderby d.CreatedOn descending
            select new DeviceListViewModel
            {
                Id = d.Id,
                DeviceName = d.MachineName,
                Category = category != null ? category.Description : "Unknown",
                Model = d.Description ?? "N/A",
                FixedAssetCode = art != null ? art.LocalCode : "N/A",  // From Article table
                DeviceValue = d.DeviceValue ?? "N/A",
                IsActive = d.IsActive,
                CreatedAt = d.CreatedOn ?? DateTime.Now,
                BranchName = branchUnit != null ? branchUnit.Name : "Main"
            })
            .ToListAsync();

                return View("~/Views/System/deviceManagment.cshtml", devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Device Management page");
                TempData["ErrorMessage"] = "An error occurred while loading devices.";
                return View("~/Views/System/deviceManagment.cshtml", new List<DeviceListViewModel>());
            }
        }

        private string GetCategoryName(int typeId)
        {
            var category = _context.ConsigneeUnits
                .FirstOrDefault(c => c.id == typeId && c.Type == 1800);
            return category?.Name ?? "Unknown";
        }

        private string GetBranchName(int? consigneeUnitId)
        {
            if (consigneeUnitId == null) return "Main";
            var branch = _context.ConsigneeUnits
                .FirstOrDefault(b => b.id == consigneeUnitId && b.Type == 1719);
            return branch?.Name ?? "Unknown";
        }
        private async Task<Article> CreateArticle(DeviceCreationViewModel model, int userId)
        {
            var article = new Article
            {
                LocalCode = model.FixedAssetCode,  // The generated code
                GslType = 15,  // Device type constant
                Name = model.DeviceName.Trim(),
                Uom = 1400,  // Default UOM
                Preference = 1,
                NeedsUOMConversion = true,
                //Description = model.Description,
                //DefaultSupplier = model.SupplierId,
                //Model = model.Description,
                User = userId,
                Remark = model.Remark,
                CreatedOn = DateTime.Now,
                LastModified = DateTime.Now,
                IsActive = true,
                Locked = false
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return article;
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Verification", "System");

            try
            {
                await LoadDropdownData();

                var fixedAssetCode = await GenerateFixedAssetCode();

                var model = new DeviceCreationViewModel
                {
                    FixedAssetCode = fixedAssetCode,
                    IsActive = true
                };

                // Debug: Log the generated code
                _logger.LogInformation($"Generated FixedAssetCode: {fixedAssetCode}");

                return View("~/Views/System/DeviceCreate.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create Device page");
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeviceCreationViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Verification", "System");

            // DEBUG: Log received values
            _logger.LogInformation($"Received POST - DeviceName: {model.DeviceName}, CategoryId: {model.CategoryId}, FixedAssetCode: {model.FixedAssetCode}");

            try
            {
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }

                    await LoadDropdownData();
                    return View("~/Views/System/DeviceCreate.cshtml", model);
                }

                // Create Article
                var article = new Article
                {
                    LocalCode = model.FixedAssetCode,
                    GslType = 15,
                    Name = model.DeviceName,
                    Uom = 1400,
                    Preference = 1,
                    NeedsUOMConversion = true,
                    User = userId.Value,
                    Remark = model.Remark,
                    CreatedOn = DateTime.Now,
                    LastModified = DateTime.Now,
                    IsActive = true,
                    Locked = false
                };

                _context.Articles.Add(article);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Article created with ID: {article.Id}");

                // Create Device
                var device = new Device
                {
                    Article = article.Id,
                    MachineName = model.DeviceName,
                    Type = model.CategoryId,
                    IsActive = model.IsActive,
                    Remark = model.Remark,
                    CreatedOn = DateTime.Now,
                    LastModified = DateTime.Now
                };

                _context.Device.Add(device);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Device created with ID: {device.Id}");

                TempData["SuccessMessage"] = $"Device '{device.MachineName}' created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await LoadDropdownData();
                return View("~/Views/System/DeviceCreate.cshtml", model);
            }
        }
        private async Task<Device> CreateDevice(DeviceCreationViewModel model, int articleId, int userId)
        {
            var device = new Device
            {
                Article = articleId,  // The Article ID we just created
                MachineName = model.DeviceName,
                Type = model.CategoryId,
                //Description = model.Description,
                //ConnectionType = model.ConnectionType,
                //DeviceValue = model.DeviceValue,
                //ConsigneeUnit = model.BranchId,
                IsActive = model.IsActive,
                Remark = model.Remark,
                CreatedOn = DateTime.Now,
                LastModified = DateTime.Now
            };

            _context.Device.Add(device);
            await _context.SaveChangesAsync();

            return device;
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Verification", "System");

            try
            {
                var device = await _context.Device
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                {
                    TempData["ErrorMessage"] = "Device not found.";
                    return RedirectToAction("Index");
                }

                // Get the Article
                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.Id == device.Article);

                await LoadDropdownData();

                var model = new DeviceCreationViewModel
                {
                    Id = device.Id,
                    CategoryId = device.Type,
                    DeviceName = device.MachineName,
                    FixedAssetCode = article?.LocalCode ?? "N/A",
                    ArticleId = device.Article,  // Store Article ID
                    Description = device.Description,
                    BranchId = device.ConsigneeUnit,
                    IsActive = device.IsActive,
                    Remark = device.Remark
                };

                return View("~/Views/System/DeviceEdit.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit Device page for ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the device.";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeviceCreationViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Verification", "System");

            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Invalid device ID.";
                return RedirectToAction("Index");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDropdownData();
                    return View("~/Views/System/DeviceEdit.cshtml", model);
                }

                var device = await _context.Device.FindAsync(id);
                if (device == null)
                {
                    TempData["ErrorMessage"] = "Device not found.";
                    return RedirectToAction("Index");
                }

                // Get the associated Article
                var article = await _context.Articles.FindAsync(device.Article);
                if (article == null)
                {
                    TempData["ErrorMessage"] = "Associated article not found.";
                    return RedirectToAction("Index");
                }

                // Check if device name already exists (excluding current device)
                var deviceExists = await _context.Device
                    .AnyAsync(d => d.Id != id && d.MachineName.ToLower() == model.DeviceName.ToLower());

                if (deviceExists)
                {
                    ModelState.AddModelError("DeviceName", "A device with this name already exists.");
                    await LoadDropdownData();
                    return View("~/Views/System/DeviceEdit.cshtml", model);
                }

                // UPDATE ARTICLE (Important!)
                article.Name = model.DeviceName;  // Update article name
                article.Description = model.Description;
                article.Remark = model.Remark;
                article.LastModified = DateTime.Now;
                // Keep LocalCode unchanged

                // UPDATE DEVICE
                device.MachineName = model.DeviceName;
                device.Type = model.CategoryId;
                device.Description = model.Description;
                device.ConsigneeUnit = model.BranchId;
                device.IsActive = model.IsActive;
                device.Remark = model.Remark;
                device.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Device and Article updated: {DeviceName} by UserId: {UserId}", device.MachineName, userId);
                TempData["SuccessMessage"] = $"Device '{device.MachineName}' updated successfully!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing device {Id}", id);
                ModelState.AddModelError("", $"Error updating device: {ex.Message}");
                await LoadDropdownData();
                return View("~/Views/System/DeviceEdit.cshtml", model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Session expired." });

            try
            {
                var device = await _context.Device.FindAsync(id);
                if (device == null)
                {
                    return Json(new { success = false, message = "Device not found." });
                }

                _context.Device.Remove(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Device deleted: {DeviceName} by UserId: {UserId}", device.MachineName, userId);
                return Json(new { success = true, message = "Device deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device {Id}", id);
                return Json(new { success = false, message = $"Error deleting device: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Session expired." });

            try
            {
                var device = await _context.Device.FindAsync(id);
                if (device == null)
                {
                    return Json(new { success = false, message = "Device not found." });
                }

                device.IsActive = !device.IsActive;
                device.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();

                var status = device.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("Device {Status}: {DeviceName} by UserId: {UserId}", status, device.MachineName, userId);

                return Json(new
                {
                    success = true,
                    isActive = device.IsActive,
                    message = $"Device {status} successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling device status for ID: {Id}", id);
                return Json(new { success = false, message = $"Error toggling status: {ex.Message}" });
            }
        }

  
        private string GetConnectionTypeName(int? connectionTypeId)
        {
            if (connectionTypeId == null) return "N/A";
            var connType = _context.SystemConstants
                .FirstOrDefault(c => c.Id == connectionTypeId && c.Category == "Connection Type");
            return connType?.Description ?? "Unknown";
        }

        private async Task<string> GetLookupName(int id)
        {
            var item = await _context.ConsigneeUnits
                .Where(c => c.id == id)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            return item ?? "N/A";
        }
    }

    public class DeviceCreationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Device name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Device name must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$", ErrorMessage = "Device name can only contain letters, numbers, spaces, hyphens, and underscores")]
        public string DeviceName { get; set; }

        public string FixedAssetCode { get; set; }
        public string Model_two { get; set; }

        public int ArticleId { get; set; }

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_,.():;]+$", ErrorMessage = "Description contains invalid characters")]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid branch")]
        public int? BranchId { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500, ErrorMessage = "Remark cannot exceed 500 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_,.():;""']+$", ErrorMessage = "Remark contains invalid characters")]
        public string? Remark { get; set; }
    }

    public class DeviceListViewModel
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }
        public string Category { get; set; }  
        public string Model { get; set; }
        public string FixedAssetCode { get; set; }
        public string DeviceValue { get; set; }
        public string SerialNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BranchName { get; set; }
    }

    public class DeviceDetailsViewModel
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }
        public string Category { get; set; }  // Changed to string
        public string ConnectionType { get; set; }
        public string Supplier { get; set; }
        public string Model { get; set; }
        public string FixedAssetCode { get; set; }
        public string DeviceValue { get; set; }
        public string SerialNumber { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public bool IsActive { get; set; }
        public string Remark { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string BranchName { get; set; }
    }
}