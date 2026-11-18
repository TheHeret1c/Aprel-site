using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Favourite
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("favorite_id")]
        public int FavoriteId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("product_id")]
        public string ProductID { get; set; }

        [Column("date_add")]
        public DateTime DateAdd { get; set; }

        [JsonIgnore]
        public Customer Customer { get; set; }
    }
}
