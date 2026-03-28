using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBorrowLK.Data;
using SmartBorrowLK.Models;
using PusherServer;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBorrowLK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Pusher _pusher;

        public ChatController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            
            var options = new PusherOptions
            {
                Cluster = config["NEXT_PUBLIC_PUSHER_CLUSTER"] ?? "ap2",
                Encrypted = true
            };

            _pusher = new Pusher(
                config["PUSHER_APP_ID"] ?? "",
                config["NEXT_PUBLIC_PUSHER_KEY"] ?? "",
                config["PUSHER_SECRET"] ?? "",
                options
            );
        }

        private int? GetCurrentUserId()
        {
            return User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0") : null;
        }

        [HttpGet("history/{receiverId}")]
        public async Task<IActionResult> GetHistory(int receiverId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Also send back receiver's basic info so the chat UI knows who they are talking to
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == receiverId);
            if (receiver == null) return NotFound("Receiver not found");

            var receiverInfo = new {
                id = receiver.Id,
                name = receiver.Name,
                profileImageUrl = receiver.ProfileImageUrl
            };

            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    id = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            var unread = await _context.Messages
                .Where(m => m.SenderId == receiverId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();
                
            if (unread.Any())
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Ok(new { receiver = receiverInfo, messages = messages });
        }

        public class SendMessageRequest
        {
            public int ReceiverId { get; set; }
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Message content cannot be empty.");

            var message = new Message
            {
                SenderId = userId.Value,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var messageData = new {
                id = message.Id,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                createdAt = message.CreatedAt
            };

            // Trigger the pusher event on the receiver's specific channel
            var channelName = $"chat-{request.ReceiverId}";
            await _pusher.TriggerAsync(channelName, "new-message", messageData);

            return Ok(messageData);
        }
        
        // Helper endpoint to check any unread messages for navbar dot
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            
            var unreadCount = await _context.Messages.CountAsync(m => m.ReceiverId == userId.Value && !m.IsRead);
            return Ok(new { count = unreadCount });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => 
                {
                    var lastMessage = g.First();
                    var partnerId = g.Key;
                    var partner = lastMessage.SenderId == userId ? lastMessage.Receiver : lastMessage.Sender;
                    var unread = g.Count(m => m.ReceiverId == userId && !m.IsRead);
                    return new {
                        partnerId = partnerId,
                        partnerName = partner?.Name ?? "Unknown",
                        partnerAvatar = partner?.ProfileImageUrl,
                        lastMessage = lastMessage.Content,
                        lastMessageTime = lastMessage.CreatedAt,
                        unreadCount = unread
                    };
                })
                .ToList();

            return Ok(conversations);
        }
    }
}
