namespace TechStoreWeb.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public ProductDetail Detail { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<Review> Reviews { get; set; }
        public bool CanReview { get; set; }

        public int AvailableStock =>
            Variants != null && Variants.Count > 0
                ? Variants.Sum(v => v.Stock)
                : Product?.Stock ?? 0;
    }

    public class RatingSummary
    {
        public double Average { get; set; }
        public int Count { get; set; }
    }
}
