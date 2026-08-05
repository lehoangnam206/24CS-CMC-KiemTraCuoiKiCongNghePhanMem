namespace TechStoreWeb.Models
{
    public class CheckoutFormModel
    {
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverEmail { get; set; }

        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? AddressDetail { get; set; }

        public string? AddressType { get; set; }

        public string? Note { get; set; }

        public string? ShippingMethod { get; set; }

        public bool BuyInsurance { get; set; }

        public string? PaymentMethod { get; set; }
    }
}
