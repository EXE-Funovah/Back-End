using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        // Role phải khớp với CHECK constraint trong SQL: 'Teacher', 'Parent', 'Student'
        public string Role { get; set; } = null!;
    }
}
