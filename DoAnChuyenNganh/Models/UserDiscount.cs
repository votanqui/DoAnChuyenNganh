namespace DoAnChuyenNganh.Models
{
    public class UserDiscount
    {
        public int Id { get; set; }
        public int UserId { get; set; } // ID của người dùng
        public int DiscountCodeID { get; set; } // Mã giảm giá đã được sử dụng
        public DateTime UsedDate { get; set; } // Ngày sử dụng
        public bool IsPending { get; set; }
    }
}
