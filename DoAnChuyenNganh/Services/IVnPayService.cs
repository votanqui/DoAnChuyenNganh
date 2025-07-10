using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModel;
namespace DoAnChuyenNganh.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(OrderViewModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
