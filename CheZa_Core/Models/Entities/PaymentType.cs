using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class PaymentType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("payment_type_id")]
        public int PaymentTypeId { get; set; }

        [Column("payment_type_name")]
        public string PaymentTypeName { get; set; }

        [JsonIgnore]
        public ICollection<Order> Order { get; set; }
    }
}
