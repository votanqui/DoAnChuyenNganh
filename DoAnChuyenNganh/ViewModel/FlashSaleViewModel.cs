using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModel
{
    public class FlashSaleViewModel
    {
        public int ProductId { get; set; } // ID sản phẩm được chọn từ dropdown
        public decimal Price { get; set; } // Giá khuyến mãi

        public DateTime StartTime { get; set; } // Thời gian bắt đầu khuyến mãi

        public DateTime EndTime { get; set; } // Thời gian kết thúc khuyến mãi

        public List<SelectListItem>? Products { get; set; } // Danh sách sản phẩm hiển thị trong dropdown
    }

}
