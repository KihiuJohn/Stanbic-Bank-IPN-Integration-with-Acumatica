// Messages.cs
using PX.Common;

namespace StanbicBankIntegration
{
    [PXLocalizable]
    public static class Messages
    {
        public const string WebhookReceivedWhileFeatureDisabled = "The webhook event was received while a required feature was disabled.";
        public const string ErrorDuringWebhookProcessing = "An error occurred during processing of the webhook event.";
        public const string InvalidPayloadFormat = "Invalid or empty payload received.";
        public const string DeserializationFailed = "Failed to deserialize webhook payload.";
    }
}