namespace CheZa_Core.Models
{
    public class AddressViewModel
    {
        public int addressId { get; set; }
        public string streetAddress { get; set; }
        public string house {  get; set; }
        public string city { get; set; }
        public int? floor { get; set; }
        public int? entrance { get; set; }
        public int? apartment { get; set; }
        public string? comment { get; set; }
        public bool isEdit { get; set; } // Новый флаг для определения редактирования
    }
}
