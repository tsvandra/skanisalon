using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public InventoryService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        public async Task<IEnumerable<InventoryDocumentDto>> GetDocumentsAsync()
        {
            var docs = await _context.InventoryDocuments
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return docs.Select(d => new InventoryDocumentDto
            {
                Id = d.Id,
                Type = d.Type,
                CreatedAt = d.CreatedAt,
                Note = d.Note
            });
        }

        public async Task<InventoryDocumentDto?> GetDocumentByIdAsync(int id)
        {
            var doc = await _context.InventoryDocuments
                .AsNoTracking()
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return null;

            return new InventoryDocumentDto
            {
                Id = doc.Id,
                Type = doc.Type,
                CreatedAt = doc.CreatedAt,
                Note = doc.Note,
                Items = doc.Items.Select(i => new InventoryDocumentItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Ismeretlen termék",
                    Quantity = i.Quantity,
                    CostPrice = i.CostPrice
                }).ToList()
            };
        }

        public async Task<(bool Success, InventoryDocumentDto? Document, string? Error)> CreateDocumentAsync(CreateInventoryDocumentDto dto)
        {
            // Tranzakció indítása: ha bármelyik tétel elbukik (pl. nincs ilyen termék), semmi sem mentődik el
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var document = new InventoryDocument
                {
                    CompanyId = GetCurrentCompanyId(),
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow,
                    Note = dto.Note,
                    Items = new List<InventoryDocumentItem>()
                };

                foreach (var itemDto in dto.Items)
                {
                    if (itemDto.Quantity <= 0)
                        return (false, null, $"A mennyiségnek nagyobbnak kell lennie 0-nál (Termék ID: {itemDto.ProductId}).");

                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);
                    
                    if (product == null || product.IsDeleted)
                        return (false, null, $"Termék nem található vagy törölve lett (ID: {itemDto.ProductId}).");

                    var documentItem = new InventoryDocumentItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        CostPrice = itemDto.CostPrice
                    };

                    document.Items.Add(documentItem);

                    // --- KÉSZLET FRISSÍTÉSE ---
                    if (dto.Type == InventoryDocumentType.Receipt)
                    {
                        product.CurrentStock += itemDto.Quantity;
                        
                        // Bevételezésnél frissítjük a termék nyilvántartott beszerzési árát az újra
                        if (itemDto.CostPrice > 0)
                        {
                            product.CostPrice = itemDto.CostPrice;
                        }
                    }
                    else if (dto.Type == InventoryDocumentType.Issue || dto.Type == InventoryDocumentType.Adjustment)
                    {
                        product.CurrentStock -= itemDto.Quantity;
                        
                        // Opcionálisan: lehetne ellenőrzés, hogy ne menjen negatívba a készlet.
                        // Viszont sok szalon szoftver engedi a negatív készletet, 
                        // hogy ne akassza meg a munkát (ha elfelejtettek bevételezni, de már elfogyott az anyag).
                    }
                }

                _context.InventoryDocuments.Add(document);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, await GetDocumentByIdAsync(document.Id), null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, null, "Hiba történt a bizonylat mentése során: " + ex.Message);
            }
        }
    }
}

