using System;

namespace TechStoreWeb.Models
{
    public class CartItem
    {
        public string Id { get; set; }

        public int ProductId { get; set; }

        public int? VariantId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public string Img { get; set; }
        public int Qty { get; set; } = 1;
        public bool Selected { get; set; } = true;

        public static string BuildId(int productId, int? variantId)
        {
            return variantId.HasValue ? $"{productId}-v{variantId.Value}" : productId.ToString();
        }
    }
}
