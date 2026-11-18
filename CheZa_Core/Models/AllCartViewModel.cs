namespace CheZa_Core.Models
{
    public class AllCartViewModel
    {
        public CartViewModel cart { get; set; }
        public MenuResponseModel menu { get; set; }
        public double priceDelivery { get; set; } = 0;
        public double avgTimeDelivery { get; set; }

        public AllCartViewModel(CartViewModel cart, MenuResponseModel menu)
        {
            this.cart = cart;
            this.menu = menu;
        }
    }
}
