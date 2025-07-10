using DoAnChuyenNganh.data;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Drawing;
using PagedList;
using System.Security.Policy;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing.Printing;

namespace DoAnChuyenNganh.Controllers
{
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Statistics()
        {
            // Lấy ngày bắt đầu và ngày kết thúc của tháng hiện tại
            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

            // Tổng người dùng
            int totalUsers = _context.Users.Count();

            // Người dùng mới hôm nay
            int newUsersToday = _context.Users.Count(u => u.CreatedAt.Date == DateTime.Now.Date);

            // Tổng doanh thu
            decimal totalRevenueOverall = _context.Orders.Sum(o => o.TotalAmount);

            // Doanh thu tháng hiện tại
            decimal totalRevenueThisMonth = _context.Orders
                .Where(o => o.OrderDate >= currentMonthStart && o.OrderDate <= currentMonthEnd)
                .Sum(o => o.TotalAmount);

            // Lấy đơn hàng theo tuần
            var ordersByWeek = _context.Orders
                .Where(o => o.OrderDate >= currentMonthStart && o.OrderDate <= currentMonthEnd)
                .AsEnumerable()
                .GroupBy(o => new
                {
                    Week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                        o.OrderDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                })
                .Select(g => new OrderByWeek
                {
                    Week = g.Key.Week,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Truyền vào ViewModel
            var statisticsViewModel = new StatisticsViewModel
            {
                TotalUsers = totalUsers,
                NewUsersToday = newUsersToday,
                TotalRevenueOverall = totalRevenueOverall,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                OrdersByWeek = ordersByWeek
            };

            return View(statisticsViewModel);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, string status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderID == orderId);
            if (order != null)
            {
                order.Status = status;
                _context.SaveChanges();
                TempData["MessageOrder"] = "Cập nhật trạng thái đơn hàng thành công!";
            }
            else
            {
                TempData["MessageOrder"] = "Không tìm thấy đơn hàng!";
            }

            return RedirectToAction("ListOrder");
        }

        public IActionResult ListCategory()
        {
            var categories = _context.Categories.ToList(); // Fetch all categories from the database
            return View(categories);  // Pass the list of categories to the view
        }
        public IActionResult DiscountCode(int page = 1, int pageSize = 10)
        {
            var discountCodes = _context.DiscountCodes
                .OrderBy(d => d.DiscountCodeID)
                .ToPagedList(page, pageSize); // Sử dụng PagedList để phân trang

            return View(discountCodes);
        }

        // POST: CreateDiscount (Tạo mã giảm giá mới)
        [HttpPost]
        public IActionResult CreateDiscount(DiscountCode discountCode)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem mã giảm giá đã tồn tại chưa
                bool isDuplicate = _context.DiscountCodes.Any(dc => dc.Code == discountCode.Code);

                if (isDuplicate)
                {
                    // Nếu mã giảm giá đã tồn tại, thêm lỗi vào ModelState
                    ModelState.AddModelError("Code", "Mã giảm giá đã tồn tại. Vui lòng nhập mã khác.");

                    // Trả về view với danh sách mã giảm giá và thông báo lỗi
                    var discountCodes = _context.DiscountCodes.OrderBy(d => d.DiscountCodeID).ToPagedList(1, 10); // Phân trang lại nếu cần
                    return View("DiscountCode", discountCodes);
                }

                // Nếu không có mã trùng lặp, thêm mã giảm giá vào cơ sở dữ liệu
                discountCode.IsUsed = false; // Mã giảm giá chưa được sử dụng
                _context.DiscountCodes.Add(discountCode);
                _context.SaveChanges();

                // Thông báo thành công
                TempData["SuccessMessage"] = "Mã giảm giá đã được tạo thành công.";
                return RedirectToAction("DiscountCode");
            }

            var discountCodesList = _context.DiscountCodes.OrderBy(d => d.DiscountCodeID).ToPagedList(1, 10); 
            return View("DiscountCode", discountCodesList); 
        }

        [HttpPost]
        public JsonResult AddCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false });
            }

            var newCategory = new Category { CategoryName = categoryName };
            _context.Categories.Add(newCategory);
            _context.SaveChanges();

            return Json(new { success = true, newCategoryID = newCategory.CategoryID });
        }

        [HttpPost]
        public JsonResult EditCategory(int categoryId, string categoryName)
        {
            var existingCategory = _context.Categories.Find(categoryId);
            if (existingCategory == null)
            {
                return Json(new { success = false });
            }

            existingCategory.CategoryName = categoryName;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Tìm danh mục cần xóa
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessageCategory"] = "Danh mục không tồn tại.";
                return RedirectToAction("ListCategory");
            }

            // Tìm và xóa các sản phẩm liên quan đến danh mục
            var products = await _context.Products.Where(p => p.CategoryID == id).ToListAsync();
            if (products.Any())
            {
                foreach (var product in products)
                {
                    // Xóa dữ liệu liên quan đến ProductVariants
                    var productVariants = await _context.ProductVariants.Where(v => v.ProductID == product.ProductID).ToListAsync();
                    if (productVariants.Any())
                    {
                        _context.ProductVariants.RemoveRange(productVariants);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa dữ liệu liên quan trong bảng Reviews
                    var reviews = await _context.Reviews.Where(r => r.ProductID == product.ProductID).ToListAsync();
                    if (reviews.Any())
                    {
                        _context.Reviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa dữ liệu liên quan trong bảng CartItems
                    var cartItems = await _context.CartItems.Where(c => c.ProductID == product.ProductID).ToListAsync();
                    if (cartItems.Any())
                    {
                        _context.CartItems.RemoveRange(cartItems);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa dữ liệu liên quan trong bảng Wishlist
                    var wishlists = await _context.Wishlists.Where(w => w.ProductID == product.ProductID).ToListAsync();
                    if (wishlists.Any())
                    {
                        _context.Wishlists.RemoveRange(wishlists);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa sản phẩm
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                }
            }

            // Xóa danh mục
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessageCategory"] = "Danh mục đã được xóa thành công.";
            return RedirectToAction("ListCategory");
        }
        [HttpPost]
        public async Task<IActionResult> SearchFlashSale(string searchKeyword, int page = 1, int pageSize = 6)
        {
            // Lấy danh sách Flash Sale và thông tin sản phẩm tương ứng
            IQueryable<FlashSale> flashSalesQuery = _context.FlashSales.Include(fs => fs.Product);

            // Dưới đây là logic lọc dữ liệu dựa vào từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                // Nếu tìm kiếm theo ID sản phẩm
                if (int.TryParse(searchKeyword, out int productId))
                {
                    flashSalesQuery = flashSalesQuery.Where(fs => fs.ProductId == productId);
                }
                else
                {
                    // Nếu tìm kiếm theo tên sản phẩm
                    flashSalesQuery = flashSalesQuery.Where(fs => fs.Product != null && fs.Product.Name.Contains(searchKeyword));
                }
            }

            // Thêm dữ liệu đã lọc
            var flashSales = await flashSalesQuery.ToListAsync();

            // Map dữ liệu sang ViewModel để hiển thị
            var viewModel = flashSales.Select(fs => new FlashSaleListViewModel
            {
                FlashSaleId = fs.Id,
                ProductName = fs.Product?.Name ?? "N/A",
                ProductImage = fs.Product?.Image1 ?? "/images/default.png",
                SalePrice = fs.Price,
                StartTime = fs.StartTime,
                EndTime = fs.EndTime
            }).ToList();

            // Thêm phân trang
            var pagedList = viewModel.ToPagedList(page, pageSize);

            return View("FlashSaleList", pagedList);
        }
        [HttpGet]
        public async Task<IActionResult> GetFlashSaleSearchSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<string>());

            var suggestions = await _context.FlashSales
                                             .Include(fs => fs.Product)
                                             .Where(fs => fs.Product != null && fs.Product.Name.Contains(keyword))
                                             .Select(fs => fs.Product.Name)
                                             .Distinct()
                                             .ToListAsync();

            return Json(suggestions);
        }

        [HttpGet]
        public async Task<IActionResult> FlashSaleList(int page = 1, int pageSize = 6)
        {
            // Lấy danh sách Flash Sale và thông tin sản phẩm tương ứng
            var flashSales = await _context.FlashSales
                .Include(fs => fs.Product) // Dùng Include để lấy thông tin sản phẩm từ Flash Sale
                .ToListAsync();

            // Map dữ liệu sang ViewModel để hiển thị
            var viewModel = flashSales.Select(fs => new FlashSaleListViewModel
            {
                FlashSaleId = fs.Id,
                ProductName = fs.Product?.Name ?? "N/A",
                ProductImage = fs.Product?.Image1 ?? "/images/default.png",
                SalePrice = fs.Price,
                StartTime = fs.StartTime,
                EndTime = fs.EndTime
            }).ToList();

            // Dùng PagedList để phân trang
            var pagedList = viewModel.ToPagedList(page, pageSize);

            return View(pagedList);
        }
        [HttpGet]
        public async Task<IActionResult> CreateFlashSale()
        {
            // Lấy danh sách ID các sản phẩm đã có trong Flash Sale
            var existingFlashSales = await _context.FlashSales
                .Select(fs => fs.ProductId)
                .ToListAsync();

            // Lấy danh sách sản phẩm chưa có trong Flash Sale
            var availableProducts = await _context.Products
                .Where(p => !existingFlashSales.Contains(p.ProductID)) // Loại trừ sản phẩm đã có trong Flash Sale
                .Select(p => new SelectListItem
                {
                    Value = p.ProductID.ToString(),
                    Text = $"{p.Name} - Giá: {(p.Price.HasValue ? p.Price.Value.ToString("N0") : "0")} VNĐ -Giá Khuyến Mãi: {(p.SalePrice.HasValue ? p.SalePrice.Value.ToString("N0") : "0")} VND" 

                })
                .ToListAsync();

            var viewModel = new FlashSaleViewModel
            {
                Products = availableProducts
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> CreateFlashSale(FlashSaleViewModel viewModel)
        {
            // Tải danh sách sản phẩm ngay từ đầu
            viewModel.Products = await _context.Products.Select(p => new SelectListItem
            {
                Value = p.ProductID.ToString(),
                Text = $"{p.Name} - Giá: {p.Price}"
            }).ToListAsync();

            if (!ModelState.IsValid)
            {
                TempData["ModelStateErrors"] = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return View(viewModel);
            }
            if (viewModel.EndTime <= viewModel.StartTime)
            {
                TempData["ErrorMessage"] = "Ngày kết thúc không được nhỏ hơn hoặc bằng ngày bắt đầu.";
                return View(viewModel);
            }
            // Kiểm tra xem sản phẩm đã tồn tại trong Flash Sale chưa
            var isProductInFlashSale = await _context.FlashSales
                .AnyAsync(fs => fs.ProductId == viewModel.ProductId);

            if (isProductInFlashSale)
            {
                TempData["ErrorMessage"] = "Sản phẩm đã tồn tại trong Flash Sale và không thể thêm lại.";
                return View(viewModel);
            }

            // Thêm dữ liệu mới vào Flash Sale
            var flashSale = new FlashSale
            {
                ProductId = viewModel.ProductId,
                StartTime = viewModel.StartTime,
                EndTime = viewModel.EndTime,
                Price = viewModel.Price,
            };

            _context.FlashSales.Add(flashSale);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == viewModel.ProductId);
            if (product != null)
            {
                product.SalePrice = viewModel.Price;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Flash Sale đã được thêm thành công và cập nhật giá khuyến mãi cho sản phẩm!";
            return RedirectToAction("FlashSaleList");
        }



        public async Task<IActionResult> ManageContactInfo()
        {
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            return View(contactInfo);
        }

        // Lưu thay đổi thông tin liên hệ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageContactInfo([Bind("Id,Name,Address,PhoneNumber,Email,WorkingHours,MapUrl")] ContactInfo contactInfo)
        {
            if (ModelState.IsValid)
            {
                // Truy vấn bản ghi cần cập nhật từ cơ sở dữ liệu
                var existingContactInfo = await _context.ContactInfos.FindAsync(contactInfo.Id);

                if (existingContactInfo == null)
                {
                    return NotFound(); // Nếu không tìm thấy bản ghi cần cập nhật
                }

                // Cập nhật thông tin trong bản ghi hiện tại
                existingContactInfo.Name = contactInfo.Name;
                existingContactInfo.Address = contactInfo.Address;
                existingContactInfo.PhoneNumber = contactInfo.PhoneNumber;
                existingContactInfo.Email = contactInfo.Email;
                existingContactInfo.WorkingHours = contactInfo.WorkingHours;
                existingContactInfo.MapUrl = contactInfo.MapUrl;

                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Sau khi cập nhật, chuyển hướng lại trang thông tin liên hệ
                return RedirectToAction(nameof(ManageContactInfo));
            }

            // Nếu có lỗi trong form, hiển thị lại form cùng với thông báo lỗi
            return View(contactInfo);
        }
        public async Task<IActionResult> ManageHomeContent()
        {
            var homeContent = await _context.HomeContents.FirstOrDefaultAsync();
            return View(homeContent);
        }

        // Lưu thay đổi nội dung trang chủ
               [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageHomeContent([Bind("Id,Notification")] HomeContent homeContent,
            IFormFile? image1, IFormFile? image2, IFormFile? image3, IFormFile? image4, IFormFile? image5, IFormFile? image6)
        {
            // Kiểm tra xem Id có hợp lệ hay không
            if (homeContent.Id == 0)
            {
                // Trả về lỗi nếu Id không hợp lệ
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Lấy thông tin trang chủ hiện tại từ cơ sở dữ liệu
                var existingContent = await _context.HomeContents.FindAsync(homeContent.Id);

                if (existingContent == null)
                {
                    return NotFound();
                }

                // Cập nhật nội dung thông báo
                existingContent.Notification = homeContent.Notification;

                // Xử lý và lưu ảnh nếu có, giữ ảnh cũ nếu không có ảnh mới
                existingContent.Image1 = image1 != null ? ProcessUpload(image1) : existingContent.Image1;
                existingContent.Image2 = image2 != null ? ProcessUpload(image2) : existingContent.Image2;
                existingContent.Image3 = image3 != null ? ProcessUpload(image3) : existingContent.Image3;
                existingContent.Image4 = image4 != null ? ProcessUpload(image4) : existingContent.Image4;
                existingContent.Image5 = image5 != null ? ProcessUpload(image5) : existingContent.Image5;
                existingContent.Image6 = image6 != null ? ProcessUpload(image6) : existingContent.Image6;
                _context.Update(existingContent);
                // Lưu các thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Quay lại trang quản lý nội dung trang chủ
                return RedirectToAction(nameof(ManageHomeContent));
            }

            // Nếu model không hợp lệ, trả về view để hiển thị lỗi
            return View(homeContent);
        }

        public IActionResult ListBrand()
        {
            var brands = _context.Brands.ToList(); // Fetch all brands from the database
            return View(brands);  // Pass the list of brands to the view
        }

        [HttpPost]
        public JsonResult AddBrand(string brandName)
        {
            if (string.IsNullOrWhiteSpace(brandName))
            {
                return Json(new { success = false });
            }

            var newBrand = new Brand { BrandName = brandName };
            _context.Brands.Add(newBrand);
            _context.SaveChanges();

            return Json(new { success = true, newBrandID = newBrand.BrandID });
        }

        [HttpPost]
        public JsonResult EditBrand(int brandID, string brandName)
        {
            var existingBrand = _context.Brands.Find(brandID);
            if (existingBrand == null)
            {
                return Json(new { success = false });
            }

            existingBrand.BrandName = brandName;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet] // Đổi từ HttpPost thành HttpGet để phù hợp với liên kết
        public IActionResult DeleteBrand(int id)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null)
            {
                TempData["ErrorMessageBrand"] = "Kích thước không tồn tại.";
                return RedirectToAction("ListBrand"); // Điều hướng về trang danh sách
            }

            try
            {
                _context.Brands.Remove(brand);
                _context.SaveChanges();
                TempData["SuccessMessageBrand"] = "Xóa kích thước thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessageBrand"] = "Đã xảy ra lỗi khi xóa kích thước: " + ex.Message;
            }

            return RedirectToAction("ListBrand");
        }

        public IActionResult ListSize()
        {
            var sizes = _context.Sizes.ToList(); // Fetch all sizes from the database
            return View(sizes);  // Pass the list of sizes to the view
        }

        [HttpPost]
        public JsonResult AddSize(string size)
        {
            if (string.IsNullOrWhiteSpace(size))
            {
                return Json(new { success = false });
            }

            var newSize = new Sizes { Size = size };
            _context.Sizes.Add(newSize);
            _context.SaveChanges();

            return Json(new { success = true, newSizeID = newSize.SizeID });
        }

        [HttpPost]
        public JsonResult EditSize(int sizeID, string size)
        {
            var existingSize = _context.Sizes.Find(sizeID);
            if (existingSize == null)
            {
                return Json(new { success = false });
            }

            existingSize.Size = size;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet] // Đổi từ HttpPost thành HttpGet để phù hợp với liên kết
        public IActionResult DeleteSize(int id)
        {
            var size = _context.Sizes.Find(id);
            if (size == null)
            {
                TempData["ErrorMessageSize"] = "Kích thước không tồn tại.";
                return RedirectToAction("Index"); // Điều hướng về trang danh sách
            }

            try
            {
                _context.Sizes.Remove(size);
                _context.SaveChanges();
                TempData["SuccessMessageSize"] = "Xóa kích thước thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessageSize"] = "Đã xảy ra lỗi khi xóa kích thước: " + ex.Message;
            }

            return RedirectToAction("ListSize");
        }


        public IActionResult ListColor()
        {
            var colors = _context.Colors.ToList(); // Fetch all colors from the database
            return View(colors);  // Pass the list of colors to the view
        }
        [HttpPost]
        public JsonResult AddColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return Json(new { success = false });
            }

            var newColor = new Colors { Color = color };
            _context.Colors.Add(newColor);
            _context.SaveChanges();

            return Json(new { success = true, newColorID = newColor.ColorID });
        }

        [HttpPost]
        public IActionResult EditColor(int ColorID, string Color)
        {
            var color = _context.Colors.Find(ColorID);
            if (color == null)
            {
                return Json(new { success = false, message = "Màu không tồn tại" });
            }

            color.Color = Color;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // AJAX Delete

        [HttpGet] // Đổi từ HttpPost thành HttpGet để phù hợp với liên kết
        public IActionResult DeleteColor(int id)
        {
            var color = _context.Colors.Find(id);
            if (color == null)
            {
                TempData["ErrorMessageColor"] = "Kích thước không tồn tại.";
                return RedirectToAction("ListColor"); // Điều hướng về trang danh sách
            }

            try
            {
                _context.Colors.Remove(color);
                _context.SaveChanges();
                TempData["SuccessMessageColor"] = "Xóa kích thước thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessageColor"] = "Đã xảy ra lỗi khi xóa kích thước: " + ex.Message;
            }

            return RedirectToAction("ListColor");
        }
        public async Task<IActionResult> ListUser(int page = 1, int pageSize = 9)
        {
            // Lấy tất cả người dùng
            var users = await _context.Users.ToListAsync();

            // Sắp xếp theo Role ưu tiên và ngày tạo
            var sortedUsers = users
                .OrderByDescending(user => user.Role == "Admin" ? 3 : user.Role == "employee" ? 2 : 1) // Sắp xếp Role
                .ThenByDescending(user => user.CreatedAt) // Sắp xếp ngày tạo giảm dần
                .ToList();

            // Tạo danh sách ListUserViewModel để chứa người dùng, trạng thái đã mua, và quyền
            var userViewModels = sortedUsers.Select(user => new ListUserViewModel
            {
                user = user,
                HasPurchased = _context.Orders.Count(o => o.UserID == user.UserID) > 0
                    ? $"Đã mua {_context.Orders.Count(o => o.UserID == user.UserID)} lần"
                    : "Chưa mua",
                Role = user.Role switch
                {
                    "admin" => "Admin",
                    "employee" => "Nhân viên",
                    _ => "Khách hàng"
                } // Gán tên hiển thị cho Role
            }).ToList();

            // Thực hiện phân trang
            var pagedList = userViewModels.ToPagedList(page, pageSize);

            // Truyền danh sách phân trang vào View
            return View(pagedList);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(int userId, string role)
        {
            // Validate the role against the allowed values: 'customer', 'employee', 'admin'
            if (string.IsNullOrEmpty(role) || !(role == "customer" || role == "employee" || role == "Admin"))
            {
                TempData["ErrorMessage"] = "Quyền không hợp lệ.";
                return RedirectToAction("ListUser");
            }

            // Get the current logged-in user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming user ID is stored in a claim
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction("ListUser");
            }

            // Check if the admin is trying to edit their own role
            if (userId.ToString() == currentUserId && role == "Admin")
            {
                TempData["ErrorMessage"] = "Admin không thể tự chỉnh sửa quyền của chính mình.";
                return RedirectToAction("ListUser");
            }

            // Update the user's role
            user.Role = role;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật quyền thành công!";
            return RedirectToAction("ListUser");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction("ListUser");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa người dùng thành công.";
            return RedirectToAction("ListUser");
        }

        public async Task<IActionResult> ListOrder(int page = 1, int pageSize = 6, string searchKeyword = null)
        {
            // Lấy danh sách đơn hàng từ cơ sở dữ liệu, bao gồm chi tiết đơn hàng
            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.OrderDetails);

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                ordersQuery = ordersQuery.Where(o => o.FullName.Contains(searchKeyword));
            }

            // Sắp xếp theo ngày đặt hàng (OrderDate) giảm dần
            ordersQuery = ordersQuery.OrderByDescending(o => o.OrderDate);

            // Lấy dữ liệu
            var orders = await ordersQuery.ToListAsync();

            // Lấy thông tin người dùng để hiển thị tên khách hàng
            var users = await _context.Users.ToListAsync();

            // Xây dựng ViewModel
            var orderViewModels = orders.Select(order => new OrderAdminViewModel
            {
                Order = order,
                UserFullName = users.FirstOrDefault(u => u.UserID == order.UserID)?.FullName,
                OrderDetails = order.OrderDetails.ToList()
            }).ToList();

            // Phân trang
            var pagedList = orderViewModels.ToPagedList(page, pageSize);

            return View(pagedList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscount(int discountId)
        {
            // Tìm Discount cần xóa
            var discount = await _context.DiscountCodes.FindAsync(discountId);
            if (discount == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá cần xóa.";
                return RedirectToAction("DiscountCode");
            }

            // Xóa dữ liệu liên quan trong bảng UserDiscounts
            var userDiscounts = await _context.UserDiscounts.Where(ud => ud.DiscountCodeID == discountId).ToListAsync();
            if (userDiscounts.Any())
            {
                _context.UserDiscounts.RemoveRange(userDiscounts);
                await _context.SaveChangesAsync(); // Lưu thay đổi trước khi tiếp tục xóa Discount
            }

            // Nếu có các bảng liên quan khác, thêm logic xóa ở đây...

            // Xóa Discount
            _context.DiscountCodes.Remove(discount);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa mã giảm giá thành công.";
            return RedirectToAction("DiscountCode");
        }


        [HttpPost]
        public async Task<IActionResult> SearchOrder(string searchKeyword, int page = 1, int pageSize = 6)
        {
            // Retrieve orders from the database
            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.OrderDetails);

            // Filter by search keyword (full name or order ID)
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                // Check if the searchKeyword is a valid integer (order ID)
                if (int.TryParse(searchKeyword, out int orderId))
                {
                    // Filter by Order ID
                    ordersQuery = ordersQuery.Where(o => o.OrderID == orderId);
                }
                else
                {
                    // Filter by Full Name
                    ordersQuery = ordersQuery.Where(o => o.FullName.Contains(searchKeyword));
                }
            }

            // Retrieve the filtered orders
            var orders = await ordersQuery.ToListAsync();

            // Retrieve users for customer names
            var users = await _context.Users.ToListAsync();

            // Build the view model
            var orderViewModels = orders.Select(order => new OrderAdminViewModel
            {
                Order = order,
                UserFullName = users.FirstOrDefault(u => u.UserID == order.UserID)?.FullName,
                OrderDetails = order.OrderDetails.ToList()
            }).ToList();

            // Pagination
            var pagedList = orderViewModels.ToPagedList(page, pageSize);

            // Return to ListOrder view with filtered results
            return View("ListOrder", pagedList);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderSearchSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<string>());

            var suggestions = await _context.Orders
                                             .Where(o => o.FullName.Contains(keyword))
                                             .Select(o => o.FullName) // Assuming you want to suggest customer names
                                             .ToListAsync();

            return Json(suggestions);
        }

        [HttpGet]
        public async Task<IActionResult> SearchByOrderStatus(string status, int page = 1, int pageSize = 6)
        {
            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.OrderDetails);

            if (!string.IsNullOrEmpty(status) && status != "Chọn trạng thái")
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = await ordersQuery.ToListAsync();

            if (!orders.Any())
            {
                TempData["MessageOrder"] = "Không tìm thấy đơn hàng nào với trạng thái đã chọn.";
                return RedirectToAction("ListOrder", "Admin");
            }

            var users = await _context.Users.ToListAsync();

            var orderViewModels = orders.Select(order => new OrderAdminViewModel
            {
                Order = order,
                UserFullName = users.FirstOrDefault(u => u.UserID == order.UserID)?.FullName,
                OrderDetails = order.OrderDetails.ToList()
            }).ToList();

            var pagedList = orderViewModels.ToPagedList(page, pageSize);

            return View("ListOrder", pagedList); // Return to ListOrder view with filtered results
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 6, string searchKeyword = null)
        {
            IQueryable<Product> productsQuery = _context.Products;

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchKeyword));
            }

            // Sắp xếp theo ProductID giảm dần để sản phẩm mới nhất hiển thị đầu tiên
            productsQuery = productsQuery.OrderByDescending(p => p.ProductID);

            // Lấy danh sách sản phẩm
            var products = await productsQuery.ToListAsync();
            var productVariants = await _context.ProductVariants.ToListAsync();
            var brands = await _context.Brands.ToListAsync();
            var colors = await _context.Colors.ToListAsync();
            var sizes = await _context.Sizes.ToListAsync();

            // Tạo danh sách ViewModel
            var productViewModels = products.Select(product => new ProductAdminViewModel
            {
                Product = product,
                ProductVariants = productVariants
                    .Where(v => v.ProductID == product.ProductID)
                    .Select(variant => new ProductVariantViewModel
                    {
                        ProductVariant = variant,
                        ColorName = colors.FirstOrDefault(c => c.ColorID == variant.ColorID)?.Color,
                        SizeName = sizes.FirstOrDefault(s => s.SizeID == variant.SizeID)?.Size
                    }).ToList(),
                BrandName = brands.FirstOrDefault(b => b.BrandID == product.BrandID)?.BrandName
            }).ToList();

            // Truyền dữ liệu vào ViewBag nếu cần
            ViewBag.Colors = _context.Colors.ToList();
            ViewBag.Sizes = _context.Sizes.ToList();

            // Phân trang danh sách sản phẩm
            var pagedList = productViewModels.ToPagedList(page, pageSize);

            return View(pagedList);
        }
        [HttpPost]
        public async Task<IActionResult> SearchProduct(string searchKeyword, int page = 1, int pageSize = 6)
        {
            // Lấy tất cả sản phẩm
            var products = await _context.Products
                                         .Where(p => p.Name.Contains(searchKeyword))
                                         .ToListAsync();

            // Nếu không tìm thấy sản phẩm, lấy toàn bộ danh sách sản phẩm
            if (!products.Any())
            {
                TempData["MessageProduct"] = "Không tìm thấy sản phẩm nào với từ khóa bạn đã nhập.";
                return RedirectToAction("Index", "Admin");
            }

            var productVariants = await _context.ProductVariants.ToListAsync();
            var brands = await _context.Brands.ToListAsync();
            var colors = await _context.Colors.ToListAsync();
            var sizes = await _context.Sizes.ToListAsync();

            // Tạo danh sách ViewModel cho sản phẩm tìm kiếm
            var productViewModels = products.Select(product => new ProductAdminViewModel
            {
                Product = product,
                ProductVariants = productVariants
                    .Where(v => v.ProductID == product.ProductID)
                    .Select(variant => new ProductVariantViewModel
                    {
                        ProductVariant = variant,
                        ColorName = colors.FirstOrDefault(c => c.ColorID == variant.ColorID)?.Color,
                        SizeName = sizes.FirstOrDefault(s => s.SizeID == variant.SizeID)?.Size
                    }).ToList(),
                BrandName = brands.FirstOrDefault(b => b.BrandID == product.BrandID)?.BrandName
            }).ToList();

            ViewBag.Colors = _context.Colors.ToList();
            ViewBag.Sizes = _context.Sizes.ToList();
            var pagedList = productViewModels.ToPagedList(page, pageSize);

            return View("Index", pagedList); // Return to Index view with filtered results
        }
        [HttpGet]
        public async Task<IActionResult> SearchByStatus(string status, int page = 1, int pageSize = 6)
        {
            // Lấy danh sách sản phẩm theo trạng thái
            IQueryable<Product> productsQuery = _context.Products;

            if (!string.IsNullOrEmpty(status) && status != "Chọn trạng thái")
            {
                productsQuery = productsQuery.Where(p => p.Status == status);
            }

            var products = await productsQuery.ToListAsync();

            // Kiểm tra nếu không tìm thấy sản phẩm
            if (!products.Any())
            {
                TempData["MessageProduct"] = "Không tìm thấy sản phẩm nào với trạng thái đã chọn.";
                return RedirectToAction("Index", "Admin");
            }

            var productVariants = await _context.ProductVariants.ToListAsync();
            var brands = await _context.Brands.ToListAsync();
            var colors = await _context.Colors.ToListAsync();
            var sizes = await _context.Sizes.ToListAsync();

            // Tạo danh sách ViewModel cho sản phẩm tìm kiếm
            var productViewModels = products.Select(product => new ProductAdminViewModel
            {
                Product = product,
                ProductVariants = productVariants
                    .Where(v => v.ProductID == product.ProductID)
                    .Select(variant => new ProductVariantViewModel
                    {
                        ProductVariant = variant,
                        ColorName = colors.FirstOrDefault(c => c.ColorID == variant.ColorID)?.Color,
                        SizeName = sizes.FirstOrDefault(s => s.SizeID == variant.SizeID)?.Size
                    }).ToList(),
                BrandName = brands.FirstOrDefault(b => b.BrandID == product.BrandID)?.BrandName
            }).ToList();

            ViewBag.Colors = _context.Colors.ToList();
            ViewBag.Sizes = _context.Sizes.ToList();
            var pagedList = productViewModels.ToPagedList(page, pageSize);

            return View("Index", pagedList); // Return to Index view with filtered results
        }


        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<string>());

            var suggestions = await _context.Products
                                             .Where(p => p.Name.Contains(keyword))
                                             .Select(p => p.Name) // Assuming you want to suggest product names
                                             .ToListAsync();

            return Json(suggestions);
        }

        public IActionResult Create()
        {
            // Fetch categories and brands from the database
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,Name,Description,Price,CategoryID,BrandID,Status,SalePrice")] Product product,
                 IFormFile? image1, IFormFile? image2, IFormFile? image3)
        {
            if (ModelState.IsValid)
            {
                // Handle image uploads
                product.Image1 = ProcessUpload(image1);
                product.Image2 = ProcessUpload(image2);
                product.Image3 = ProcessUpload(image3);

                // Add product to the Products table
                _context.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Repopulate categories and brands if model state is invalid
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View(product);
        }
        // Hàm xử lý thêm biến thể (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(int productId, ProductVariant variant)
        {
            // Gán ProductID cho biến thể
            variant.ProductID = productId;

            if (ModelState.IsValid)
            {
                // Thêm biến thể vào bảng ProductVariants
                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, gửi lại thông tin
            ViewBag.ProductID = productId; // Gửi ProductID đến view nếu có lỗi
            return View(variant);
        }


        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm từ cơ sở dữ liệu dựa trên ProductID
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách các Category và Brand để hiển thị trong dropdown
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View(product);
        }



        // POST: Product/Edit/5
        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,Name,Description,Price,SalePrice,CategoryID,BrandID,Status")] Product product,
            IFormFile? image1, IFormFile? image2, IFormFile? image3)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing product from the database to retain its current images
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Check if a new image is provided for image1, otherwise retain the old image
                    if (image1 != null)
                    {
                        product.Image1 = ProcessUpload(image1);
                    }
                    else
                    {
                        product.Image1 = existingProduct.Image1; // Keep the existing image
                    }

                    // Check if a new image is provided for image2, otherwise retain the old image
                    if (image2 != null)
                    {
                        product.Image2 = ProcessUpload(image2);
                    }
                    else
                    {
                        product.Image2 = existingProduct.Image2; // Keep the existing image
                    }

                    // Check if a new image is provided for image3, otherwise retain the old image
                    if (image3 != null)
                    {
                        product.Image3 = ProcessUpload(image3);
                    }
                    else
                    {
                        product.Image3 = existingProduct.Image3; // Keep the existing image
                    }

                    // Update the product information
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    var cartItems = _context.CartItems.Where(ci => ci.ProductID == id).ToList();

                    foreach (var cartItem in cartItems)
                    {
                        // Update the price and sale price in the cart items
                        cartItem.Price = product.Price;
                        cartItem.SalePrice = product.SalePrice;

                        // Update the TotalPrice based on SalePrice if available, otherwise use Price
                        cartItem.TotalPrice = cartItem.Quantity * (cartItem.SalePrice ?? cartItem.Price);

                        // Mark the CartItem entity as modified
                        _context.CartItems.Update(cartItem);
                    }

                    // Save changes to CartItems
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();
            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVariant(int id)
        {
            // Tìm biến thể theo ID
            var variant = _context.ProductVariants.Find(id);

            if (variant == null)
            {
                return NotFound(); // Nếu không tìm thấy biến thể, trả về lỗi 404
            }

            // Xóa biến thể khỏi cơ sở dữ liệu
            _context.ProductVariants.Remove(variant);
            _context.SaveChanges(); // Lưu thay đổi

            // Thông báo thành công và điều hướng về trang sản phẩm
            TempData["SuccessMessage"] = "Biến thể đã được xóa thành công.";
            return RedirectToAction("Index"); // Điều hướng lại trang Index (danh sách sản phẩm)
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Tìm sản phẩm cần xóa
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Xóa dữ liệu liên quan trong bảng Reviews
            var reviews = await _context.Reviews.Where(r => r.ProductID == id).ToListAsync();
            if (reviews.Any())
            {
                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync();  // Lưu thay đổi trước khi xóa sản phẩm
            }

            // Xóa dữ liệu liên quan trong bảng CartItem

            // Xóa dữ liệu liên quan trong bảng ProductVariants
            var productVariants = await _context.ProductVariants.Where(v => v.ProductID == id).ToListAsync();
            if (productVariants.Any())
            {
                _context.ProductVariants.RemoveRange(productVariants);
                await _context.SaveChangesAsync();
            }

            // Xóa dữ liệu liên quan trong bảng CartItems
            var cartItems = await _context.CartItems.Where(c => c.ProductID == id).ToListAsync();
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            // Xóa dữ liệu liên quan trong bảng Wishlist
            var wishlists = await _context.Wishlists.Where(w => w.ProductID == id).ToListAsync();
            if (wishlists.Any())
            {
                _context.Wishlists.RemoveRange(wishlists);
                await _context.SaveChangesAsync();
            }
            // Xóa sản phẩm
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra sản phẩm có tồn tại không
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }

        private string ProcessUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "";
            }
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return "/images/" + uniqueFileName;
        }
    }
}
