using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Models;  
namespace DoAnChuyenNganh.data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Users> Users { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Brand> Brands { get; set; }

        public DbSet<Sizes> Sizes { get; set; }

        public DbSet<Colors> Colors { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }

        public DbSet<CartItem>  CartItems { get; set; }

        public DbSet<DiscountCode> DiscountCodes { get; set; }

        public DbSet<UserDiscount> UserDiscounts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        public DbSet<ContactInfo> ContactInfos { get; set; }
        public DbSet<HomeContent> HomeContents { get; set; }

        public DbSet<FlashSale> FlashSales { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    }
}
