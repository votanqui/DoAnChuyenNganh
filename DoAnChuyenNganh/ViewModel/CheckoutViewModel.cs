using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class CheckoutViewModel
    {
        public UserViewModel User { get; set; }
        public List<CartItem> CartItems { get; set; }
        public decimal TotalPrice { get; set; } // Total price of the cart

    }

}
