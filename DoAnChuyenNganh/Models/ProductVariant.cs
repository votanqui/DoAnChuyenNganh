using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantID { get; set; } // Khóa chính

        public int ProductID { get; set; } // Khóa ngoại liên kết với Products

        public int ColorID { get; set; } // Khóa ngoại liên kết với Colors

        public int SizeID { get; set; } // Khóa ngoại liên kết với Sizes

        [Required]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; } // Số lượng tồn kho cho biến thể

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày thêm biến thể vào hệ thống

        public DateTime UpdatedAt { get; set; } = DateTime.Now; // Ngày cập nhật biến thể


    }
}
