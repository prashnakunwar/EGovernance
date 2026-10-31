
using EGOVERNANCE.Data;
using EGOVERNANCE.Models;
using EGOVERNANCE.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGOVERNANCE.Controllers
{
    [Authorize(Roles = "Citizen")]
    public class CitizenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public CitizenController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ==========================================
        // CITIZEN DASHBOARD
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var complaints = _context.Complaints
                .Where(c => c.UserId == user.Id);

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            ViewBag.TotalComplaints =
                await complaints.CountAsync();

            ViewBag.PendingComplaints =
                await complaints.CountAsync(c => c.Status == "Pending");

            ViewBag.InProgressComplaints =
                await complaints.CountAsync(c => c.Status == "In Progress");

            ViewBag.ResolvedComplaints =
                await complaints.CountAsync(c => c.Status == "Resolved");

            ViewBag.RejectedComplaints =
                await complaints.CountAsync(c => c.Status == "Rejected");

            ViewBag.RecentComplaints = await complaints
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ==========================================
        // SUBMIT COMPLAINT - GET
        // ==========================================

        [HttpGet]
        public IActionResult SubmitComplaint()
        {
            return View(new ComplaintSubmissionViewModel());
        }

        // ==========================================
        // SUBMIT COMPLAINT - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(
            ComplaintSubmissionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            string? imagePath = null;

            // Handle optional image upload.
            if (model.Image != null && model.Image.Length > 0)
            {
                const long maxFileSize = 5 * 1024 * 1024;

                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                var extension = Path.GetExtension(
                    model.Image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.Image),
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    return View(model);
                }

                if (model.Image.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.Image),
                        "Image size cannot exceed 5 MB.");

                    return View(model);
                }

                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "complaints");

                Directory.CreateDirectory(uploadFolder);

                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

                var filePath = Path.Combine(
                    uploadFolder,
                    uniqueFileName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                imagePath = $"/uploads/complaints/{uniqueFileName}";
            }

            // Create the complaint.
            var complaint = new Complaint
            {
                ComplaintNumber =
                    $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",

                Title = model.Title,
                Category = model.Category,
                Description = model.Description,
                Location = model.Location,
                WardNumber = model.WardNumber,
                ImagePath = imagePath,

                Status = "Pending",
                CreatedAt = DateTime.UtcNow,

                // Connect complaint to logged-in citizen.
                UserId = user.Id
            };

            try
            {
                _context.Complaints.Add(complaint);

                // Save initial complaint history.
                _context.ComplaintHistories.Add(
                    new ComplaintHistory
                    {
                        Complaint = complaint,
                        Status = "Pending",
                        Remarks = "Complaint submitted successfully.",
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "System"
                    });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Delete uploaded image if database saving fails.
                if (imagePath != null)
                {
                    var savedFilePath = Path.Combine(
                        _environment.WebRootPath,
                        imagePath.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(savedFilePath))
                    {
                        System.IO.File.Delete(savedFilePath);
                    }
                }

                throw;
            }

            TempData["SuccessMessage"] =
                $"Your complaint was submitted successfully. " +
                $"Your complaint number is {complaint.ComplaintNumber}.";

            return RedirectToAction(nameof(MyComplaints));
        }

        // ==========================================
        // MY COMPLAINTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> MyComplaints()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Show only complaints submitted by this citizen.
            var complaints = await _context.Complaints
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(complaints);
        }

        // ==========================================
        // TRACK COMPLAINT - GET
        // ==========================================

        [HttpGet]
        [ActionName("TrackComplaint")]
        public async Task<IActionResult> TrackComplaintGet(
            string? complaintNumber)
        {
            // Display tracking form if no number is provided.
            if (string.IsNullOrWhiteSpace(complaintNumber))
                return View("TrackComplaint");

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Find only the logged-in citizen's complaint.
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c =>
                    c.ComplaintNumber == complaintNumber.Trim() &&
                    c.UserId == user.Id);

            if (complaint == null)
                return NotFound();

            // Retrieve complaint status history.
            var history = await _context.ComplaintHistories
                .Where(h => h.ComplaintId == complaint.Id)
                .OrderBy(h => h.UpdatedAt)
                .ToListAsync();

            ViewBag.ComplaintHistory = history;

            return View("TrackComplaintResult", complaint);
        }

        // ==========================================
        // TRACK COMPLAINT - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackComplaint(
            string complaintNumber)
        {
            if (string.IsNullOrWhiteSpace(complaintNumber))
            {
                ModelState.AddModelError(
                    "",
                    "Please enter your complaint number.");

                return View("TrackComplaint");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Find complaint using reference number
            // and authenticated citizen ID.
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c =>
                    c.ComplaintNumber == complaintNumber.Trim() &&
                    c.UserId == user.Id);

            if (complaint == null)
            {
                ModelState.AddModelError(
                    "",
                    "Complaint not found. Please check your complaint number.");

                return View("TrackComplaint");
            }

            // Get all recorded status updates and remarks.
            var history = await _context.ComplaintHistories
                .Where(h => h.ComplaintId == complaint.Id)
                .OrderBy(h => h.UpdatedAt)
                .ToListAsync();

            ViewBag.ComplaintHistory = history;

            return View("TrackComplaintResult", complaint);
        }

        // ==========================================
        // MUNICIPAL NOTICES
        // ==========================================

        [HttpGet]
        public IActionResult Notices()
        {
            return View();
        }

        // ==========================================
        // MUNICIPAL SERVICES
        // ==========================================

        [HttpGet]
        public IActionResult Services()
        {
            return View();
        }

        // ==========================================
        // CITIZEN PROFILE
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.PhoneNumber = user.PhoneNumber;

            return View();
        }
    }
}
