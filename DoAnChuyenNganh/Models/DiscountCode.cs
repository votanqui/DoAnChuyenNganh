namespace DoAnChuyenNganh.Models
{
    public class DiscountCode
    {
        public int DiscountCodeID { get; set; } // ID mã giảm giá
        public string Code { get; set; } // Mã giảm giá
        public decimal DiscountValue { get; set; } // Giá trị giảm
        public bool IsPercentage { get; set; } // true: giảm theo %, false: giảm theo số tiền
        public bool IsUsed { get; set; } // Trạng thái mã (đã sử dụng hay chưa)
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn mã giảm giá
        public int MaxUsesPerUser { get; set; } // Số lần mã giảm giá có thể sử dụng cho mỗi người dùng
        public int TotalUses { get; set; } // Tổng số lần mã giảm giá có thể được sử dụng trên toàn hệ thống
    }

}
