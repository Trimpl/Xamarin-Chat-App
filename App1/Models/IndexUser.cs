using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace App1.Models
{
    public class IndexUser
    {
        public IdentityUser IdentityUser { get; set; }
        public Avatar Avatar { get; set; }
    }
}
