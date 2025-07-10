using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class PurchasedProductViewModel
    {
        public Order Order { get; set; } // Thông tin đơn hàng
        public List<OrderDetail> OrderDetails { get; set; } // Sử dụng trực tiếp OrderDetails từ Order
    }
}
