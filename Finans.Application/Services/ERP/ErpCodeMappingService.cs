using Finans.Application.Abstractions.ERP;
using Finans.Application.Abstractions.Logging;
using Finans.Contracts.ERP;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.ERP
{
    public sealed class ErpCodeMappingService : IErpCodeMappingService
    {
        private readonly FinansDbContext _db;
        private readonly IAuditLogService _auditLogService;
        public ErpCodeMappingService(FinansDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task CreateOrUpdateAsync(ErpCodeMappingDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.GlCode))
                throw new InvalidOperationException("GlCode zorunlu.");

            if (string.IsNullOrWhiteSpace(dto.BankAccountCode))
                throw new InvalidOperationException("BankAccountCode zorunlu.");

            ErpCodeMapping entity;

            if (dto.Id > 0)
            {
                entity = await _db.ErpCodeMappings
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.Id == dto.Id && !x.IsDeleted, ct)
                    ?? throw new InvalidOperationException("Mapping kaydı bulunamadı.");

                entity.BankId = dto.BankId;
                entity.Currency = dto.Currency;
                entity.DebitCredit = dto.DebitCredit;
                entity.DescriptionKeyword = dto.DescriptionKeyword;
                entity.CurrentCode = dto.CurrentCode;
                entity.GlCode = dto.GlCode;
                entity.BankAccountCode = dto.BankAccountCode;
                entity.Priority = dto.Priority;
                entity.IsActive = dto.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entity = new ErpCodeMapping
                {
                    CompanyId = dto.CompanyId,
                    BankId = dto.BankId,
                    Currency = dto.Currency,
                    DebitCredit = dto.DebitCredit,
                    DescriptionKeyword = dto.DescriptionKeyword,
                    CurrentCode = dto.CurrentCode,
                    GlCode = dto.GlCode,
                    BankAccountCode = dto.BankAccountCode,
                    Priority = dto.Priority,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                _db.ErpCodeMappings.Add(entity);

                await _auditLogService.WriteAsync(
    companyId: dto.CompanyId,
    userId: null,
    entityName: "ErpCodeMapping",
    actionType: dto.Id > 0 ? "Update" : "Create",
    recordId: entity.Id.ToString(),
    description: "ERP kod eşleme kaydı işlendi.",
    oldValues: null,
    newValues: null,
    ipAddress: null,
    machineName: Environment.MachineName,
    ct: ct);

            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int companyId, int id, CancellationToken ct = default)
        {
            var entity = await _db.ErpCodeMappings
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id && !x.IsDeleted, ct);

            if (entity == null)
                throw new InvalidOperationException("Mapping kaydı bulunamadı.");

            entity.IsDeleted = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _auditLogService.WriteAsync(
    companyId: companyId,
    userId: null,
    entityName: "ErpCodeMapping",
    actionType: "Delete",
    recordId: id.ToString(),
    description: "ERP kod eşleme kaydı silindi.",
    oldValues: null,
    newValues: null,
    ipAddress: null,
    machineName: Environment.MachineName,
    ct: ct);



            await _db.SaveChangesAsync(ct);
        }
    }
}
