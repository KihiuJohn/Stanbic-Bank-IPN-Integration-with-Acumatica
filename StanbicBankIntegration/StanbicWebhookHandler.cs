using System;
using System.IO;
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
        // Parameterless constructor is mandatory for the Webhooks screen to discover the class
        public StanbicWebhookHandler() { }

        public async Task HandleAsync(WebhookContext context, CancellationToken cancellationToken)
        {
            try
            {
                await ProcessRequest(context, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error to Acumatica Trace (SM204520)
                PXTrace.WriteError($"{Messages.ErrorDuringWebhookProcessing}: {ex.Message}");

                // Return 500 to the sender so they know to retry
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        private async Task ProcessRequest(WebhookContext context, CancellationToken cancellationToken)
        {
            PXTrace.WriteInformation("Starting webhook processing...");

            string json;
            using (var reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }
            PXTrace.WriteInformation($"Received payload (length: {json.Length}): {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                PXTrace.WriteWarning(LogMessages.InvalidPayload);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            StanbicPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<StanbicPayload>(json);
                PXTrace.WriteInformation("Payload deserialized successfully.");
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(LogMessages.DeserializationError, ex.Message);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Parse Amount and Currency
            string currency = "KES";
            decimal? amount = null;
            if (!string.IsNullOrEmpty(payload.TransAmount))
            {
                var parts = payload.TransAmount.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    currency = parts[0];
                    if (decimal.TryParse(parts[1].Replace(",", ""), out decimal parsed)) amount = parsed;
                }
                else if (decimal.TryParse(payload.TransAmount.Replace(",", ""), out decimal parsed))
                {
                    amount = parsed;
                }
                PXTrace.WriteInformation($"Parsed amount: {amount}, currency: {currency}");
            }

            try
            {
                PXTrace.WriteInformation("Entering login scope with user 'admin'...");
                using (new PXLoginScope("admin")) // Replace with a REAL admin user if "admin" fails
                {
                    PXTrace.WriteInformation("Login scope entered successfully.");
                    var graph = PXGraph.CreateInstance<PXGraph>();
                    PXTrace.WriteInformation("PXGraph created.");

                    var txn = new StanbicBankTxn
                    {
                        TransID = payload.TransID,
                        TransactionType = payload.TransactionType,
                        TransTime = payload.TransTime,
                        TransAmount = amount,
                        Currency = currency,
                        MSISDN = payload.MSISDN,
                        BillRefNumber = payload.BillRefNumber,
                        ThirdPartyTransID = payload.ThirdPartyTransID,
                        SecureHash = payload.secureHash,
                        RawPayload = json,
                        Status = "New"
                    };
                    PXTrace.WriteInformation("Transaction object created.");

                    graph.Caches[typeof(StanbicBankTxn)].Insert(txn);
                    PXTrace.WriteInformation("Transaction inserted into cache.");

                    graph.Persist();
                    PXTrace.WriteInformation("Persist() called successfully.");
                }
                PXTrace.WriteInformation(LogMessages.ProcessingSuccess, payload.TransID);
                context.Response.StatusCode = StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(LogMessages.ProcessingError, ex.Message + "\n" + ex.StackTrace);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
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
        public string PaymentDetails { get; set; }
        public string CallbackUrl { get; set; }
        public string apiClientId { get; set; }
        public string ApiSecret { get; set; }
        public object dealReference { get; set; }
        public string ApiKey { get; set; }
        public string secureHash { get; set; }
    }
}