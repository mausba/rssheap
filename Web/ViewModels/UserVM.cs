using Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Web.ViewModels
{
    public class UserVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }

        public string Email { get; set; }
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }

        public DateTime Created { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
