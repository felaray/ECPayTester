using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Numerics;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;

namespace ECPayTester.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [ViewData]
        public string merchantID { get; set; }

        
        [ViewData]
        public string tradeNo { get; set; }
        [ViewData]
        public string tradeDate { get; set; }
        [ViewData]
        public string tradeDesc { get; set; }
        [ViewData]
        public string itemName { get; set; }
        [ViewData]
        public string returnUrl { get; set; }
        [ViewData]
        public decimal amt { get; set; }
        [ViewData]
        public string orderResultURL { get; set; }
        [ViewData]
        public string checkMacValue { get; set; }


        readonly string _endpoint, _hashKey, _hashIV, _merchantID;

        public IndexModel(IConfiguration configuration )
        {
            _endpoint = configuration["ECPay:Endpoint"];
            _hashKey = configuration["ECPay:HashKey"];
            _hashIV = configuration["ECPay:HashIV"];
            _merchantID = configuration["ECPay:MerchantID"];
        }


        public async void OnGet()
        {
            merchantID = "2000132";

            tradeNo = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            tradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            tradeDesc = "Order note";
            itemName = $"Order Item Name";
            var endpoint = "https://payment-stage.ecpay.com.tw";
            returnUrl = endpoint + "/api/ecPay/confirm"; //測試
            orderResultURL = $"{endpoint}/index";
            amt = 10000;
            checkMacValue =GetCheckMacValue(tradeNo, tradeDate, tradeDesc, itemName, returnUrl, amt, orderResultURL);
        }

        public string GetCheckMacValue(string tradeNo, string tradeDate, string tradeDesc, string itemName, string returnUrl, decimal amt, string orderResultURL)
        {
            var parameters = new Dictionary<string, string> {
                {"TradeDesc",tradeDesc },
                {"PaymentType","aio" },
                {"MerchantTradeDate", tradeDate },
                {"MerchantTradeNo", tradeNo},
                {"MerchantID",_merchantID },
                {"ReturnURL",returnUrl },
                {"ItemName",itemName },
                {"TotalAmount",$"{amt}" },
                //當付款方式[ChoosePayment]為 ALL
                //時，可隱藏不需要的付款方式，多筆請
                //以井號分隔(#)。
                //可用的參數值：
                //Credit:信用卡
                //WebATM:網路 ATM
                //ATM:自動櫃員機
                //CVS:超商代碼
                //BARCODE:超商條碼
                {"ChoosePayment","ALL" },
                {"EncryptType","1"},

                ////此參數為付款完成後同時開立電子發票
                //{"InvoiceMark","1"},
                ////統一編號
                //{"CustomerIdentifier","11111111"},              
                ////
                //{"CustomerName ","Test "},
                //{"RelateNumber",tradeNo},
                //{"TaxType","1"},
                //{"Donation","0"},
                //{"Print","1"},
                //{"InvoiceItemName",itemName },
                //{"InvoiceItemCount","1"},
                //{"InvoiceItemWord","1"},
                //{"InvoiceItemPrice",$"{amt}" },
                //{"DelayDay","0"},
                //{"InvType","07"},
                {"OrderResultURL", orderResultURL},
            };
            var source = string.Join("&", parameters.OrderBy(c => c.Key).Select(c => string.Format("{0}={1}", c.Key, c.Value)));
            var checkMacValue = BuildCheckMacValue(source);
            return checkMacValue;
        }

        private string BuildCheckMacValue(string parameters)
        {
            string szCheckMacValue = String.Empty;
            szCheckMacValue = String.Format("HashKey={0}&{1}&HashIV={2}", _hashKey, parameters, _hashIV);
            szCheckMacValue = HttpUtility.UrlEncode(szCheckMacValue).ToLower();
            using (SHA256 mySHA256 = SHA256.Create())
            {
                var byteArr = Encoding.UTF8.GetBytes(szCheckMacValue);
                byte[] hashValue = mySHA256.ComputeHash(byteArr);
                return szCheckMacValue = BitConverter.ToString(hashValue).Replace("-", "").ToUpper();
            }
        }
    }
}