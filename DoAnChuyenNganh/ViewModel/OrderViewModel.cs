namespace DoAnChuyenNganh.ViewModel
{
    public class OrderViewModel
    {
        public int OrderID { get; set; } // Unique identifier for each order
        public string FullName { get; set; } // Customer's full name
        public decimal PriceShip { get; set; } // Shipping price
        public decimal TotalAmount { get; set; } // Total amount of the order
        public DateTime OrderDate { get; set; } // Date when the order was placed
        public string Status { get; set; } // Order status (e.g., Pending, Completed, Canceled)
        public string PaymentMethod { get; set; } // Payment method used (e.g., PayPal, COD)
        public string ShippingAddress { get; set; } // Customer's shipping address
        public string Note { get; set; }
        public string Phone { get; set; }
    }
}
