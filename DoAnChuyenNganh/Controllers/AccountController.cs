using DoAnChuyenNganh.data;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using DoAnChuyenNganh.ViewModel;

namespace DoAnChuyenNganh.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

    
        public ActionResult Send()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Send(Gmail gmail)
        {
            if (!IsGmailAddress(gmail.To))
            {
                TempData["ErrorMessage"] = "Địa chỉ email không phải là địa chỉ Gmail hợp lệ.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == gmail.To);

            if (user != null)
            {
                string token = Guid.NewGuid().ToString();
                PasswordResetToken resetToken = new PasswordResetToken
                {
                    Email = gmail.To,
                    Token = token,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(resetToken);
                _context.SaveChanges();

                string resetUrl = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);
                gmail.SendMail($"Vui lòng nhấp vào liên kết sau để đặt lại mật khẩu của bạn: {resetUrl}");

                TempData["SuccessMessage"] = "Đường dẫn khôi phục mật khẩu đã được gửi đến email của bạn!";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                TempData["ErrorMessage"] = "Địa chỉ email không tồn tại trong hệ thống.";
                return View();
            }
        }

        public ActionResult ResetPassword(string token)
        {
            var resetToken = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token);
            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "Token không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Login", "Account");
            }

            return View(new ResetPasswordModel { Token = token });
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var resetToken = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == model.Token);
                if (resetToken == null)
                {
                    TempData["ErrorMessage"] = "Token không hợp lệ hoặc đã hết hạn.";
                    return RedirectToAction("Login", "Account");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == resetToken.Email);
                if (user != null)
                {
                    user.Password = model.NewPassword; // Hoặc mã hóa nếu cần
                    _context.SaveChanges();

                    _context.PasswordResetTokens.Remove(resetToken); // Xóa token
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công!";
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(model);
        }

        private bool IsGmailAddress(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false; // Trả về false nếu email rỗng
            }

            string pattern = @"^[a-zA-Z0-9_.+-]+@gmail\.com$";
            return System.Text.RegularExpressions.Regex.IsMatch(email, pattern);
        }

        private string GenerateRandomPassword()
        {
            // Tạo mật khẩu ngẫu nhiên (ví dụ: 6 ký tự số)
            Random random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [HttpGet]
        public IActionResult Login()
        {
            var error = Request.Query["error"].ToString(); // Đặt dòng này ở đầu
            if (error == "facebook_error")
            {
                ViewBag.ErrorMessage = "Đăng Nhập FaceBook Thất Bại !!!";
            }
            if (error == "google_error")
            {
                ViewBag.ErrorMessage = "Đăng Nhập Google Thất Bại !!";
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password )
        {
           
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password); // So sánh email và mật khẩu

            if (user != null)
            {
                // Tạo danh tính claims cho người dùng
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role) // Thêm vai trò của người dùng
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Đăng nhập
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chính
            }
            else
            {
                // Nếu đăng nhập thất bại, thêm thông báo lỗi
                ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(string fullName, string email, string phone, string password, string confirmPassword)
        {
            // Lưu lại thông tin đã nhập vào ViewBag
            ViewBag.FullName = fullName;
            ViewBag.Email = email;
            ViewBag.Phone = phone;


            if (password.Length < 8)
            {
                ViewBag.ErrorMessage = "Mật Khẩu Phải Trên 8 Ký Tự.";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật Khẩu Không Trùng Khớp.";
                return View();
            }

            string emailPattern = @"^[\w-\.]+@gmail\.com$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                ViewBag.EmailError = "Email phải là địa chỉ Gmail hợp lệ (ví dụ: user@gmail.com).";
                return View();
            }

            string phonePattern = @"^\d{10}$";
            if (!Regex.IsMatch(phone, phonePattern))
            {
                ViewBag.PhoneError = "Vui Lòng Nhập Số Điện Thoại Hợp Lệ";
                return View();
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                ViewBag.EmailError = "Email này đã được đăng ký.";
                return View();
            }

            var user = new Users
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                Password = password,
                Address = "1",// Chú ý: Lưu mật khẩu chưa mã hóa có thể không an toàn
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(); // Đăng xuất người dùng
            return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chính
        }
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal != null)
            {
                // Truy cập thông tin người dùng từ Google
                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                // Kiểm tra xem email đã tồn tại trong hệ thống chưa và lấy role nếu có
                var existingUser = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => new {
                        u.FullName,
                        u.Email,
                        Password = u.Password ?? "No password provided",
                        Phone = u.Phone ?? "No phone provided",
                        Address = u.Address ?? "No address provided",
                        Role = u.Role // Lấy role của người dùng
                    })
                    .FirstOrDefaultAsync();

                if (existingUser == null)
                {
                    // Nếu người dùng chưa tồn tại, tạo người dùng mới
                    var newUser = new Users
                    {
                        FullName = name,
                        Email = email,
                        Password = "1",
                        Address = "1",
                        Phone = "1",
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync(); // Lưu người dùng mới vào cơ sở dữ liệu

                    // Gán tất cả các trường để phù hợp với kiểu của existingUser
                    existingUser = new
                    {
                        FullName = newUser.FullName,
                        Email = newUser.Email,
                        Password = "No password provided",
                        Phone = "No phone provided",
                        Address = "No address provided",
                        Role = "customer" // Role mặc định cho người dùng mới
                    };
                }

                // Tạo danh tính claims cho người dùng, bao gồm role
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, existingUser.Email),
            new Claim(ClaimTypes.Name, existingUser.FullName),
            new Claim(ClaimTypes.Role, existingUser.Role) // Thêm role vào claims
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Đăng nhập người dùng
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chính
            }

            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var redirectUrl = Url.Action("FacebookResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }
        [HttpGet]
        public async Task<IActionResult> FacebookResponse(string error, string error_description)
        {
            // Kiểm tra nếu người dùng từ chối truy cập hoặc có lỗi khác
            if (!string.IsNullOrEmpty(error))
            {
                // Ghi log lỗi nếu cần thiết (tùy chọn)
                _logger.LogError($"Lỗi khi đăng nhập Facebook: {error}, Mô tả: {error_description}");

                // Gửi thông báo lỗi về trang login
                if (error == "access_denied")
                {
                    TempData["ErrorMessage"] = "Bạn đã từ chối cấp quyền cho ứng dụng. Vui lòng thử lại.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi kết nối với Facebook. Vui lòng thử lại.";
                }

                // Chuyển hướng về trang login với thông báo lỗi
                return RedirectToAction("Login", "Account");
            }

            // Nếu không có lỗi, tiến hành xử lý xác thực bình thường
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal != null)
            {
                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                var existingUser = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => new {
                        u.FullName,
                        u.Email,
                        Role = u.Role
                    })
                    .FirstOrDefaultAsync();

                if (existingUser == null)
                {
                    var newUser = new Users
                    {
                        FullName = name,
                        Email = email,
                        Role = "customer",
                        Password = "1",
                        Address="1",
                        Phone="1",
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    existingUser = new
                    {
                        FullName = newUser.FullName,
                        Email = newUser.Email,
                        Role = newUser.Role
                    };

                    TempData["Message"] = "Tài khoản mới đã được tạo thành công!";
                }
                else
                {
                    TempData["Message"] = "Đăng nhập thành công!";
                }

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, existingUser.Email),
            new Claim(ClaimTypes.Name, existingUser.FullName),
            new Claim(ClaimTypes.Role, existingUser.Role)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            // Nếu xác thực thất bại
            TempData["ErrorMessage"] = "Đăng nhập thất bại. Vui lòng thử lại.";
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Profile()
        {
            // Lấy email của người dùng hiện tại từ claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Kiểm tra nếu email không tồn tại trong claims
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(); // Trả về lỗi không được phép nếu không tìm thấy email
            }

            // Sử dụng một truy vấn cẩn thận để kiểm tra null trên các trường dữ liệu
            var user = _context.Users
                 .Where(u => u.Email == email)
                 .Select(u => new {
                     u.Email,
                     u.FullName,
                     Password = u.Password != null ? u.Password : "Không có thông tin",
                     Phone = u.Phone != null && u.Phone != "1" ? u.Phone : "Không có thông tin",
                     Address = u.Address != null && u.Address != "1" ? u.Address : "Không có thông tin"
                 })
                 .FirstOrDefault();

            // Nếu không tìm thấy người dùng
            if (user == null)
            {
                return NotFound(); // Trả về lỗi NotFound nếu không tìm thấy người dùng
            }

            var userProfile = new UserProfileViewModel
            {
                Email = user.Email,
                Name = user.FullName,
                Password = user.Password,
                Phone = user.Phone,
                Address = user.Address,
                IsPasswordSet = user.Password != null && user.Password != "1"

            };

            // Trả về view với thông tin người dùng
            return View(userProfile);
        }
        [HttpPost]
        public IActionResult SaveDefaultOrder()
        {
            // Tạo một đối tượng Order với giá trị quy định sẵn
            var newOrder = new Order
            {
                FullName = "1", // Giá trị mặc định cho tên
                PriceShip = 1, // Giá trị mặc định cho phí vận chuyển
                TotalAmount = 1, // Giá trị mặc định cho tổng số tiền
                OrderDate = DateTime.Now, // Thời gian đặt hàng hiện tại
                Status = "1", // Trạng thái đơn hàng mặc định
                PaymentMethod = "1", // Phương thức thanh toán mặc định
                ShippingAddress = "1 specified", // Địa chỉ giao hàng mặc định
                Note = "1", // Ghi chú mặc định 
            };

            // Lưu đơn hàng vào DB
            _context.Orders.Add(newOrder);
            _context.SaveChanges();

            return Ok("Order saved successfully with default values.");
        }
        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // Lấy email của người dùng hiện tại từ claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Kiểm tra nếu email không tồn tại trong claims
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Tìm người dùng trong cơ sở dữ liệu bằng email
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Nếu mật khẩu hiện tại của người dùng là "1" (tức là đăng ký qua Google)
            if (user.Password == "1")
            {
                // Kiểm tra mật khẩu mới và xác nhận mật khẩu
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu mới và xác nhận mật khẩu không khớp." });
                }

                // Cập nhật mật khẩu mới cho người dùng
                user.Password = newPassword;
                _context.SaveChanges();

                return Json(new { success = true, message = "Đổi mật khẩu thành công." });
            }

            // Nếu người dùng đã có mật khẩu khác
            if (!string.IsNullOrEmpty(currentPassword))
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại." });
                }

                // Kiểm tra mật khẩu hiện tại
                if (user.Password != currentPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác." });
                }

                // Kiểm tra mật khẩu mới và xác nhận mật khẩu
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu mới và xác nhận mật khẩu không khớp." });
                }
            }

            // Cập nhật mật khẩu mới cho người dùng
            user.Password = newPassword;
            _context.SaveChanges();

            return Json(new { success = true, message = "Đổi mật khẩu thành công." });
        }

        [HttpPost]
        public IActionResult UpdateUserInfo(string name, string email, string phone, string address)
        {
            // Lấy email của người dùng hiện tại từ claims
            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // Kiểm tra nếu email không tồn tại trong claims
            if (string.IsNullOrEmpty(currentEmail))
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Tìm người dùng trong cơ sở dữ liệu bằng email
            var user = _context.Users.FirstOrDefault(u => u.Email == currentEmail);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Cập nhật thông tin người dùng
            user.FullName = name;
            user.Email = email;
            user.Phone = phone;
            user.Address = address;

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.SaveChanges();

            return Json(new { success = true, message = "Cập nhật thông tin thành công." });
        }

    }
}
