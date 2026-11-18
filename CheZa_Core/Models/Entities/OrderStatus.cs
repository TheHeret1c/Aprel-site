using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class OrderStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("status_name")]
        public string StatusName { get; set; }

        [JsonIgnore]
        public ICollection<Order> Order { get; set; }
    }
}
