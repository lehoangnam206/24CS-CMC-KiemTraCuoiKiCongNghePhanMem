using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStoreWeb.Models
{
    [Table("PromotionProducts")]
    public class PromotionProduct
    {
        [Key]
        public int Id { get; set; }

        public int PromotionId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPrice { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public Promotion Promotion { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }
    }
}
