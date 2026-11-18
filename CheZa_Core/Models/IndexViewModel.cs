using CheZa_Core.Models.Entities;
namespace CheZa_Core.Models
{
    public class IndexViewModel
    {
        public MenuResponseModel menu {  get; set; }
        public CartViewModel cart { get; set; }
        public string userName { get; set; }
        public List<string> favoriteProductIds { get; set; }
        public List<Review> reviews { get; set; }
    }
}
