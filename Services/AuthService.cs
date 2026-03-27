using Microsoft.EntityFrameworkCore;
using SmartBorrowLK.Data;
using SmartBorrowLK.Models;
using SmartBorrowLK.ViewModels;

namespace SmartBorrowLK.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterViewModel model);
        Task<User?> LoginAsync(LoginViewModel model);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public AuthService(AppDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<User?> RegisterAsync(RegisterViewModel model)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return null; 
            }

            string imageUrl = string.Empty;
            if (model.ProfileImage != null)
            {
                // Call our Cloudinary service
                imageUrl = await _cloudinaryService.UploadImageAsync(model.ProfileImage);
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password), // Secure hashing
                Role = "Vendor", // Default role
                ProfileImageUrl = imageUrl
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(LoginViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            
            // Verify user exists and password matches
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return null;
            }

            return user;
        }
    }
}