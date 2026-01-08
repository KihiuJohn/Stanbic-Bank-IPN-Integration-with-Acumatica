using PX.Common;

namespace StanbicBankIntegration
{
    [PXLocalizable]
    public static class Messages
    {
        public const string CustomerRequired = "Customer ID is required to process the payment.";
        public const string BatchError = "Some transactions could not be processed.";
        public const string ProcessedSuccess = "Payment Created Successfully.";
        public const string MissingFilterParams = "Please select a Cash Account and Payment Method in the Selection settings.";

        // Webhook specific
        public const string InvalidPayload = "Invalid or empty payload received.";
        public const string DeserializationError = "Failed to deserialize payload: {0}";
    }
}