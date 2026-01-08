using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PX.Data;
using PX.Api.Webhooks;
using PX.Common;

namespace StanbicBankIntegration
{
    public class StanbicWebhookHandler : IWebhookHandler
    {
        public async Task HandleAsync(WebhookContext context, CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                string jsonBody = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(jsonBody)) return;

                var result = await Task.Run(() => ProcessWebhook(jsonBody), cancellationToken);

                context.Response.StatusCode = StatusCodes.Status200OK;
                using (var sw = new StreamWriter(context.Response.Body, Encoding.UTF8))
                {
                    await sw.WriteAsync(result);
                }
            }
        }

        private string ProcessWebhook(string json)
        {
            string loginUser = "gibbs@Company"; // Ensure this user exists

            using (new PXLoginScope(loginUser))
            {
                PXContext.SetScreenID("SM304000");
                Guid? currentUserId = PXAccess.GetUserID(); // Get the ID of the logged-in user

                try
                {
                    var payload = JsonConvert.DeserializeObject<StanbicPayload>(json);

                    // 1. DUPLICATE CHECK
                    var existing = PXDatabase.SelectSingle<StanbicBankTxn>(
                        new PXDataField("TransID"),
                        new PXDataFieldValue("TransID", payload.TransID));

                    if (existing != null)
                    {
                        WriteLog(payload.TransID, "INFO", $"Duplicate: {payload.TransID}", null, currentUserId);
                        return $"Success: Duplicate {payload.TransID}";
                    }

                    // 2. PARSE AMOUNT (e.g., "KES 1.00")
                    decimal parsedAmount = 0;
                    string currency = "KES";
                    if (!string.IsNullOrEmpty(payload.TransAmount))
                    {
                        var parts = payload.TransAmount.Split(' ');
                        if (parts.Length > 1)
                        {
                            currency = parts[0];
                            decimal.TryParse(parts[1], out parsedAmount);
                        }
                        else decimal.TryParse(parts[0], out parsedAmount);
                    }

                    // 3. INSERT TRANSACTION
                    PXDatabase.Insert<StanbicBankTxn>(
                        //new PXDataFieldAssign<StanbicBankTxn.companyID>(2),
                        new PXDataFieldAssign<StanbicBankTxn.transID>(payload.TransID),
                        new PXDataFieldAssign<StanbicBankTxn.transactionType>(payload.TransactionType),
                        new PXDataFieldAssign<StanbicBankTxn.transAmount>(parsedAmount),
                        new PXDataFieldAssign<StanbicBankTxn.currency>(currency),
                        new PXDataFieldAssign<StanbicBankTxn.billRefNumber>(payload.BillRefNumber),
                        new PXDataFieldAssign<StanbicBankTxn.mSISDN>(payload.MSISDN),
                        new PXDataFieldAssign<StanbicBankTxn.status>("New"),
                        new PXDataFieldAssign<StanbicBankTxn.rawPayload>(json),
                        new PXDataFieldAssign<StanbicBankTxn.createdByID>(currentUserId),
                        new PXDataFieldAssign<StanbicBankTxn.createdByScreenID>("SM304000"),
                        new PXDataFieldAssign<StanbicBankTxn.createdDateTime>(DateTime.UtcNow)
                    );

                    WriteLog(payload.TransID, "SUCCESS", $"Captured payment for {payload.BillRefNumber}", null, currentUserId);
                    return "Success";
                }
                catch (Exception ex)
                {
                    WriteLog("SYSTEM", "ERROR", ex.Message, ex.StackTrace, currentUserId);
                    return $"Error: {ex.Message}";
                }
            }
        }

        private void WriteLog(string transID, string level, string message, string stackTrace, Guid? userId)
        {
            PXDatabase.Insert<StanbicWebhookLog>(
                //new PXDataFieldAssign<StanbicWebhookLog.companyID>(2),
                new PXDataFieldAssign<StanbicWebhookLog.transID>(transID),
                new PXDataFieldAssign<StanbicWebhookLog.logLevel>(level),
                new PXDataFieldAssign<StanbicWebhookLog.message>(message),
                new PXDataFieldAssign<StanbicWebhookLog.exception>(stackTrace),
                new PXDataFieldAssign<StanbicWebhookLog.eventTime>(DateTime.Now),
                new PXDataFieldAssign<StanbicWebhookLog.createdByID>(userId),
                new PXDataFieldAssign<StanbicWebhookLog.createdByScreenID>("SM304000"),
                new PXDataFieldAssign<StanbicWebhookLog.createdDateTime>(DateTime.UtcNow)
            );
        }

        public class StanbicPayload
        {
            public string TransactionType { get; set; }
            public string TransID { get; set; }
            public string TransAmount { get; set; }
            public string BillRefNumber { get; set; }
            public string MSISDN { get; set; }
            public object dealReference { get; set; } // Handled as object for {}
        }
    }
}