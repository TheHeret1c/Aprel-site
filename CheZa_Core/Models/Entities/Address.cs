using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("address_id")]
        public int AddressId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("street_address")]
        public string StreetAddress { get; set; }

        [Column("city_id")]
        public int CityId { get; set; }

        [Column("entrance")]
        public int? Entrance {  get; set; }

        [Column("floor")]
        public int? Floor { get; set; }

        [Column("apartment")]
        public int? Apartment {  get; set; }

        [Column("house")]
        public string? House {  get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [JsonIgnore]
        public Customer Customer { get; set; }
        [JsonIgnore]
        public City City { get; set; }
        [JsonIgnore]
        public ICollection<Order> Order { get; set; }
    }
}
