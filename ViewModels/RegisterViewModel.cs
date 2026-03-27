using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartBorrowLK.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; } // Handles the file upload
    }
}