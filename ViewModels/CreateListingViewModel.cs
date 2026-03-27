using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartBorrowLK.ViewModels
{
    public class CreateListingViewModel
    {
        [Required]
        [Display(Name = "Item Name (e.g., Sony A7III)")]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Condition (e.g., Like New, Good)")]
        public string Condition { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Brief Description for AI")]
        public string RawDescription { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Item Image")]
        public IFormFile Image { get; set; } = null!;
    }
}