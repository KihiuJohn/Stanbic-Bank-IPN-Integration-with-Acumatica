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
            string loginUser = "gibbs@Company";

            using (new PXLoginScope(loginUser))
            {
                PXContext.SetScreenID("SM304000");

                try
                {
                    StanbicPayload payload = JsonConvert.DeserializeObject<StanbicPayload>(json);

                    // 1. DUPLICATE CHECK (Prevents the PK Violation Error)
                    var existing = PXDatabase.SelectSingle<StanbicBankTxn>(
                        new PXDataField("TransID"),
                        new PXDataFieldValue("TransID", payload.TransID));

                    if (existing != null)
                    {
                        WriteLog(payload.TransID, "INFO", $"Duplicate ignored: {payload.TransID}");
                        return $"Success: Transaction {payload.TransID} already exists.";
                    }

                    // 2. PARSE DATA
                    decimal parsedAmount = 0;
                    string currency = "KES";
                    if (!string.IsNullOrEmpty(payload.TransAmount))
                    {
                        var parts = payload.TransAmount.Split(' ');
                        if (parts.Length > 1) { currency = parts[0]; decimal.TryParse(parts[1].Replace(",", ""), out parsedAmount); }
                        else { decimal.TryParse(parts[0].Replace(",", ""), out parsedAmount); }
                    }

                    // 3. DIRECT INSERT TRANSACTION
                    PXDatabase.Insert<StanbicBankTxn>(
                        new PXDataFieldAssign<StanbicBankTxn.transID>(payload.TransID),
                        new PXDataFieldAssign<StanbicBankTxn.transactionType>(payload.TransactionType),
                        new PXDataFieldAssign<StanbicBankTxn.transAmount>(parsedAmount),
                        new PXDataFieldAssign<StanbicBankTxn.currency>(currency),
                        new PXDataFieldAssign<StanbicBankTxn.billRefNumber>(payload.BillRefNumber),
                        new PXDataFieldAssign<StanbicBankTxn.mSISDN>(payload.MSISDN),
                        new PXDataFieldAssign<StanbicBankTxn.rawPayload>(json),
                        new PXDataFieldAssign<StanbicBankTxn.status>("New"),
                        new PXDataFieldAssign<StanbicBankTxn.createdDateTime>(DateTime.UtcNow)
                    );

                    // 4. DIRECT INSERT LOG
                    WriteLog(payload.TransID, "SUCCESS", $"Recorded payment for {payload.BillRefNumber}");

                    return $"Success: Trans {payload.TransID} saved.";
                }
                catch (Exception ex)
                {
                    WriteLog("SYSTEM", "ERROR", ex.Message);
                    return $"Error: {ex.Message}";
                }
            }
        }

        // Helper method to ensure logs save regardless of graph state
        private void WriteLog(string transID, string level, string message, string exception = null)
        {
            // Direct SQL insert to bypass any transaction rollbacks
            PXDatabase.Insert<StanbicWebhookLog>(
                new PXDataFieldAssign<StanbicWebhookLog.transID>(transID),
                new PXDataFieldAssign<StanbicWebhookLog.logLevel>(level),
                new PXDataFieldAssign<StanbicWebhookLog.message>(message),
                new PXDataFieldAssign<StanbicWebhookLog.exception>(exception),
                new PXDataFieldAssign<StanbicWebhookLog.eventTime>(DateTime.Now),
                new PXDataFieldAssign<StanbicWebhookLog.createdByScreenID>("SM304000"),
                new PXDataFieldAssign<StanbicWebhookLog.createdDateTime>(DateTime.UtcNow)
            );
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
