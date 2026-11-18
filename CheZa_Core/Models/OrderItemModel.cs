namespace CheZa_Core.Models
{
    public class OrderItemModel
    {
        public string ProductId {  get; set; }
        public List<string> SelectedModifiersId { get; set; }
        public double TotalPrice { get; set; }
        public string Comment { get; set; }

        public OrderItemModel(string productId, List<string> selectedModifiersId, double totalPrice, string comment)
        {
            ProductId = productId;
            SelectedModifiersId = selectedModifiersId;
            TotalPrice = totalPrice;
            Comment = comment;
        }
    }
}
