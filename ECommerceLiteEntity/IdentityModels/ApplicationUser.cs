using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using ECommerceLiteEntity.Models;
using System.Collections.Generic;

namespace ECommerceLiteEntity.IdentityModels
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(maximumLength: 25, MinimumLength = 2, ErrorMessage = "İsminizin uzunluğu 2 ile 25 karakter arasında olmalıdır!")]
        [Display(Name = "Ad")]
        [Required]
        public string Name { get; set; }
        [StringLength(maximumLength: 25, MinimumLength = 2, ErrorMessage = "Soyisminizin uzunluğu 2 ile 25 karakter arasında olmalıdır!")]
        [Display(Name = "Soyad")]
        [Required]
        public string Surname { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RegisterDate { get; set; } = DateTime.Now;
        //TODO: Guid'in kaç haneli olduğuna bakıp stringlength attribute tanımlayacağız
        public string ActivationCode { get; set; }
        public virtual List<Customer> CustomerList { get; set; }
        public virtual List<Admin> AdminList { get; set; }
        public virtual List<PassiveUser> PassiveUserList { get; set; }
    }
}
