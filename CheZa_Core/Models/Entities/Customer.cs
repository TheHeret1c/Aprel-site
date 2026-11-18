using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("loyality_card")]
        public string? LoyalityCard { get; set; }

        [Column("additional_info")]
        public string? AdditionalInfo { get; set; }

        [JsonIgnore]
        public User User { get; set; }
        [JsonIgnore]
        public ICollection<Review> Review { get; set; }
        [JsonIgnore]
        public ICollection<Favourite> Favourite { get; set; }
        [JsonIgnore]
        public ICollection<Address> Address { get; set; }
        [JsonIgnore]
        public ICollection<Order> Order { get; set; }
    }
}
