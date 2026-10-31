using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EGOVERNANCE.Models
{
    public class ComplaintHistory
    {
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }

        [ForeignKey(nameof(ComplaintId))]
        public Complaint? Complaint { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? UpdatedBy { get; set; }
    }
}