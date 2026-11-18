using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("order_item_id")]
        public int OrderItemId { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("product_id")]
        public string ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [JsonIgnore]
        public Order Order { get; set; }
        [JsonIgnore]
        public ICollection<OrderItemModifier> OrderItemModifier { get; set; }
    }
}
