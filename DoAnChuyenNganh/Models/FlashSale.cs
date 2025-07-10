namespace DoAnChuyenNganh.Models
{
    public class FlashSale
    {
        public int Id { get; set; } // ID của Flash Sale
        public int ProductId { get; set; } // Liên kết đến sản phẩm
        public DateTime StartTime { get; set; } // Thời gian bắt đầu Flash Sale
        public DateTime EndTime { get; set; } // Thời gian kết thúc Flash Sale
        public decimal Price { get; set; }
        // Navigation property
        public Product Product { get; set; }
    }

}
