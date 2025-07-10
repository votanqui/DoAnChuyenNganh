namespace DoAnChuyenNganh.ViewModel
{
    public class FlashSaleListViewModel
    {
        public int FlashSaleId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

}
