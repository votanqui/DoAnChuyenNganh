using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModel;

namespace DoAnChuyenNganh.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CreatePaymentUrl(OrderViewModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VnPayLibrary();
            var urlCallBack = _configuration["PaymentCallBack:ReturnUrl"];

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((int)model.TotalAmount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"{model.FullName} {model.Note} {model.TotalAmount}{model.OrderID}");
            pay.AddRequestData("vnp_OrderType", model.PaymentMethod);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl =
                pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);

            return paymentUrl;
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var responseData = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

            // Chuyển đổi dữ liệu phản hồi thành PaymentResponseModel
            var paymentResponse = new PaymentResponseModel
            {
                OrderDescription = responseData.OrderDescription, // Sử dụng đúng tên thuộc tính
                TransactionId = responseData.TransactionId,
                OrderId = responseData.OrderId,
                PaymentMethod = responseData.PaymentMethod,
                PaymentId = responseData.PaymentId,
                Success = responseData.VnPayResponseCode == "00", // Kiểm tra mã phản hồi
                Token = responseData.Token,
                VnPayResponseCode = responseData.VnPayResponseCode
            };

            return paymentResponse;
        }
    }
}
