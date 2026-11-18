using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class OrderItemModifier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("order_item_modifier_id")]
        public int OrderItemModifierId { get; set; }

        [Column("order_item_id")]
        public int OrderItemId { get; set; }

        [Column("modifier_id")]
        public string ModifierId { get; set; }

        [JsonIgnore]
        public OrderItem OrderItem { get; set; }
    }
}
