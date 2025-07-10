namespace DoAnChuyenNganh.ViewModel
{
    public class CartItemViewModel
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public string SizeName { get; set; }
        public string ColorName { get; set; }
        public decimal TotalPrice { get; set; }
        public string Image1 { get; set; } // Add Image1 field here


    }
}
