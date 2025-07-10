using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.Models
{
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }                  // Mã định danh duy nhất cho người dùng

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }             // Tên đầy đủ của người dùng

        [Required]
        [StringLength(255)]
        public string? Password { get; set; }             // Mật khẩu (nên được mã hóa)

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }                // Email của người dùng (không được trùng lặp)

        [StringLength(15)]
        [Phone]
        public string? Phone { get; set; }                // Số điện thoại của người dùng

        [StringLength(255)]
        public string? Address { get; set; }              // Địa chỉ của người dùng

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^(customer|employee|admin)$")]
        public string Role { get; set; } = "customer";


        public DateTime CreatedAt { get; set; } = DateTime.Now;  // Ngày tạo tài khoản (mặc định hiện tại)

        public DateTime UpdatedAt { get; set; } = DateTime.Now;  // Ngày cập nhật tài khoản gần nhất

    }
}
