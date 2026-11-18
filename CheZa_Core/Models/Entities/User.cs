using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("phone")]
        public string? PhoneNumber { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("date_birthday")]
        public DateTime? DateBirthday { get; set; }

        [JsonIgnore]
        public Role Role {  get; set; }
        [JsonIgnore]
        public Customer Customer { get; set; }
        [JsonIgnore]
        public Admin Admin { get; set; }
    }
}
