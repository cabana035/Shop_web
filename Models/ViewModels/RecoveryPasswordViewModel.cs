using System.ComponentModel.DataAnnotations;

namespace Shop_web.Models.ViewModels
{
    public class RecoveryPasswordViewModel
    {
        [Required]
        public string Email { get; set; }
    }
}
