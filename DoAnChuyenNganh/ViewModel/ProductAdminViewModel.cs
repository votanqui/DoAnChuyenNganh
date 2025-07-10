using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class ProductAdminViewModel
    {
        public Product Product { get; set; } // Thông tin sản phẩm
        public List<ProductVariantViewModel> ProductVariants { get; set; } // Danh sách biến thể
        public string BrandName { get; set; }
    }
}
