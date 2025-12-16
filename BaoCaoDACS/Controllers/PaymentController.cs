using System.Diagnostics;
using BAO_CAO.Models;
using BaoCaoDACS.Models;
using BaoCaoDACS.Models.VnPay;
using BaoCaoDACS.Reponsitory;
using BaoCaoDACS.Reponsitory.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Controllers
{

    public class PaymentController : Controller
    {
        private IMomoService _momoService;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IMomoService momoService,IVnPayService vnPayService, ILogger<PaymentController> logger)
        {
            _momoService = momoService;
            _vnPayService = vnPayService;
            _logger = logger;

        }
        [HttpPost]
        [Route("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl(PaymentMultiViewModel vm)
        {
            var model = vm.MomoInfo;
            // kiểm tra null/IsValid…
            var response = await _momoService.CreatePaymentAsync(model);
            if (response == null || string.IsNullOrEmpty(response.PayUrl))
                return BadRequest("Không tạo được URL thanh toán.");
            return Redirect(response.PayUrl);
        }

        [HttpPost]
        [Route("CreateVnpayPaymentUrl")]
        public IActionResult CreateVnpayPaymentUrl(PaymentMultiViewModel vm)
        {
            try
            {
                _logger.LogInformation("Received VNPay payment request");
                
                var model = vm?.VnpayInfo;
                
                // Kiểm tra model validation
                if (model == null)
                {
                    _logger.LogWarning("VNPay model is null");
                    return BadRequest("Dữ liệu gửi sang không đúng định dạng");
                }
                
                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(model.Name) || model.Amount <= 0)
                {
                    _logger.LogWarning("Invalid VNPay payment data: Name={Name}, Amount={Amount}", 
                        model.Name, model.Amount);
                    return BadRequest("Thông tin thanh toán không hợp lệ");
                }

                _logger.LogInformation("Creating VNPay URL for: {Name}, Amount: {Amount}", 
                    model.Name, model.Amount);

                var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL");
                return BadRequest("Có lỗi xảy ra khi tạo URL thanh toán");
            }
        }
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            return Json(response);
        }

    }
}
