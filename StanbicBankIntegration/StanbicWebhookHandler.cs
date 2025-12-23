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
                PXTrace.WriteError($"{Messages.ErrorDuringWebhookProcessing}: {ex.Message}");
                // Return 500 to the sender so they know to retry
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        private async Task ProcessRequest(WebhookContext context, CancellationToken cancellationToken)
        {
            string json;
            using (var reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

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
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(LogMessages.DeserializationError, ex.Message);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Parse Amount and Currency (Expected: "KES 100.00")
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
            }

            // Note: Add security verification here later (header check + hash validation)

            // Establish Login Scope to allow DB persistence
            // 'admin' should be a valid user in the target tenant
            using (new PXLoginScope("admin"))
            {
                var graph = PXGraph.CreateInstance<PXGraph>();
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
                graph.Caches[typeof(StanbicBankTxn)].Insert(txn);
                // Persist the data to the custom table
                graph.Persist();
            }

            PXTrace.WriteInformation(LogMessages.ProcessingSuccess, payload.TransID);
            context.Response.StatusCode = StatusCodes.Status200OK;
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