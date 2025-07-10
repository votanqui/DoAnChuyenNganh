using DoAnChuyenNganh.Models;
using PagedList;

namespace DoAnChuyenNganh.ViewModel
{
    public class AllproductViewModel
    {
        public List<Brand> Brands { get; set; }
        public List<Sizes> Sizes { get; set; }
        public List<Colors> Colors { get; set; }
        public IPagedList<Product> Products { get; set; }

        public List<Category> Categories { get; set; }

        public List<TimeSpan> TimeRemaining { get; set; }
        // Các trường để lưu bộ lọc đã chọn
        public Dictionary<int, bool> WishlistStatuses { get; set; } = new Dictionary<int, bool>();
    }

}
