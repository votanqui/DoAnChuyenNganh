using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModel
{
    public class LocationViewModel
    {
        public List<Province> Provinces { get; set; }
        public List<District> Districts { get; set; }
        public List<Ward> Wards { get; set; }
    }
}
