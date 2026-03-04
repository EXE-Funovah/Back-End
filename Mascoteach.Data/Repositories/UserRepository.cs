using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MascoteachContext context) : base(context) 
        { 
        
        }
    }
}
