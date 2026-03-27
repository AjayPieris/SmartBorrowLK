namespace SmartBorrowLK.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; 
        public string Role { get; set; } = "Vendor"; // Admin, Vendor
        
        // Add this line to store the Cloudinary URL
        public string ProfileImageUrl { get; set; } = string.Empty; 
    }
}