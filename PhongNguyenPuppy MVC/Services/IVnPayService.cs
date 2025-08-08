using PhongNguyenPuppy_MVC.ViewModels;
namespace PhongNguyenPuppy_MVC.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}