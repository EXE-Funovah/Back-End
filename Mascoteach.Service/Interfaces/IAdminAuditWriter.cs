using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminAuditWriter
{
    Task WriteAsync(AdminAuditWriteRequest request);
}

