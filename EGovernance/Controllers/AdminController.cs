
using EGOVERNANCE.Data;
using EGOVERNANCE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGOVERNANCE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // ADMIN DASHBOARD
        // =========================

        public async Task<IActionResult> Index()
        {
            var complaints = _context.Complaints;

            ViewBag.TotalComplaints = await complaints.CountAsync();

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

        // =========================
        // ALL CITIZEN COMPLAINTS
        // =========================

        [HttpGet]
        public async Task<IActionResult> Complaints(string? status)
        {
            var complaints = _context.Complaints
                .Include(c => c.User)
                .AsQueryable();

            // Filter complaints by status if selected.
            if (!string.IsNullOrWhiteSpace(status))
            {
                complaints = complaints.Where(c => c.Status == status);
            }

            var complaintList = await complaints
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;

            return View(complaintList);
        }
        // =========================
        // VIEW COMPLAINT DETAILS
        // =========================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return NotFound();
            }

            return View(complaint);
        }


        // =========================
        // UPDATE COMPLAINT STATUS
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaint(
    int id,
    string status,
    string? adminRemarks)
        {
            var allowedStatuses = new[]
            {
        "Pending",
        "In Progress",
        "Resolved",
        "Rejected"
    };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid complaint status.";
                return RedirectToAction("Details", new { id });
            }

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return NotFound();
            }

            // Update the main complaint record.
            complaint.Status = status;
            complaint.AdminRemarks = adminRemarks;

            // Create a history record.
            var history = new ComplaintHistory
            {
                ComplaintId = complaint.Id,
                Status = status,
                Remarks = adminRemarks,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = User.Identity?.Name
            };

            // Save both changes to SQL Server.
            _context.ComplaintHistories.Add(history);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Complaint updated and history saved successfully.";

            return RedirectToAction("Details", new { id });
        }
    }
}
