using CheZa_Core.Models.Entities;
namespace CheZa_Core.Models
{
    public class ProfileViewModel
    {
        public User user {  get; set; }
        public List<Order> orders { get; set; }
        public List<Address> addresses { get; set; }
        public int selectAddress { get; set; }
        public List<string> favourites { get; set; }
        public MenuResponseModel menu { get; set; }
    }
}
