using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;

namespace Orchard.Users.ViewModels {
    public class UserCreateViewModel  {
        [Required]
        public string UserName { get; set; }

        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public IContent User { get; set; }
    }
}