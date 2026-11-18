using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Admin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("admin_id")]
        public int AdminId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("additional_info")]
        public string AdditionalInfo { get; set; }

        [JsonIgnore]
        public User User { get; set; }
    }
}
