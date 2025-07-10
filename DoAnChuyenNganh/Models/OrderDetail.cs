namespace DoAnChuyenNganh.Models
{
    public class OrderDetail
    {
        public int OrderDetailID { get; set; } // Unique identifier for each order detail
        public int OrderID { get; set; } // Foreign key to the Orders table
        public int ProductID { get; set; } // ID of the product
        public string SizeName { get; set; } // Size of the product
        public string ColorName { get; set; } // Color of the product
        public int Quantity { get; set; } // Quantity of the product ordered
        public string Name { get; set; } // Name of the product
        public string Image1 { get; set; } // Image URL of the product
        public decimal? TotalPrice { get; set; } // Total price for the quantity of products
        public string FullName { get; set; } // Customer's full name (if needed)
        public string Email { get; set; } // Customer's email (if needed)
        public string Phone { get; set; } // Customer's phone (if needed)

        // Navigation property to the parent Order
        public virtual Order Order { get; set; } // Reference back to the parent order
    }
}
