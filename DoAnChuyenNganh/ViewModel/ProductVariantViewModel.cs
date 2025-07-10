using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class ProductVariantViewModel
    {
        public ProductVariant ProductVariant { get; set; } // Biến thể sản phẩm
        public string ColorName { get; set; } // Tên màu sắc
        public string SizeName { get; set; } // Tên kích thước
        public int Stock => ProductVariant.Stock;
        public int VariantID => ProductVariant.VariantID;
    }

}
