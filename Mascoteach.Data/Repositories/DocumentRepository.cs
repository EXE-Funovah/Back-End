using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Data.Repositories
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(MascoteachContext context) :base(context) 
        {
            
        }
    }
}
