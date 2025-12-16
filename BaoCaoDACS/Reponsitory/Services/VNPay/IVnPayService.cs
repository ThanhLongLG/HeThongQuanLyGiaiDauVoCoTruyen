using System.Threading.Tasks;
using BaoCaoDACS.Models;
using BaoCaoDACS.Models.VnPay;
namespace BaoCaoDACS.Reponsitory.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);

    }
}