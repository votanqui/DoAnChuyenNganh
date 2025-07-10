using System.Net.Mail;
using System.Net;

namespace DoAnChuyenNganh.Models
{
    public class Gmail
    {
        private const string Email = "nhom8provip@gmail.com";
        private const string Password = "f y w x tr r m h w v e m q h u";

        public string To { get; set; }

        public void SendMail(string newPassword)
        {
            string subject = "KHÔI PHỤC MẬT KHẨU TÀI KHOẢN SHOPCUAQUI";
            string body = "Mật khẩu mới của bạn là: " + newPassword;

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
