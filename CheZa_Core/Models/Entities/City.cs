using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("city_id")]
        public int CityId { get; set; }

        [Column("city_name")]
        public string CityName { get; set; }

        [JsonIgnore]
        public ICollection<Address> Address { get; set; }
    }
}
