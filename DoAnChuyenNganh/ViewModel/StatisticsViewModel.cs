namespace DoAnChuyenNganh.ViewModel
{
    public class StatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public decimal TotalRevenueOverall { get; set; }
        public List<OrderByWeek> OrdersByWeek { get; set; }

        // Biểu đồ (Doanh thu, người dùng theo ngày)
        public List<decimal> DailyRevenue { get; set; }
        public List<int> DailyUsers { get; set; }
    }


    public class OrderByWeek
    {
        public int Week { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

}
