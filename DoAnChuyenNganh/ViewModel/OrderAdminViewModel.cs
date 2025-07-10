using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class OrderAdminViewModel
    {
        public Order Order { get; set; } // Thông tin đơn hàng
        public string UserFullName { get; set; } // Tên khách hàng
        public List<OrderDetail> OrderDetails { get; set; } // Sử dụng trực tiếp OrderDetails từ Order
    }

}
