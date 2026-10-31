using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EGOVERNANCE.ViewModels
{
    public class ComplaintSubmissionViewModel
    {
        [Required(ErrorMessage = "Please enter a complaint title.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a complaint category.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe your complaint.")]
        [StringLength(3000, MinimumLength = 10,
            ErrorMessage = "Description must be between 10 and 3000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the location.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your ward number.")]
        public string WardNumber { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}