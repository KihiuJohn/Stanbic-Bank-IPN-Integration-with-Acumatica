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
                    await WriteResponseAsync(context.Response.Body, "Error: Empty request body"); // Updated to use Stream
                    return;
                }

                var result = await Task.Run(() => ProcessWebhook(jsonBody), cancellationToken);

                context.Response.StatusCode = StatusCodes.Status200OK;
                await WriteResponseAsync(context.Response.Body, result); // Updated to use Stream
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await WriteResponseAsync(context.Response.Body, $"Error: {ex.Message}"); // Updated to use Stream
            }
        }

        private async Task WriteResponseAsync(Stream responseStream, string message) // Updated parameter type
        {
            using (var sw = new StreamWriter(responseStream, Encoding.UTF8, 1024, leaveOpen: true)) // Added bufferSize argument
            {
                await sw.WriteAsync(message);
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

                    if (string.IsNullOrWhiteSpace(payload?.TransID))
                    {
                        WriteLog("SYSTEM", "ERROR", "TransID is missing from payload");
                        return "Error: TransID is required";
                    }

                    // CRITICAL: Check for duplicate BEFORE inserting
                    var existing = PXDatabase.SelectSingle<StanbicBankTxn>(
                        new PXDataField("TransID"),
                        new PXDataFieldValue("TransID", payload.TransID));

                    if (existing != null)
                    {
                        WriteLog(payload.TransID, "INFO", $"Duplicate ignored: {payload.TransID}");
                        return $"Success: Transaction {payload.TransID} already exists (ignored duplicate).";
                    }

                    // Parse amount and currency
                    decimal parsedAmount = 0;
                    string currency = "KES";

                    if (!string.IsNullOrEmpty(payload.TransAmount))
                    {
                        var parts = payload.TransAmount.Split(' ');
                        if (parts.Length > 1)
                        {
                            currency = parts[0].Trim();
                            decimal.TryParse(parts[1].Replace(",", "").Replace(" ", ""), out parsedAmount);
                        }
                        else
                        {
                            decimal.TryParse(parts[0].Replace(",", "").Replace(" ", ""), out parsedAmount);
                        }
                    }

                    // CRITICAL FIX: Generate NoteID to avoid NULL duplicate issues
                    Guid noteID = Guid.NewGuid();

                    // Insert transaction record
                    PXDatabase.Insert<StanbicBankTxn>(
                        new PXDataFieldAssign<StanbicBankTxn.transID>(payload.TransID),
                        new PXDataFieldAssign<StanbicBankTxn.noteID>(noteID),  // CRITICAL: Always generate NoteID
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
                        new PXDataFieldAssign<StanbicBankTxn.secureHash>(payload.secureHash),
                        new PXDataFieldAssign<StanbicBankTxn.rawPayload>(json),
                        new PXDataFieldAssign<StanbicBankTxn.status>("New"),
                        new PXDataFieldAssign<StanbicBankTxn.createdDateTime>(PX.Common.PXTimeZoneInfo.UtcNow),
                        new PXDataFieldAssign<StanbicBankTxn.createdByScreenID>("SM304000")
                    );

                    // Log success
                    WriteLog(payload.TransID, "SUCCESS",
                        $"Recorded payment: {currency} {parsedAmount:N2} for {payload.BillRefNumber}");

                    return $"Success: Transaction {payload.TransID} saved successfully.";
                }
                catch (Exception ex)
                {
                    WriteLog("SYSTEM", "ERROR", $"Processing failed: {ex.Message}", ex.ToString());
                    return $"Error: {ex.Message}";
                }
            }
        }

        private void WriteLog(string transID, string level, string message, string exception = null)
        {
            try
            {
                PXDatabase.Insert<StanbicWebhookLog>(
                    new PXDataFieldAssign<StanbicWebhookLog.transID>(transID),
                    new PXDataFieldAssign<StanbicWebhookLog.logLevel>(level),
                    new PXDataFieldAssign<StanbicWebhookLog.message>(message),
                    new PXDataFieldAssign<StanbicWebhookLog.exception>(exception),
                    new PXDataFieldAssign<StanbicWebhookLog.eventTime>(DateTime.Now),
                    new PXDataFieldAssign<StanbicWebhookLog.createdByScreenID>("SM304000"),
                    new PXDataFieldAssign<StanbicWebhookLog.createdDateTime>(PX.Common.PXTimeZoneInfo.UtcNow)
                );
            }
            catch
            {
                // Swallow logging errors to prevent cascade failures
            }
        }

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