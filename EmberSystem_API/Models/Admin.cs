using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurity_Backend.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }
        public string AdminName { get; set; }
        public string AdminSurname { get; set; }

        public virtual AppUser AppUser { get; set; }
    }
}
