using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductVariant> ProductVariants { get; set; }
        public List<ReviewViewModel> Reviews { get; set; } // Cập nhật kiểu dữ liệu
        public string BrandName { get; set; }
        public List<(string ColorName, string SizeName, ProductVariant Variant)> ColorSizeVariants { get; set; }

        public List<Product> SimilarProducts { get; set; }
    }


}
