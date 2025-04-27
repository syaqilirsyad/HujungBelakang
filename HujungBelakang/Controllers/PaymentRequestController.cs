using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HujungBelakang.Models;
using HujungBelakang.Helper;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using log4net;
using System.Xml;
using System.Text.Json;

namespace HujungBelakang.Controllers
{
    [Route("api/submittrxmessage")]
    [ApiController]
    public class PaymentRequestController : ControllerBase
    {
        private readonly ILogger<PaymentRequestController> _logger;

        public PaymentRequestController(ILogger<PaymentRequestController> logger)
        {
            _logger = logger;
        }

        private static readonly Dictionary<string, (string Key, string Password)> partners = new()
        {
            ["FG-00001"] = ("FAKEGOOGLE", "FAKEPASSWORD1234"),
            ["FG-00002"] = ("FAKEPEOPLE", "FAKEPASSWORD4578"),
        };

        [HttpPost]
        public IActionResult Post([FromBody] TrxMessageRequestModel req)
        {

            var serializedRequest = JsonSerializer.Serialize(req, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation($"Request body: {serializedRequest}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .Where(v => v.Errors.Any())
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                string errorMessages = string.Join("; ", errors);
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = errorMessages
                });
            }

            if (!partners.TryGetValue(req.partnerrefno, out var partner) || partner.Key != req.partnerkey)
            {
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                });
            }

            var pass = Encoding.UTF8.GetString(Convert.FromBase64String(req.partnerpassword));
            if (pass != partner.Password)
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                });

            if (!DateTime.TryParse(req.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timeReq))
            {
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Invalid timestamp format"
                });
            }
            var now = DateTime.UtcNow;

            if (Math.Abs((now - timeReq).TotalMinutes) > 5)
            {
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Expired."
                });
            }

            var sigTimestamp = timeReq.ToString("yyyyMMddHHmmss");
            var getSig = $"{sigTimestamp}{req.partnerkey}{req.partnerrefno}{req.totalamount}{req.partnerpassword}";
            byte[] sha = SHA256.HashData(Encoding.UTF8.GetBytes(getSig));
            var hexa = BitConverter.ToString(sha).Replace("-", "").ToLowerInvariant();
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(hexa));

            if (base64 != req.sig.Trim())
            {
                return Ok(new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                });
            }

            if (req.items != null && req.items.Any())
            {
                long sumItems = 0;
                foreach (var item in req.items)
                {
                    if (item.qty < 1 || item.qty > 5)
                        return Ok(new TrxMessageResponseModel
                        {
                            result = 0,
                            resultmessage = "qty must be between 1 and 5."
                        });
                    if (item.unitprice < 1)
                        return Ok(new TrxMessageResponseModel
                        {
                            result = 0,
                            resultmessage = "unitprice must be positive."
                        });

                    sumItems += item.qty * item.unitprice;
                }

                if (sumItems != req.totalamount)
                {
                    return Ok(new TrxMessageResponseModel
                    {
                        result = 0,
                        resultmessage = "Invalid Total Amount."
                    });
                }
            }

            var discountRate = Helper.DiscountCalc.GetDiscountRate(req.totalamount);
            var totalDiscount = (long)(req.totalamount * discountRate);
            var finalAmount = req.totalamount - totalDiscount;

            //Success boi
            return Ok(new TrxMessageResponseModel
            {
                result = 1,
                totalamount = req.totalamount,
                totaldiscount = totalDiscount,
                finalamount = finalAmount
            });
        }
    }
}
