using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class Sizes
    {
        [Key]
        public int SizeID { get; set; } // Khóa chính

        [Required]
        [MaxLength(10)]
        public string Size { get; set; } // Kích thước (ví dụ: S, M, L, XL)
    }
}
