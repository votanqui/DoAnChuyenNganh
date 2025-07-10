using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class ProductViewModel
    {
        public List<Product> ActiveProducts { get; set; } // Sản phẩm có Status = 1
        public List<Product> AllProducts { get; set; }    // Tất cả sản phẩm
        public HomeContent HomeContent { get; set; }
    }

}
