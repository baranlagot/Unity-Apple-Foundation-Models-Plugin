namespace Baran.AppleFoundationModels
{
    public sealed class AppleFoundationModelsResult
    {
        private AppleFoundationModelsResult(
            string text,
            string rawResponse,
            bool isSuccess,
            string errorMessage)
        {
            Text = text ?? string.Empty;
            RawResponse = rawResponse ?? string.Empty;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string Text { get; }

        public string RawResponse { get; }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static AppleFoundationModelsResult Success(string text, string rawResponse = null)
        {
            return new AppleFoundationModelsResult(text, rawResponse ?? text, true, string.Empty);
        }

        public static AppleFoundationModelsResult Failure(string errorMessage, string rawResponse = null)
        {
            return new AppleFoundationModelsResult(string.Empty, rawResponse, false, errorMessage);
        }
    }
}
