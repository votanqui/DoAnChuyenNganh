using System.Net.Mail;
using System.Net;

namespace DoAnChuyenNganh.Models
{
    public class OrderEmailService
    {
        private const string Email = "nhom8provip@gmail.com";
        private const string Password = "f y w x tr r m h w v e m q h u";

        public string To { get; set; }

        public void SendOrderConfirmation(string fullName, string orderDetails, decimal totalAmount, string shippingAddress)
        {
            string subject = "XÁC NHẬN ĐƠN HÀNG CỦA BẠN TẠI SHOPCUAQUI";
            string body = $"Xin chào {fullName},\n\n" +
                          "Cảm ơn bạn đã đặt hàng tại SHOPCUAQUI. Dưới đây là chi tiết đơn hàng của bạn:\n\n" +
                          $"{orderDetails}\n\n" +
                          $"Tổng tiền: {totalAmount:N0} VND\n" +
                          $"Địa chỉ giao hàng: {shippingAddress}\n\n" +
                          "Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận đơn hàng.\n\n" +
                          "Trân trọng,\nSHOPCUAQUI";

            MailMessage mc = new MailMessage(Email, To);
            mc.Subject = subject;
            mc.Body = body;
            mc.IsBodyHtml = false;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Timeout = 1000000,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Email, Password)
            };

            smtp.Send(mc);
        }
    }
}

