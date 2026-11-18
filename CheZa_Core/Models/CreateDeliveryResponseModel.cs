namespace CheZa_Core.Models
{
    public class CreateDeliveryResponseModel
    {
        public string correlationId {  get; set; }

    }

    public class CreateDeliveryOrderInfo
    {
        public string id { get; set; }
        public string organizationId { get; set; }
        public Int64 timestamp { get; set; }
        public string creationStatus { get; set; }
        public CreateDeliveryErrorInfo errorInfo { get; set; }
    }

    public class CreateDeliveryErrorInfo
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
