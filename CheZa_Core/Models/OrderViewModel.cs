using CheZa_Core.Models.Entities;

namespace CheZa_Core.Models
{
    public class OrderViewModel
    {
        //public List<ProductDetailsViewModel> order {  get; set; }
        //public int orderId { get; set; }

        public int orderId {  get; set; }
        public double totalAmount { get; set; }
        public List<ProductOrderViewModel> products { get; set; }
        public bool haveReview {  get; set; }
    }
}
