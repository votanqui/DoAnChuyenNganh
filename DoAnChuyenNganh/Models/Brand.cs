using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class Brand
    {
        [Key]
        public int BrandID { get; set; } // Khóa chính

        [Required]
        [MaxLength(100)]
        public string BrandName { get; set; } // Tên thương hiệu
    }
}
