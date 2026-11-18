using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("role_name")]
        public string RoleName { get; set; }

        // Ссылка на User для связи
        [JsonIgnore]
        public ICollection<User> User { get; set; }
    }
}
