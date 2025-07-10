using DoAnChuyenNganh.data;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList;

namespace DoAnChuyenNganh.Controllers
{
    [Authorize(Policy = "RequireAdminOrEmployeeRole")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public EmployeeController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> user(int page = 1, int pageSize = 9)
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
        public async Task<IActionResult> Order(int page = 1, int pageSize = 6, string searchKeyword = null)
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
            return View("Order", pagedList);
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
                TempData["MessageOrderEmployee"] = "Không tìm thấy đơn hàng nào với trạng thái đã chọn.";
                return RedirectToAction("Order", "Employee");
            }

            var users = await _context.Users.ToListAsync();

            var orderViewModels = orders.Select(order => new OrderAdminViewModel
            {
                Order = order,
                UserFullName = users.FirstOrDefault(u => u.UserID == order.UserID)?.FullName,
                OrderDetails = order.OrderDetails.ToList()
            }).ToList();

            var pagedList = orderViewModels.ToPagedList(page, pageSize);

            return View("Order", pagedList); // Return to ListOrder view with filtered results
        }
    }

}
