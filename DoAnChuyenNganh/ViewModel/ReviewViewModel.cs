namespace DoAnChuyenNganh.ViewModel
{
    public class ReviewViewModel
    {
        public string FullName { get; set; } // Thêm thuộc tính này nếu nó chưa tồn tại
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Rating { get; set; }
    }


}
