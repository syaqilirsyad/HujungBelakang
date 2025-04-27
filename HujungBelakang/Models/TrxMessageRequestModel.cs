using System.ComponentModel.DataAnnotations;

namespace HujungBelakang.Models
{
    public class TrxMessageRequestModel
    {
        [Required(ErrorMessage = "partnerkey is required.")]
        [MaxLength(50)]
        public string partnerkey { get; set; } = null!;

        [Required(ErrorMessage = "partnerrefno is required.")]
        [MaxLength(50)]
        public string partnerrefno { get; set; } = null!;

        [Required(ErrorMessage = "partnerpassword is required.")]
        [MaxLength(50)]
        public string partnerpassword { get; set; } = null!;

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "totalamount is required and > 0")]
        public long totalamount { get; set; }

        [Required(ErrorMessage = "items is required")]
        public List<ItemDetailModel> items { get; set; }

        [Required(ErrorMessage = "timestamp is required.")]
        public string timestamp { get; set; } = null!;

        [Required(ErrorMessage = "sig is required.")]
        public string sig { get; set; } = null!;
    }
}
