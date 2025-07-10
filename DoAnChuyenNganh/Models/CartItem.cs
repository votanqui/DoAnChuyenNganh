namespace DoAnChuyenNganh.Models
{
    public class CartItem
    {
        public int CartItemID { get; set; }
        public int ProductID { get; set; }
        public string SizeName { get; set; }
        public string ColorName { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; } // Tên sản phẩm
        public string Image1 { get; set; } // Hình ảnh sản phẩm
        public decimal? Price { get; set; } // Giá gốc
        public decimal? SalePrice { get; set; } // Giá khuyến mãi
        public int UserID { get; set; }

        public decimal? TotalPrice { get; set; }

        public virtual Product Product { get; set; }
    }

}
