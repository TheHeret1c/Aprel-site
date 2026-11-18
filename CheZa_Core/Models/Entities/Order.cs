using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CheZa_Core.Models.Entities
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("customer_id")]
        public int CustomerId {  get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("payment_type_id")]
        public int PaymentTypeId { get; set; }

        [Column("total_amount")]
        public double TotalAmount { get; set; }

        [Column("address_id")]
        public int AddressId { get; set; }


        [JsonIgnore]
        public Customer Customer { get; set; }
        [JsonIgnore]
        public OrderStatus OrderStatus { get; set; }
        [JsonIgnore]
        public PaymentType PaymentType { get; set; }
        [JsonIgnore]
        public Review Review { get; set; }
        [JsonIgnore]
        public ICollection<OrderItem> OrderItem { get; set; }
        [JsonIgnore]
        public Address Address { get; set; }
    }
}
