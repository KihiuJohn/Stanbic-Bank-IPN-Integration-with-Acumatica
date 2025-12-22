// LogMessages.cs
using PX.Common;

namespace StanbicBankIntegration
{
    //[PXInternalUseOnly]
    public static class LogMessages
    {
        public const string OperationByClassAndMethod = "{Class}.{Method}";
        public const string InvalidPayload = "Invalid or empty payload received for webhook.";
        public const string DeserializationError = "Failed to deserialize payload: {ErrorMessage}";
        public const string ProcessingSuccess = "Webhook processed successfully for TransID: {TransID}";
        public const string ProcessingError = "Error processing webhook: {ErrorMessage}";
    }
}