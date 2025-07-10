namespace DoAnChuyenNganh.Models
{
    public class Wishlist
    {
        public int WishlistID { get; set; }                 // Mã định danh danh sách yêu thích
        public int UserID { get; set; }                      // Mã người dùng (liên kết với Users)
        public int ProductID { get; set; }                   // Mã sản phẩm (liên kết với Products)
        public DateTime CreatedAt { get; set; }
    }
}
