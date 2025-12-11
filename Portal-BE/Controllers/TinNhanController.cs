using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Config;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TinNhanController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TinNhanController(ThinhContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 0;
        }

        /// <summary>
        /// Lấy danh sách cuộc trò chuyện (CuocTroChuyen)
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var list = await _context.CuocTroChuyens
                .OrderByDescending(c => c.NgayTao)
                .Select(c => new
                {
                    c.Id,
                    c.TenCuocTroChuyen,
                    c.LaNhom,
                    c.NgayTao,
                    TinNhanGanNhat = c.TinNhans
                        .OrderByDescending(t => t.ThoiGianGui)
                        .Select(t => new
                        {
                            t.Id,
                            t.IdNguoiGui,
                            t.NoiDung,
                            t.ThoiGianGui
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách cuộc trò chuyện thành công", data = list });
        }

        /// <summary>
        /// Lấy danh sách tin nhắn theo cuộc trò chuyện
        /// </summary>
        [HttpGet("messages/{conversationId}")]
        public async Task<IActionResult> GetMessagesByConversation(int conversationId)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var conversation = await _context.CuocTroChuyens.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy cuộc trò chuyện" });
            }

            var messages = await _context.TinNhans
                .Where(t => t.IdCuocTroChuyen == conversationId)
                .OrderBy(t => t.ThoiGianGui)
                .Select(t => new
                {
                    t.Id,
                    t.IdCuocTroChuyen,
                    t.IdNguoiGui,
                    t.NoiDung,
                    t.ThoiGianGui,
                    t.DaXem
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách tin nhắn thành công", data = messages });
        }

        /// <summary>
        /// Gửi tin nhắn
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            if (request == null || request.IdCuocTroChuyen <= 0 || string.IsNullOrWhiteSpace(request.NoiDung))
            {
                return Ok(new { statusCode = 400, message = "Dữ liệu không hợp lệ" });
            }

            var conversation = await _context.CuocTroChuyens.FirstOrDefaultAsync(c => c.Id == request.IdCuocTroChuyen);
            if (conversation == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy cuộc trò chuyện" });
            }

            var message = new TinNhan
            {
                IdCuocTroChuyen = request.IdCuocTroChuyen,
                IdNguoiGui = request.IdNguoiGui ?? userId,
                NoiDung = request.NoiDung,
                ThoiGianGui = DateTime.Now,
                DaXem = false
            };

            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast realtime qua SignalR group conversation-{id}
            try
            {
                await _hubContext.Clients.Group($"conversation-{request.IdCuocTroChuyen}").SendAsync("ReceiveMessage", new
                {
                    message.Id,
                    message.IdCuocTroChuyen,
                    message.IdNguoiGui,
                    message.NoiDung,
                    message.ThoiGianGui,
                    message.DaXem
                });
            }
            catch
            {
                // Ignore broadcast errors to avoid breaking API response
            }

            return Ok(new { statusCode = 200, message = "Gửi tin nhắn thành công", data = message.Id });
        }

        public class SendMessageRequest
        {
            public int IdCuocTroChuyen { get; set; }
            public long? IdNguoiGui { get; set; } // nếu null sẽ lấy theo user hiện tại
            public string NoiDung { get; set; } = string.Empty;
        }
    }
}

