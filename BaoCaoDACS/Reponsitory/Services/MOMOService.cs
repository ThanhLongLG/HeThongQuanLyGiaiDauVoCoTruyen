using BaoCaoDACS.Models;
using BaoCaoDACS.Reponsitory.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BaoCaoDACS.Reponsitory.Services
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        public MomoService(IOptions<MomoOptionModel> options)
        {
            _options = options;
        }
        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(MomoInfoModel model)
        {
            model.OrderId = DateTime.UtcNow.Ticks.ToString();
            model.OrderInfo = $"Khách hàng: {model.FullName}. Nội dung: {model.OrderInfo}";
            string requestId = model.OrderId;
            string extraData = "";

            // 🔹 Chuỗi ký đúng chuẩn Momo v2
            string rawHash =
                $"accessKey={_options.Value.AccessKey}" +
                $"&amount={model.Amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={_options.Value.NotifyUrl}" +
                $"&orderId={model.OrderId}" +
                $"&orderInfo={model.OrderInfo}" +
                $"&partnerCode={_options.Value.PartnerCode}" +
                $"&redirectUrl={_options.Value.ReturnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={_options.Value.RequestType}";

            var signature = ComputeHmacSha256(rawHash, _options.Value.SecretKey);

            var client = new RestClient(_options.Value.MomoApiUrl);
            var request = new RestRequest { Method = Method.Post };
            request.AddHeader("Content-Type", "application/json");

            var requestData = new
            {
                partnerCode = _options.Value.PartnerCode,
                partnerName = "Test",
                storeId = "MomoTestStore",
                requestId = requestId,
                amount = ((long)model.Amount).ToString(CultureInfo.InvariantCulture),
                orderId = model.OrderId,
                orderInfo = model.OrderInfo,
                redirectUrl = _options.Value.ReturnUrl,
                ipnUrl = _options.Value.NotifyUrl,
                lang = "vi",
                requestType = _options.Value.RequestType,
                extraData = extraData,
                signature = signature
            };

            request.AddJsonBody(requestData);

            var response = await client.ExecuteAsync(request);

            // Log phản hồi thật để kiểm tra
            Console.WriteLine("Momo raw response: " + response.Content);

            var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
            return momoResponse;
        }






        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            var amount = collection.First(s => s.Key == "amount").Value;
            var orderInfo = collection.First(s => s.Key == "orderInfo").Value;
            var orderId = collection.First(s => s.Key == "orderId").Value;

            return new MomoExecuteResponseModel()
            {
                Amount = amount,
                OrderId = orderId,
                OrderInfo = orderInfo

            };
        }
















        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }
    }
}

