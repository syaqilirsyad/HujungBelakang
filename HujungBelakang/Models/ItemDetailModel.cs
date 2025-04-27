using System.ComponentModel.DataAnnotations;

namespace HujungBelakang.Models
{
    public class ItemDetailModel
    {
        [Required(ErrorMessage = "partneritemref is required.")]
        [MaxLength(50)]
        public string partneritemref { get; set; } = null!;

        [Required(ErrorMessage = "name is required.")]
        [MaxLength(100)]
        public string name { get; set; } = null!;

        public int qty { get; set; }

        public long unitprice { get; set; }
    }
}
