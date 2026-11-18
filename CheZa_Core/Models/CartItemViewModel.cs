namespace CheZa_Core.Models
{
    public class CartItemViewModel
    {
        public Product product { get; set; }
        public List<Product> modifiers { get; set; }
        public int quantity { get; set; }
        public string comment { get; set; }
    }
}
