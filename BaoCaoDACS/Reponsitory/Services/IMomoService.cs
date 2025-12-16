using System.Threading.Tasks;
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(MomoInfoModel momoInfo);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
     
    }
}