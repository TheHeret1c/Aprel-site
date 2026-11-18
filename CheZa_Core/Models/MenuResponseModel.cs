namespace CheZa_Core.Models
{
    public class MenuResponseModel
    {
        public List<Product> _products { get; set; } = new List<Product>();
        public List<string> _categories { get; set; } = new List<string>();

        public MenuResponseModel(List<Product> products, List<string> categories)
        {
            _products = products;
            _categories = categories;
        }

        public MenuResponseModel() { }
    }
}
