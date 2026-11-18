using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("review_id")]
        public int? ReviewId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("order_id")]
        public int? OrderId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("review_text")]
        public string ReviewText { get; set; }

        [Column("review_date")]
        public DateTime ReviewDate { get; set; }


        [JsonIgnore]
        public Customer Customer { get; set; }
        [JsonIgnore]
        public Order Order { get; set; }
    }
}
