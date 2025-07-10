using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class Colors
    {
        [Key]
        public int ColorID { get; set; } // Khóa chính

        [Required]
        [MaxLength(30)]
        public string Color { get; set; } // Tên màu sắc
    }
}
