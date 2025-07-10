using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class Order
    {
        public int OrderID { get; set; } // Unique identifier for each order
        public string FullName { get; set; } // Customer's full name
        public int UserID {  get; set; }
        public decimal PriceShip { get; set; } // Shipping price
        public decimal TotalAmount { get; set; } // Total amount of the order
        public DateTime OrderDate { get; set; } // Date when the order was placed
        public string Status { get; set; } // Order status (e.g., Pending, Completed, Canceled)
        public string PaymentMethod { get; set; } // Payment method used (e.g., PayPal, COD)
        public string ShippingAddress { get; set; } // Customer's shipping address
        public string Note { get; set; } // Additional notes for the order
                                         //public string Phone22 {  get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>(); // Collection of order details
    }

}

