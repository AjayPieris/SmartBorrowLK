using System.ComponentModel.DataAnnotations;

namespace SmartBorrowLK.ViewModels
{
    public class CreateReviewViewModel
    {
        public int ListingId { get; set; }
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Please write at least 10 characters.")]
        public string Comment { get; set; } = string.Empty;
    }
}