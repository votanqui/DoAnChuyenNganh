using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnChuyenNganh.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; } // Make nullable if it can be NULL in the database
        public int? CategoryID { get; set; } // Make nullable if it can be NULL in the database
        public int? BrandID { get; set; }
        // Make nullable if it can be NULL in the database
        public decimal? SalePrice { get; set; }
        public string? Image1 { get; set; }
        public string? Image2 { get; set; }
        public string? Image3 { get; set; }
        public string? Status { get; set; }
        [NotMapped]
        public double AverageRating { get; set; } // Đánh giá trung bình
        [NotMapped]
        public int DiscountPercentage { get; set; }
        [NotMapped]
        public TimeSpan TimeRemaining { get; set; }
    }
}

