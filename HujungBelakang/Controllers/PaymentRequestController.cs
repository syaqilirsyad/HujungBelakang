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
        private void LogResponse(TrxMessageResponseModel response)
        {
            var serializedResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogTrace($"Response body: {serializedResponse}");
        }

        private static readonly Dictionary<string, (string Key, string Password)> partners = new()
        {
            ["FG-00001"] = ("FAKEGOOGLE", "FAKEPASSWORD1234"),
            ["FG-00002"] = ("FAKEPEOPLE", "FAKEPASSWORD4578"),
        };

        [HttpPost]
        public IActionResult Post([FromBody] TrxMessageRequestModel req)
        {
            var encrequest = new TrxMessageRequestModel
            {
                partnerrefno = req.partnerrefno,
                partnerkey = req.partnerkey,
                partnerpassword = Helper.EncyptionPass.EncryptPassword(req.partnerpassword),
                timestamp = req.timestamp,
                totalamount = req.totalamount,
                sig = req.sig,
                items = req.items
            };
            var serializedRequest = JsonSerializer.Serialize(encrequest, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogTrace($"Request body (safe): {serializedRequest}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .Where(v => v.Errors.Any())
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                string errorMessages = string.Join("; ", errors);

                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = errorMessages
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            if (!partners.TryGetValue(req.partnerrefno, out var partner) || partner.Key != req.partnerkey)
            {
                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            var pass = Encoding.UTF8.GetString(Convert.FromBase64String(req.partnerpassword));
            if (pass != partner.Password)
            {
                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            if (!DateTime.TryParse(req.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timeReq))
            {
                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Invalid timestamp format"
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            var now = DateTime.UtcNow;

            if (Math.Abs((now - timeReq).TotalMinutes) > 5)
            {
                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Expired."
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            var sigTimestamp = timeReq.ToString("yyyyMMddHHmmss");
            var getSig = $"{sigTimestamp}{req.partnerkey}{req.partnerrefno}{req.totalamount}{req.partnerpassword}";
            byte[] sha = SHA256.HashData(Encoding.UTF8.GetBytes(getSig));
            var hexa = BitConverter.ToString(sha).Replace("-", "").ToLowerInvariant();
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(hexa));

            if (base64 != req.sig.Trim())
            {
                var errorResponse = new TrxMessageResponseModel
                {
                    result = 0,
                    resultmessage = "Access Denied!"
                };
                LogResponse(errorResponse);
                return Ok(errorResponse);
            }

            if (req.items != null && req.items.Any())
            {
                long sumItems = 0;
                foreach (var item in req.items)
                {
                    if (item.qty < 1 || item.qty > 5)
                    {
                        var errorResponse = new TrxMessageResponseModel
                        {
                            result = 0,
                            resultmessage = "qty must be between 1 and 5."
                        };
                        LogResponse(errorResponse);
                        return Ok(errorResponse);
                    }
                    if (item.unitprice < 1)
                    {
                        var errorResponse = new TrxMessageResponseModel
                        {
                            result = 0,
                            resultmessage = "unitprice must be positive."
                        };
                        LogResponse(errorResponse);
                        return Ok(errorResponse);
                    }

                    sumItems += item.qty * item.unitprice;
                }

                if (sumItems != req.totalamount)
                {
                    var errorResponse = new TrxMessageResponseModel
                    {
                        result = 0,
                        resultmessage = "Invalid Total Amount."
                    };
                    LogResponse(errorResponse);
                    return Ok(errorResponse);
                }
            }

            var discountRate = Helper.DiscountCalc.GetDiscountRate(req.totalamount);
            var totalDiscount = (long)(req.totalamount * discountRate);
            var finalAmount = req.totalamount - totalDiscount;

            var successResponse = new TrxMessageResponseModel
            {
                result = 1,
                totalamount = req.totalamount,
                totaldiscount = totalDiscount,
                finalamount = finalAmount
            };
            LogResponse(successResponse);
            return Ok(successResponse);
        }

    }
}
