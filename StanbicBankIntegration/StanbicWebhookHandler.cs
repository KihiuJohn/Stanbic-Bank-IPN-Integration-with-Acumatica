using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PX.Data;
using PX.Common;
using PX.Api.Webhooks;
using PX.Objects.GL;
using System.Linq;

namespace StanbicBankIntegration
{
    public class StanbicWebhookHandler : IWebhookHandler
    {
        public StanbicWebhookHandler() { }

        public async Task HandleAsync(WebhookContext context, CancellationToken cancellationToken)
        {
            string jsonBody = string.Empty;
            try
            {
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    jsonBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonBody))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var result = await Task.Run(() => ProcessWebhook(jsonBody), cancellationToken);

                context.Response.StatusCode = StatusCodes.Status200OK;
                using (var sw = new StreamWriter(context.Response.Body, Encoding.UTF8))
                {
                    await sw.WriteAsync(result);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                using (var sw = new StreamWriter(context.Response.Body, Encoding.UTF8))
                {
                    await sw.WriteAsync($"Error: {ex.Message}");
                }
            }
        }

        private string ProcessWebhook(string json)
        {
            // Ensure 'Company' matches the Tenant Name in the login screen
            string loginUser = "gibbs@Company";

            using (new PXLoginScope(loginUser))
            {
                // This sets the context so Acumatica knows which CompanyID to use automatically
                PXContext.SetScreenID("SM304000");

                try
                {
                    StanbicPayload payload = JsonConvert.DeserializeObject<StanbicPayload>(json);
                    if (payload == null || string.IsNullOrEmpty(payload.TransID))
                        return "Error: Payload is empty or TransID is missing.";

                    // Parsing the Amount (handling "KES 100.00" format)
                    decimal parsedAmount = 0;
                    string currency = "KES";
                    if (!string.IsNullOrEmpty(payload.TransAmount))
                    {
                        var parts = payload.TransAmount.Split(' ');
                        if (parts.Length > 1)
                        {
                            currency = parts[0];
                            decimal.TryParse(parts[1].Replace(",", ""), out parsedAmount);
                        }
                        else
                        {
                            decimal.TryParse(parts[0].Replace(",", ""), out parsedAmount);
                        }
                    }

                    // PXDatabase.Insert will AUTOMATICALLY add CompanyID based on the 'gibbs@Company' login.
                    // DO NOT add CompanyID here or you will get the "Specified more than once" error.
                    PXDatabase.Insert<StanbicBankTxn>(
                        new PXDataFieldAssign<StanbicBankTxn.transID>(payload.TransID),
                        new PXDataFieldAssign<StanbicBankTxn.transactionType>(payload.TransactionType),
                        new PXDataFieldAssign<StanbicBankTxn.transTime>(payload.TransTime),
                        new PXDataFieldAssign<StanbicBankTxn.transAmount>(parsedAmount),
                        new PXDataFieldAssign<StanbicBankTxn.currency>(currency),
                        new PXDataFieldAssign<StanbicBankTxn.businessShortCode>(payload.BusinessShortCode),
                        new PXDataFieldAssign<StanbicBankTxn.businessAccountNo>(payload.BusinessAccountNo),
                        new PXDataFieldAssign<StanbicBankTxn.billRefNumber>(payload.BillRefNumber),
                        new PXDataFieldAssign<StanbicBankTxn.invoiceNumber>(payload.InvoiceNumber),
                        new PXDataFieldAssign<StanbicBankTxn.orgAccountBalance>(payload.OrgAccountBalance),
                        new PXDataFieldAssign<StanbicBankTxn.availableAccountBalance>(payload.AvailableAccountBalance),
                        new PXDataFieldAssign<StanbicBankTxn.thirdPartyTransID>(payload.ThirdPartyTransID),
                        new PXDataFieldAssign<StanbicBankTxn.mSISDN>(payload.MSISDN),
                        new PXDataFieldAssign<StanbicBankTxn.paymentDetails>(payload.FirstName + " " + payload.LastName),
                        new PXDataFieldAssign<StanbicBankTxn.secureHash>(payload.secureHash),
                        new PXDataFieldAssign<StanbicBankTxn.rawPayload>(json),
                        new PXDataFieldAssign<StanbicBankTxn.status>("New"),
                        new PXDataFieldAssign<StanbicBankTxn.createdByScreenID>("SM304000"),
                        new PXDataFieldAssign<StanbicBankTxn.createdDateTime>(DateTime.UtcNow)
                    );

                    // Verification
                    var row = PXDatabase.SelectSingle<StanbicBankTxn>(
                        new PXDataField("TransID"),
                        new PXDataFieldValue("TransID", payload.TransID));

                    if (row != null)
                        return $"Success: Trans {payload.TransID} is now in the database.";
                    else
                        return "Error: Insert appeared to work but record is not visible. Verify Tenant name 'Company' is correct.";
                }
                catch (Exception ex)
                {
                    return $"Final Error: {ex.Message}";
                }
            }
        }

        // Ensure your Payload class matches the Stanbic JSON structure
        public class StanbicPayload
        {
            public string TransactionType { get; set; }
            public string TransID { get; set; }
            public string TransTime { get; set; }
            public string TransAmount { get; set; }
            public string BusinessShortCode { get; set; }
            public string BusinessAccountNo { get; set; }
            public string BillRefNumber { get; set; }
            public string InvoiceNumber { get; set; }
            public string OrgAccountBalance { get; set; }
            public string AvailableAccountBalance { get; set; }
            public string ThirdPartyTransID { get; set; }
            public string MSISDN { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string secureHash { get; set; }
        }
    }
}
