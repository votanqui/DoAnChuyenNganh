using DoAnChuyenNganh.data;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DoAnChuyenNganh.ViewModel;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;
using PagedList;
using System.Security.Cryptography;
using DoAnChuyenNganh.Services;

namespace DoAnChuyenNganh.Controllers
{
    public class HomeController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IVnPayService vnPayService)
        {
            _context = context;
            _logger = logger;
            _vnPayService = vnPayService;
        }
        public IActionResult AccessDenied()
        {
            return View(); // Trả về view AccessDenied
        }
        public IActionResult Contact()
        {
            {
                // Lấy dữ liệu liên hệ từ cơ sở dữ liệu
                var contactInfo = _context.ContactInfos.FirstOrDefault();

                // Nếu không có dữ liệu, trả về trang lỗi hoặc một giá trị mặc định
                if (contactInfo == null)
                {
                    contactInfo = new ContactInfo
                    {
                        Name = "Hutech Khu E",
                        Address = "Quận 9, TP.HCM",
                        PhoneNumber = "0913 133 2046",
                        Email = "votanqui29052003@gmail.com",
                        WorkingHours = "7:30 AM đến 9 PM",
                        MapUrl = "https://www.google.com/maps/embed?pb=..."
                    };
                }

                return View(contactInfo);
            }
        }
        public IActionResult FlashSale(int page = 1, int pageSize = 9)
        {
            // Lấy tất cả sản phẩm và lọc các sản phẩm có % giảm giá > 50%
            var now = DateTime.Now; // Thời gian hiện tại
            var flashSaleProducts = _context.Products
                .Where(p => _context.FlashSales
                    .Any(fs => fs.ProductId == p.ProductID && fs.StartTime <= now && fs.EndTime >= now)) // Kiểm tra Flash Sale còn hiệu lực
                .ToList();


            var products = flashSaleProducts.ToPagedList(page, pageSize);
            var timeRemaining = products.Select(product =>
            {
                var flashSale = _context.FlashSales
                    .FirstOrDefault(fs => fs.ProductId == product.ProductID && fs.StartTime <= now && fs.EndTime >= now);

                return flashSale != null ? (flashSale.EndTime - now) : TimeSpan.Zero;
            }).ToList();
            // Khởi tạo ViewModel (sử dụng cùng một ViewModel nếu cần)
            var viewModel = new AllproductViewModel
            {
                Brands = _context.Brands.ToList(),
                Sizes = _context.Sizes.ToList(),
                Colors = _context.Colors.ToList(),
                Products = products,
                TimeRemaining = timeRemaining
            };

            // Tính toán đánh giá trung bình và % giảm giá cho từng sản phẩm
            foreach (var product in products)
            {
                // Tính trung bình sao, nếu không có đánh giá thì giá trị là 0
                var productReviews = _context.Reviews
                    .Where(r => r.ProductID == product.ProductID)
                    .Select(r => r.Rating);

                product.AverageRating = productReviews.Any()
                    ? productReviews.Average()
                    : 0;

                // Tính % giảm giá, nếu không có giá gốc hoặc giá khuyến mãi thì đặt là 0
                if (product.Price.HasValue && product.Price > 0 && product.SalePrice.HasValue && product.SalePrice > 0)
                {
                    product.DiscountPercentage = (int)Math.Round(100 - (product.SalePrice.Value / product.Price.Value) * 100, 0);
                }
                else
                {
                    product.DiscountPercentage = 0; // Không có giảm giá
                }
            }

            return View(viewModel); // Truyền ViewModel vào View
        }



        [Authorize]
        public async Task<IActionResult> Wishlist(int page = 1, int pageSize = 6)
        {
            // Lấy email của người dùng đang đăng nhập
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Lấy UserID dựa trên email
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.UserID })
                .FirstOrDefault();

            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng nếu không tìm thấy user
            }

            var userId = user.UserID;

            // Lấy danh sách yêu thích của người dùng và thông tin sản phẩm
            var wishlistQuery = _context.Wishlists
                .Where(w => w.UserID == userId)
                .Select(w => w.ProductID)
                .Distinct(); // Đảm bảo không có trùng lặp ProductID

            // Lấy thông tin sản phẩm tương ứng với ProductID trong Wishlist
            var products = await _context.Products
                .Where(p => wishlistQuery.Contains(p.ProductID))
                .ToListAsync();

            // Xây dựng ViewModel kết hợp Wishlist và Product
            var wishlistViewModels = products.Select(product => new WishlistProductViewModel
            {
                Wishlist = _context.Wishlists.FirstOrDefault(w => w.ProductID == product.ProductID && w.UserID == userId),
                Product = product
            }).ToList();

            // Phân trang
            var pagedList = wishlistViewModels.ToPagedList(page, pageSize);

            return View(pagedList);
        }

        [Authorize]
        public async Task<IActionResult> ListPurchasedProducts(int page = 1, int pageSize = 6)
        {
            // Lấy email của người dùng đang đăng nhập
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Lấy UserID dựa trên email
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.UserID })
                .FirstOrDefault();

            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng nếu không tìm thấy user
            }

            var userId = user.UserID;

            // Lấy danh sách đơn hàng của người dùng hiện tại, bao gồm chi tiết đơn hàng
            var ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate);

            var orders = await ordersQuery.ToListAsync();

            // Tạo danh sách ViewModel
            var orderViewModels = orders.Select(order => new PurchasedProductViewModel
            {
                Order = order,
                OrderDetails = order.OrderDetails.ToList()
            }).ToList();

            // Phân trang
            var pagedList = orderViewModels.ToPagedList(page, pageSize);

            return View(pagedList);
        }


        [HttpPost]
        public IActionResult ToggleWishlist(int ProductID)
        {
            if (!User.Identity.IsAuthenticated)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }

            var email = User.FindFirstValue(ClaimTypes.Email);

            // Lấy UserID dựa trên email
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new
                {
                    u.UserID
                })
                .FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            // Kiểm tra xem sản phẩm có trong wishlist của người dùng chưa
            var wishlistItem = _context.Wishlists.FirstOrDefault(w => w.ProductID == ProductID && w.UserID == user.UserID);

            if (wishlistItem != null)
            {
                // Nếu sản phẩm đã có trong wishlist, xóa khỏi wishlist
                _context.Wishlists.Remove(wishlistItem);
                _context.SaveChanges();

                return Json(new { success = true, action = "removed", message = "Đã xoá khỏi danh sách yêu thích." });
            }
            else
            {
                // Nếu sản phẩm chưa có, thêm vào wishlist
                var newWishlistItem = new Wishlist
                {
                    UserID = user.UserID,
                    ProductID = ProductID,
                    CreatedAt = DateTime.Now
                };

                _context.Wishlists.Add(newWishlistItem);
                _context.SaveChanges();

                return Json(new { success = true, action = "added", message = "Đã thêm vào danh sách yêu thích." });
            }
        }


        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sản phẩm có Status = "Ghim"
            var activeProducts = await _context.Products
                                               .Where(p => p.Status == "Ghim")
                                               .ToListAsync();

            // Lấy tất cả sản phẩm
            var allProducts = await _context.Products.ToListAsync();

            // Lấy tất cả các HomeContent (nếu có nhiều bản ghi)
            var homeContents = await _context.HomeContents.ToListAsync(); // Sử dụng ToListAsync() nếu có nhiều HomeContent

            // Tạo ViewModel để truyền cả hai danh sách vào View
            var viewModel = new ProductViewModel
            {
                ActiveProducts = activeProducts,
                AllProducts = allProducts,
                HomeContent = homeContents.FirstOrDefault() // Truyền đối tượng HomeContent đầu tiên vào ViewModel (nếu chỉ lấy 1 bản ghi)
            };

            return View(viewModel); // Chuyển sản phẩm và HomeContent sang View
        }

        public IActionResult Products(int page = 1, int pageSize = 9)
        {
            // Lấy danh sách tất cả sản phẩm và phân trang
            var allProducts = _context.Products.ToList();
            var products = allProducts.ToPagedList(page, pageSize);

            // Khởi tạo ViewModel
            var viewModel = new AllproductViewModel
            {
                Brands = _context.Brands.ToList(),
                Sizes = _context.Sizes.ToList(),
                Colors = _context.Colors.ToList(),
                Categories = _context.Categories.ToList(),
                Products = products
            };

            // Tính toán đánh giá trung bình và % giảm giá cho từng sản phẩm
            foreach (var product in products)
            {
                // Tính trung bình sao, nếu không có đánh giá thì giá trị là 0
                var productReviews = _context.Reviews
                    .Where(r => r.ProductID == product.ProductID)
                    .Select(r => r.Rating);

                product.AverageRating = productReviews.Any()
                    ? productReviews.Average()
                    : 0;

                // Tính % giảm giá, nếu không có giá gốc hoặc giá khuyến mãi thì đặt là 0
                if (product.Price.HasValue && product.Price > 0 && product.SalePrice.HasValue && product.SalePrice > 0)
                {
                    product.DiscountPercentage = (int)Math.Round(100 - (product.SalePrice.Value / product.Price.Value) * 100, 0);
                }
                else
                {
                    product.DiscountPercentage = 0; // Không có giảm giá
                }
            }

            // Kiểm tra nếu user đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var userId = _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.UserID)
                    .FirstOrDefault();

                if (userId != 0)
                {
                    // Lấy danh sách sản phẩm mà user đã thêm vào wishlist
                    var wishlistProductIds = _context.Wishlists
                        .Where(w => w.UserID == userId)
                        .Select(w => w.ProductID)
                        .ToList();

                    // Cập nhật trạng thái yêu thích cho từng sản phẩm
                    foreach (var product in products)
                    {
                        viewModel.WishlistStatuses[product.ProductID] = wishlistProductIds.Contains(product.ProductID);
                    }
                }
            }

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> SearchProducts(string selectedBrand, string selectedSize, string selectedColor, string selectedCategory, int page = 1, int pageSize = 9)
        {
            // Prepare the query for products
            IQueryable<Product> productsQuery = _context.Products.AsQueryable();

            // Apply filter for Brand if provided
            if (!string.IsNullOrEmpty(selectedBrand))
            {
                productsQuery = productsQuery.Where(p => p.BrandID.ToString() == selectedBrand);
            }

            // Apply filter for Category if provided
            if (!string.IsNullOrEmpty(selectedCategory))
            {
                productsQuery = productsQuery.Where(p => p.CategoryID.ToString() == selectedCategory);
            }

            // Prepare a list of filtered Product IDs based on Size and Color, if they are provided
            List<int> filteredProductIds = new List<int>();

            // Check if Size or Color filters are provided
            if (!string.IsNullOrEmpty(selectedSize) || !string.IsNullOrEmpty(selectedColor))
            {
                // Step 1: Prepare a query for ProductVariants
                IQueryable<ProductVariant> variantsQuery = _context.ProductVariants.AsQueryable();

                // Apply filters for Size and Color if they are provided
                if (!string.IsNullOrEmpty(selectedSize))
                {
                    variantsQuery = variantsQuery.Where(v => v.SizeID.ToString() == selectedSize);
                }

                if (!string.IsNullOrEmpty(selectedColor))
                {
                    variantsQuery = variantsQuery.Where(v => v.ColorID.ToString() == selectedColor);
                }

                // Get distinct Product IDs from filtered ProductVariants
                filteredProductIds = await variantsQuery
                    .Select(v => v.ProductID) // Get ProductID from ProductVariant
                    .Distinct()                // Ensure unique ProductIDs
                    .ToListAsync();
            }

            // Step 2: Filter Products based on filtered ProductIDs
            if (filteredProductIds.Any())
            {
                productsQuery = productsQuery.Where(p => filteredProductIds.Contains(p.ProductID));
            }

            // Fetch the products
            var products = await productsQuery.ToListAsync();
            var pagedProducts = products.ToPagedList(page, pageSize);

            var viewModel = new AllproductViewModel
            {
                Brands = await _context.Brands.ToListAsync(),
                Sizes = await _context.Sizes.ToListAsync(),
                Colors = await _context.Colors.ToListAsync(),
                Categories = await _context.Categories.ToListAsync(), // Add Categories
                Products = pagedProducts // Use the filtered products
            };

            if (!pagedProducts.Any())
            {
                TempData["MessageProductError1"] = "Không tìm thấy sản phẩm nào với bộ lọc đã chọn.";
            }

            return View("Products", viewModel); // Return to Products view with filtered results
        }


        public async Task<IActionResult> Details(int id)
        {
            // Lấy sản phẩm theo ID
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound(); // Nếu không tìm thấy sản phẩm
            }

            // Lấy tên thương hiệu từ bảng Brand dựa trên BrandID
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandID == product.BrandID);
            var brandName = brand?.BrandName ?? "Không có thương hiệu";

            // Lấy tất cả biến thể và lọc theo ProductID
            var productVariants = await _context.ProductVariants
                .Where(v => v.ProductID == id)
                .ToListAsync();

            // Tạo danh sách để lưu trữ tên màu và kích thước cho từng biến thể
            var variantInfoList = new List<(string ColorName, string SizeName, ProductVariant Variant)>();

            foreach (var variant in productVariants)
            {
                // Lấy tên màu sắc từ bảng Colors dựa trên ColorID
                var color = await _context.Colors.FirstOrDefaultAsync(c => c.ColorID == variant.ColorID);
                string colorName = color?.Color ?? "Không có màu";

                // Lấy tên kích thước từ bảng Sizes dựa trên SizeID
                var size = await _context.Sizes.FirstOrDefaultAsync(s => s.SizeID == variant.SizeID);
                string sizeName = size?.Size ?? "Không có kích thước";

                // Thêm vào danh sách kết quả
                variantInfoList.Add((colorName, sizeName, variant));
            }

            // Sắp xếp danh sách theo kích thước (SizeName) và loại bỏ các mục trùng lặp
            variantInfoList = variantInfoList
                .OrderBy(v => int.TryParse(v.SizeName, out var parsedSize) ? parsedSize : 0) // Sắp xếp theo giá trị số
                .GroupBy(v => v.SizeName) // Nhóm theo kích thước để loại bỏ trùng lặp
                .Select(g => g.First()) // Chọn biến thể đầu tiên trong nhóm
                .ToList();

            // Tìm các đánh giá cho sản phẩm và lấy tên người dùng từ bảng Users
            var reviews = await _context.Reviews
                .Where(r => r.ProductID == id)
                .Select(r => new
                {
                    r.Comment,
                    r.Rating,
                    r.CreatedAt,
                    UserName = _context.Users.FirstOrDefault(u => u.UserID == r.UserID).FullName // Lấy tên đầy đủ từ bảng Users
                })
                .ToListAsync();

            // Chuyển đổi danh sách đánh giá sang kiểu mà ViewModel có thể sử dụng
            var reviewList = reviews.Select(r => new ReviewViewModel
            {
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt,
                FullName = r.UserName ?? "Ẩn danh" // Hiển thị "Ẩn danh" nếu không có tên
            }).ToList();
            decimal productPrice = product.Price ?? 0; // Xử lý giá trị null
            decimal priceLowerBound = productPrice * 0.8m;
            decimal priceUpperBound = productPrice * 1.2m;

            var similarProducts = await _context.Products
                .Where(p =>
                    p.BrandID == product.BrandID &&
                    p.ProductID != id &&
                    p.Price >= priceLowerBound &&
                    p.Price <= priceUpperBound)
                .Take(5)
                .ToListAsync();

            // Tạo ViewModel
            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                ProductVariants = productVariants,
                Reviews = reviewList,
                BrandName = brandName,
                ColorSizeVariants = variantInfoList,
                SimilarProducts = similarProducts// Sử dụng trường này để chứa thông tin màu sắc và kích thước
            };

            return View(viewModel); // Trả về View với ViewModel
        }
         public IActionResult Success()
        {
            return View();
        }
        public IActionResult ErrorOrder()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddReview(int productId, string comment, int rating)
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (!User.Identity.IsAuthenticated)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập
                return RedirectToAction("Login", "Account"); // Đảm bảo bạn có controller và action cho trang đăng nhập
            }

            // Lấy tên người dùng từ thông tin xác thực
            string fullName = User.Identity.Name; // Assuming that User.Identity.Name contains the full name

            // Kiểm tra nếu bình luận rỗng
            if (string.IsNullOrEmpty(comment))
            {
                ModelState.AddModelError("Comment", "Bạn cần nhập bình luận.");
                return RedirectToAction("Details", new { id = productId });
            }

            // Kiểm tra nếu đánh giá không hợp lệ
            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("Rating", "Đánh giá phải từ 1 đến 5 sao.");
                return RedirectToAction("Details", new { id = productId });
            }

            // Tạo một đánh giá mới
            var review = new Review
            {
                ProductID = productId,
                UserID = GetUserId(fullName), // Giả định có một hàm để lấy UserID từ tên
                Comment = comment,
                Rating = rating,
                CreatedAt = DateTime.Now
            };

            // Thêm đánh giá vào cơ sở dữ liệu
            _context.Reviews.Add(review);
            _context.SaveChanges();

            // Chuyển hướng về trang chi tiết sản phẩm
            return RedirectToAction("Details", new { id = productId });
        }

        // Giả định có một hàm lấy UserID từ tên
        private int GetUserId(string fullName)
        {
            // Logic lấy UserID từ tên người dùng hoặc session
            var user = _context.Users.FirstOrDefault(u => u.FullName == fullName);
            return user?.UserID ?? 0; // Trả về 0 nếu không tìm thấy người dùng
        }
        [HttpPost]
        public IActionResult AddToCart([FromBody] AddToCartViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem sản phẩm có tồn tại trong cơ sở dữ liệu không
                var product = _context.Products.FirstOrDefault(p => p.ProductID == model.ProductID);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Lấy email của người dùng đang đăng nhập
                var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(currentEmail))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
                }

                // Lấy UserID dựa trên email
                var user = _context.Users
                .Where(u => u.Email == currentEmail)
                .Select(u => new {
                    u.UserID,
                    u.Email,
                    u.FullName,
                     u.Password ,
                    u.Phone,
                     u.Address
                })
                .FirstOrDefault();

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                var size = _context.Sizes
               .Where(s => s.Size == model.SizeName)
               .FirstOrDefault();

                if (size == null)
                {
                    return Json(new { success = false, message = $"Kích thước {model.SizeName} không tồn tại." });
                }

                // Kiểm tra ColorID từ bảng Color dựa trên ColorName
                var color = _context.Colors
                    .Where(c => c.Color == model.ColorName)
                    .FirstOrDefault();

                if (color == null)
                {
                    return Json(new { success = false, message = $"Màu sắc {model.ColorName} không tồn tại." });
                }

                // Kiểm tra biến thể sản phẩm dựa trên ProductID, SizeID và ColorID
                var productVariant = _context.ProductVariants
                    .Where(pv => pv.ProductID == model.ProductID
                                 && pv.SizeID == size.SizeID
                                 && pv.ColorID == color.ColorID)
                    .FirstOrDefault();

                if (productVariant == null)
                {
                    return Json(new { success = false, message = $"Sản phẩm {product.Name} hiện không có size và màu sắc mà bạn chọn (Size: {model.SizeName}, Color: {model.ColorName})." });
                }
                // Tính tổng tiền cho sản phẩm
                decimal unitPrice = product.SalePrice.GetValueOrDefault(product.Price.GetValueOrDefault()); // Sử dụng giá khuyến mãi nếu có, ngược lại dùng giá gốc
                decimal totalPrice = unitPrice * model.Quantity; // Tính tổng tiền

                // Tạo đối tượng CartItem với thông tin từ sản phẩm
                var cartItem = new CartItem
                {
                    ProductID = model.ProductID,
                    SizeName = model.SizeName,
                    ColorName = model.ColorName,
                    Quantity = model.Quantity,
                    Name = product.Name, // Lấy tên sản phẩm
                    Image1 = product.Image1, // Lấy hình ảnh chính
                    Price = product.Price, // Lấy giá gốc
                    SalePrice = product.SalePrice, // Lấy giá khuyến mãi
                    UserID = user.UserID, // Lưu UserID vào CartItem dựa trên email
                    TotalPrice = totalPrice // Lưu tổng tiền vào CartItem
                };

                // Thêm cartItem vào database thông qua DbContext
                _context.CartItems.Add(cartItem);
                _context.SaveChanges();

                return Json(new { success = true, message = "Sản phẩm đã được thêm vào giỏ hàng!" });
            }

            return Json(new { success = false, message = "Vui Lòng Chọn Kích Thước Và Màu Sắc." });
        }
        public IActionResult Test()
        {
            return View();
        }
        [HttpPost] // Ensure this method handles POST requests
        public IActionResult CreatePaymentUrl([FromBody] OrderViewModel model)
        {
            // Validate the incoming model

            // Lấy email từ Claims
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Lấy UserID dựa trên email
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.UserID, u.FullName, u.Email, u.Phone }) // Lấy thêm Email và Phone
                .FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Tạo đơn hàng (Order)
            var newOrder = new Order
            {
                FullName = model.FullName,
                ShippingAddress = model.ShippingAddress,
                UserID = user.UserID,
                OrderDate = DateTime.Now,
                Status = "Đang chờ thanh toán", // Trạng thái đơn hàng ban đầu là chờ thanh toán
                PriceShip = model.PriceShip, // Phí vận chuyển
                TotalAmount = model.TotalAmount, // Tổng số tiền đơn hàng
                PaymentMethod = model.PaymentMethod, // Phương thức thanh toán
                Note = model.Note // Ghi chú
            };

            // Lưu đơn hàng vào DB
            _context.Orders.Add(newOrder);
            _context.SaveChanges();

            // Lưu OrderDetails từ CartItems nhưng không xóa giỏ hàng và không trừ số lượng tồn kho
            var cartItems = _context.CartItems.Where(ci => ci.UserID == user.UserID).ToList();
            foreach (var item in cartItems)
            {
                // Tạo chi tiết đơn hàng
                var orderDetail = new OrderDetail
                {
                    OrderID = newOrder.OrderID, // Gán OrderID của đơn hàng vừa tạo
                    ProductID = item.ProductID,
                    SizeName = item.SizeName, // Thêm kích thước
                    ColorName = item.ColorName, // Thêm màu sắc
                    Quantity = item.Quantity,
                    Name = item.Name, // Thêm tên sản phẩm
                    Image1 = item.Image1, // Thêm hình ảnh sản phẩm
                    TotalPrice = item.SalePrice.HasValue ? item.SalePrice.Value * item.Quantity : item.Price * item.Quantity,
                    FullName = model.FullName, // Ghi lại thông tin khách hàng
                    Email = user.Email, // Thêm email của người dùng
                    Phone = user.Phone // Thêm số điện thoại của người dùng
                };

                _context.OrderDetails.Add(orderDetail);
            }

            // Lưu thay đổi cho OrderDetails nhưng không trừ tồn kho hoặc xóa cart
            _context.SaveChanges();

            // Call the service to create the payment URL
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            // Return a JSON response with the payment URL
            return Json(new { success = true, paymentUrl = url });
        }
        public IActionResult PaymentCallback()
        {
            // Nhận phản hồi từ VNPAY
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.VnPayResponseCode == "00" && response.Success)
            {
                // Lấy email từ Claims
                var email = User.FindFirstValue(ClaimTypes.Email);

                // Tìm user dựa trên email
                var user = _context.Users
                    .Where(u => u.Email == email)
                    .FirstOrDefault();

                if (user == null)
                {
                    return Json(new { success = false, message = "Người dùng không tồn tại." });
                }

                // Tìm đơn hàng đang thanh toán với PaymentMethod là VNPAY và trạng thái "Đang thanh toán"
                var newOrder = _context.Orders
                    .Where(o => o.UserID == user.UserID && o.PaymentMethod == "VNPAY" && o.Status == "Đang chờ thanh toán")
                    .FirstOrDefault();

                if (newOrder == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng đang thanh toán bằng VNPAY." });
                }

                // Cập nhật trạng thái đơn hàng sau khi thanh toán thành công
                newOrder.Status = "Đã thanh toán";

                // Lưu thay đổi vào DB
                _context.SaveChanges();

                // Lấy CartItems của User liên quan đến đơn hàng
                var cartItems = _context.CartItems
                    .Where(ci => ci.UserID == newOrder.UserID)
                    .ToList();

                string productDetails = "";
                // Chuẩn bị chi tiết đơn hàng và sản phẩm
                foreach (var item in cartItems)
                {
                    // Tính tổng giá cho từng sản phẩm
                    decimal itemTotalPrice = item.SalePrice.HasValue ? item.SalePrice.Value * item.Quantity : (decimal)item.Price * item.Quantity;

                    productDetails += $"Tên sản phẩm: {item.Name}, Số lượng: {item.Quantity}, Size: {item.SizeName}, Màu: {item.ColorName}, Tổng tiền: {itemTotalPrice:N0} VND\n";
                }

                // Chuẩn bị chi tiết đơn hàng để gửi email
                string orderDetails = $"Mã đơn hàng: {newOrder.OrderID}\n" +
                                      $"Ngày đặt hàng: {newOrder.OrderDate.ToString("dd/MM/yyyy")}\n" +
                                      $"Sản phẩm:\n{productDetails}" +
                                      $"Tổng tiền: {newOrder.TotalAmount:N0} VND\n" +
                                      $"Phí vận chuyển: {newOrder.PriceShip:N0} VND\n" +
                                      $"Địa chỉ giao hàng: {newOrder.ShippingAddress}";

                // Gửi email xác nhận đơn hàng sau khi thanh toán thành công
                var orderEmailService = new OrderEmailService
                {
                    To = user.Email
                };
                orderEmailService.SendOrderConfirmation(user.FullName, orderDetails, newOrder.TotalAmount, newOrder.ShippingAddress);

                // Xóa CartItems của User sau khi thanh toán
                _context.CartItems.RemoveRange(cartItems);
                _context.SaveChanges();

                // Chuyển hướng đến trang thông báo thành công
                return RedirectToAction("Success", "Home");
            }

            // Xử lý khi thanh toán thất bại
            return RedirectToAction("ErroOrder", "Home");
        }
        [HttpPost]
        public IActionResult SubmitOrder([FromBody] OrderViewModel order)
        {
            // Lấy email từ Claims
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Lấy UserID dựa trên email
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new {
                    u.UserID,
                    u.Email,
                    u.FullName,
                    u.Password ,
                    u.Phone ,
                    u.Address,
                })
                .FirstOrDefault();

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }
            var newOrder = new Order
            {
                FullName = order.FullName,
                ShippingAddress = order.ShippingAddress,
                UserID= user.UserID,
                OrderDate = DateTime.Now,
                Status = "Đang chuẩn bị hàng", // Trạng thái đơn hàng ban đầu
                PriceShip = order.PriceShip, // Phí vận chuyển
                TotalAmount = order.TotalAmount, // Tổng số tiền đơn hàng
                PaymentMethod = order.PaymentMethod, // Phương thức thanh toán
                Note = order.Note // Ghi chú
            };

            // Lưu đơn hàng vào DB
            _context.Orders.Add(newOrder);
            _context.SaveChanges();

            // Lấy CartItems của User
            var cartItems = _context.CartItems
                .Where(ci => ci.UserID == user.UserID)
                .ToList();
            string productDetails = "";
            // Lưu OrderDetails từ CartItems và trừ số lượng tồn kho
            foreach (var item in cartItems)
            {
                // Tìm SizeID từ bảng Size dựa trên SizeName
                var size = _context.Sizes
                    .Where(s => s.Size == item.SizeName)
                    .FirstOrDefault();

                if (size == null)
                {
                    return Json(new { success = false, message = $"Kích thước {item.SizeName} không tồn tại." });
                }

                // Tìm ColorID từ bảng Color dựa trên ColorName
                var color = _context.Colors
                    .Where(c => c.Color == item.ColorName)
                    .FirstOrDefault();

                if (color == null)
                {
                    return Json(new { success = false, message = $"Màu sắc {item.ColorName} không tồn tại." });
                }

                // Tìm ProductVariant dựa trên ProductID, SizeID và ColorID
                var productVariant = _context.ProductVariants
                    .Where(pv => pv.ProductID == item.ProductID
                                 && pv.SizeID == size.SizeID
                                 && pv.ColorID == color.ColorID)
                    .FirstOrDefault();

                if (productVariant == null)
                {
                    return Json(new { success = false, message = $"Sản phẩm {item.Name} hiện không có size và màu sắc mà bạn chọn (Size: {item.SizeName}, Color: {item.ColorName})." });
                }

                // Kiểm tra tồn kho
                if (productVariant.Stock < item.Quantity)
                {
                    return Json(new { success = false, message = $"Sản phẩm {item.Name} không đủ hàng tồn kho." });
                }

                // Trừ số lượng tồn kho
                productVariant.Stock -= item.Quantity;

                // Tạo chi tiết đơn hàng
                var orderDetail = new OrderDetail
                {
                    OrderID = newOrder.OrderID, // Gán OrderID của đơn hàng vừa tạo
                    ProductID = item.ProductID,
                    SizeName = item.SizeName,
                    ColorName = item.ColorName,
                    Quantity = item.Quantity,
                    Name = item.Name,
                    Image1 = item.Image1,
                    TotalPrice = item.SalePrice.HasValue ? item.SalePrice.Value * item.Quantity : item.Price * item.Quantity,
                    FullName = order.FullName,
                    Email = user.Email,
                    Phone = order.Phone
                };

                // Thêm chi tiết đơn hàng vào DB
                _context.OrderDetails.Add(orderDetail);
                decimal itemTotalPrice = item.SalePrice.HasValue ? item.SalePrice.Value * item.Quantity : (decimal)item.Price * item.Quantity;
                productDetails += $"Tên sản phẩm: {item.Name}, Số lượng: {item.Quantity}, Size: {item.SizeName}, Màu: {item.ColorName}, Tổng tiền: {itemTotalPrice:N0} VND\n";
            }

            // Lưu thay đổi cho tồn kho và chi tiết đơn hàng
            _context.SaveChanges();

            // Xóa CartItems của User sau khi đặt hàng
            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();

            string orderDetails = $"Mã đơn hàng: {newOrder.OrderID}\n" +
                            $"Ngày đặt hàng: {newOrder.OrderDate.ToString("dd/MM/yyyy")}\n" +
                            $"Sản phẩm:\n{productDetails}" +
                            $"Tổng tiền: {newOrder.TotalAmount:N0} VND\n" +
                            $"Phí vận chuyển: {order.PriceShip:N0} VND\n" +
                            $"Địa chỉ giao hàng: {newOrder.ShippingAddress}";

            // Gửi email xác nhận đơn hàng
            var orderEmailService = new OrderEmailService { To = user.Email };
            orderEmailService.SendOrderConfirmation(user.FullName, orderDetails, newOrder.TotalAmount, newOrder.ShippingAddress);
            // Trả về JSON với thông báo thành công
            return Json(new { success = true, message = "Đặt hàng thành công!", redirectUrl = Url.Action("Success", "Home") });
        }

        public IActionResult Cart()
        {
            // Lấy email của người dùng đang đăng nhập
            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(currentEmail))
            {
                // Nếu người dùng chưa đăng nhập, chuyển hướng đến trang đăng nhập hoặc xử lý theo nhu cầu
                return RedirectToAction("Login", "Account");
            }

            // Lấy User dựa trên email và xử lý các giá trị null cho Password, Phone, và Address
            var user = _context.Users
                .Where(u => u.Email == currentEmail)
                .Select(u => new {
                    u.UserID,
                    u.Email,
                    u.FullName,
                    Password = u.Password != null ? u.Password : "Không có thông tin",
                    Phone = u.Phone != null ? u.Phone : "Không có thông tin",
                    Address = u.Address != null ? u.Address : "Không có thông tin"
                })
                .FirstOrDefault();

            if (user == null)
            {
                // Xử lý khi không tìm thấy người dùng
                return RedirectToAction("Login", "Account");
            }

            // Truy vấn giỏ hàng của người dùng dựa trên Email (hoặc UserID nếu cần)
            var cart = _context.CartItems
                .Where(ci => ci.UserID == user.UserID) // Có thể thay bằng UserID nếu cần
                .ToList();

            // Trả giỏ hàng và thông tin user cho view
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            // Find the cart item by its ID
            var cartItem = _context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
            if (cartItem == null)
            {
                return NotFound(new { message = "Item not found" });
            }

            // Update the quantity, or remove the item if quantity is set to 0
            if (quantity > 0)
            {
                cartItem.Quantity = quantity; // Update the quantity
                                              // Update the TotalPrice based on the new quantity
                cartItem.TotalPrice = (cartItem.SalePrice ?? cartItem.Price) * cartItem.Quantity;
            }
            else
            {
                _context.CartItems.Remove(cartItem); // Remove the cart item if quantity is 0
            }

            // Save changes to the database
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Cập nhật giỏ hàng thành công!";
            return RedirectToAction("Cart");
        }

     

        [HttpPost]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            var cartItem = _context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Cart");
        }
        public async Task<IActionResult> Checkout()
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang đăng nhập nếu người dùng chưa đăng nhập
            }

            // Lấy email của người dùng đã đăng nhập
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Lấy thông tin người dùng
            var user = await _context.Users
                .Where(u => u.Email == userEmail)
                .Select(u => new UserViewModel // Sử dụng UserViewModel
                {
                    UserID = u.UserID,
                    Email = u.Email,
                    FullName = u.FullName,
                    Password = u.Password != null ? u.Password : "Không có thông tin",
                    Phone = u.Phone != null ? u.Phone : "Không có thông tin",
                    Address = u.Address != null ? u.Address : "Không có thông tin"
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng nếu không tìm thấy người dùng
            }

            // Lấy các sản phẩm trong giỏ hàng theo UserID
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserID == user.UserID)
                .Include(ci => ci.Product) // Bao gồm thông tin sản phẩm liên quan
                .ToListAsync();

            // Kiểm tra xem giỏ hàng có sản phẩm không
            if (!cartItems.Any())
            {
                // Chuyển hướng về trang Cart nếu giỏ hàng trống
                return RedirectToAction("Cart", "Home"); // Chuyển hướng tới trang Cart
            }

            // Tính tổng giá trị từ cột TotalPrice trong CartItems
            decimal totalPrice = cartItems.Sum(ci => ci.TotalPrice ?? 0);

            // Lấy danh sách tỉnh từ phương thức GetProvinces
            var locationViewModel = new LocationViewModel
            {
                Provinces = await GetProvinces(), // Giả sử đây là phương thức lấy danh sách tỉnh
                Districts = new List<District>(), // Khởi tạo danh sách trống
                Wards = new List<Ward>() // Khởi tạo danh sách trống
            };

            // Tạo ViewModel kết hợp
            var model = new CheckoutFullViewModel
            {
                Checkout = new CheckoutViewModel
                {
                    User = user, // Sử dụng UserViewModel
                    CartItems = cartItems,
                    TotalPrice = totalPrice
                },
                Location = locationViewModel
            };

            return View(model);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private static readonly string apiUrl = "https://online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee";
        private static readonly string token = "aab15036-86ac-11ef-bbb6-a2edf3918909"; // Token của bạn
        private static readonly string shopId = "5399908"; // ShopId của bạn

        public async Task<IActionResult> CalculateShippingFee(int districtId, string wardCode)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Token", token);
                client.DefaultRequestHeaders.Add("ShopId", shopId);

                var requestData = new
                {
                    service_type_id = 2,
                    from_district_id = 3276,
                    to_district_id = districtId,
                    to_ward_code = wardCode,
                    height = 2,
                    length = 2,
                    weight = 2,
                    width = 40,
                    insurance_value = 500000,
                    coupon = (string)null,
                    items = new[]
                    {
                new
                {
                    name = "TEST1",
                    quantity = 1,
                    height = 2,
                    weight = 2,
                    length = 2,
                    width = 2
                }
            }
                };

                string jsonData = JsonConvert.SerializeObject(requestData);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();

                        // Parse the JSON response
                        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);

                        // Lấy giá trị total từ dữ liệu phản hồi
                        var total = jsonResponse.data?.total;

                        // Trả về giá trị total cho view
                        return Content($" {total} ");
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        return Content($"Error: {response.StatusCode}, Details: {error}");
                    }
                }
                catch (Exception ex)
                {
                    return Content($"Exception: {ex.Message}");
                }
            }
        }

        private static readonly string apiProvinceUrl = "https://online-gateway.ghn.vn/shiip/public-api/master-data/province";
        private static readonly string apiDistrictUrl = "https://online-gateway.ghn.vn/shiip/public-api/master-data/district";
        private static readonly string apiWardUrl = "https://online-gateway.ghn.vn/shiip/public-api/master-data/ward"; // Thay đổi URL
             [HttpPost]
        private async Task<List<Province>> GetProvinces()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Token", token);

                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiProvinceUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<ProvinceResponse>(result);
                        return data.Data; // Return the list of provinces
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error: {response.StatusCode}, Details: {error}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception: {ex.Message}");
                }
            }
        }


        // Phương thức để lấy quận dựa trên province_id
        [HttpPost]
        public async Task<JsonResult> GetDistricts(int province_id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Token", token);

                var requestData = new
                {
                    province_id = province_id
                };

                string jsonData = JsonConvert.SerializeObject(requestData);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiDistrictUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<DistrictResponse>(result);
                        return Json(data.Data); // Trả về danh sách quận dưới dạng JSON
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = error });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetWards(int district_id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Token", token);


                // Tạo dữ liệu yêu cầu
                var requestData = new
                {
                    district_id = district_id
                };

                // Serialize dữ liệu thành JSON
                string jsonData = JsonConvert.SerializeObject(requestData);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    // Gửi yêu cầu POST đến API để lấy phường/xã
                    HttpResponseMessage response = await client.PostAsync($"{apiWardUrl}?district_id={district_id}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<WardResponse>(result);
                        return Json(data.Data); // Trả về danh sách phường/xã dưới dạng JSON
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = error });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

 

    }
}

