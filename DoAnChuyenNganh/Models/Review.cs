namespace DoAnChuyenNganh.Models
{
    public class Review
    {
        public int ReviewID { get; set; }                 // Mã định danh đánh giá
        public int ProductID { get; set; }                  // Mã sản phẩm (liên kết với Products)
        public int UserID { get; set; }                     // Mã người dùng (liên kết với Users)
        public int Rating { get; set; }                     // Đánh giá từ 1 đến 5 sao
        public string Comment { get; set; }                 // Nhận xét về sản phẩm
        public DateTime CreatedAt { get; set; }             // Ngày viết đánh giá

      
    }

}
