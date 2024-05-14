using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;

namespace ApplicationSecurity_Backend.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        

        // Other customer properties
        public virtual AppUser AppUser { get; set; }

    }
}
