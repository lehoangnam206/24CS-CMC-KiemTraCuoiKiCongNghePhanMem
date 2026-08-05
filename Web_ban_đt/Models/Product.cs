using System.ComponentModel.DataAnnotations;

namespace TechStoreWeb.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }

        [Display(Name = "Giá vốn (VNĐ)")]
        [Range(0, 999999999999, ErrorMessage = "Giá vốn phải là số không âm.")]
        public decimal? CostPrice { get; set; }
        public string ImageUrl { get; set; }
        public int Stock { get; set; } = 100;
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}