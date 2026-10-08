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
                Note = d.Note,
                AppointmentId = d.AppointmentId,
                IsReversed = d.IsReversed,
                ReversalOfDocumentId = d.ReversalOfDocumentId
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
                AppointmentId = doc.AppointmentId,
                IsReversed = doc.IsReversed,
                ReversalOfDocumentId = doc.ReversalOfDocumentId,
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
                    AppointmentId = dto.AppointmentId,
                    Items = new List<InventoryDocumentItem>()
                };

                if (dto.AppointmentId.HasValue)
                {
                    var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == dto.AppointmentId.Value && a.CompanyId == GetCurrentCompanyId());
                    if (appointment != null)
                    {
                        if (appointment.MaterialUsageRecorded && dto.Type == InventoryDocumentType.Issue)
                            return (false, null, "Ennek a foglalásnak a napi zárása már rögzítve van. Előbb vond vissza a zárást, ha módosítani szeretnél.");

                        appointment.MaterialUsageRecorded = true;
                    }
                }

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

        public async Task<(bool Success, int ReversedDocuments, string? Error)> ReverseAppointmentClosingAsync(int appointmentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = GetCurrentCompanyId();

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

                if (appointment == null)
                    return (false, 0, "A foglalás nem található.");

                if (!appointment.MaterialUsageRecorded)
                    return (false, 0, "Ennek a foglalásnak a napi zárása nincs rögzítve, így nincs mit visszavonni.");

                // A foglaláshoz tartozó, még nem sztornózott kiadási bizonylatok
                var documents = await _context.InventoryDocuments
                    .Include(d => d.Items)
                    .Where(d => d.AppointmentId == appointmentId
                                && d.Type == InventoryDocumentType.Issue
                                && !d.IsReversed)
                    .ToListAsync();

                foreach (var original in documents)
                {
                    var reversal = new InventoryDocument
                    {
                        CompanyId = companyId,
                        Type = InventoryDocumentType.Receipt,
                        CreatedAt = DateTime.UtcNow,
                        Note = $"Sztornó: napi zárás visszavonása (Foglalás ID: {appointmentId}, bizonylat #{original.Id})",
                        AppointmentId = appointmentId,
                        ReversalOfDocumentId = original.Id,
                        Items = new List<InventoryDocumentItem>()
                    };

                    foreach (var item in original.Items)
                    {
                        reversal.Items.Add(new InventoryDocumentItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            CostPrice = item.CostPrice
                        });

                        // Készlet visszaírása (a beszerzési árat nem módosítjuk)
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            product.CurrentStock += item.Quantity;
                        }
                    }

                    original.IsReversed = true;
                    _context.InventoryDocuments.Add(reversal);
                }

                // Újra lezárhatóvá tesszük a foglalást
                appointment.MaterialUsageRecorded = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, documents.Count, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, 0, "Hiba történt a zárás visszavonása során: " + ex.Message);
            }
        }

        public async Task<(bool Success, ImportDeliveryNoteResultDto? Result, string? Error)> ImportDeliveryNoteAsync(ImportDeliveryNoteDto dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
            {
                return (false, null, "Legalább egy tételt meg kell adni a bevételezéshez.");
            }

            int companyId = GetCurrentCompanyId();
            if (companyId == 0)
            {
                return (false, null, "Érvénytelen szalon azonosító.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int createdCount = 0;
                int matchedCount = 0;

                var document = new InventoryDocument
                {
                    CompanyId = companyId,
                    Type = InventoryDocumentType.Receipt,
                    CreatedAt = DateTime.UtcNow,
                    Note = $"Szállítólevél bevételezés: {dto.DocumentNumber} - {dto.Supplier}. {dto.Note}".Trim(),
                    Items = new List<InventoryDocumentItem>()
                };

                foreach (var item in dto.Items)
                {
                    if (item.Action == "skip") continue;
                    if (item.Quantity <= 0) continue;

                    int targetProductId;

                    if (item.Action == "create" && item.NewProduct != null)
                    {
                        var newProd = new Product
                        {
                            CompanyId = companyId,
                            Name = item.NewProduct.Name,
                            Description = item.NewProduct.Description,
                            EAN = item.NewProduct.EAN,
                            Shade = item.NewProduct.Shade,
                            ImageUrl = item.NewProduct.ImageUrl,
                            IsProfessional = item.NewProduct.IsProfessional,
                            IsRetail = item.NewProduct.IsRetail,
                            Unit = item.NewProduct.Unit,
                            PackageSize = item.NewProduct.PackageSize > 0 ? item.NewProduct.PackageSize : 1,
                            CostPrice = item.CostPrice > 0 ? item.CostPrice : item.NewProduct.CostPrice,
                            RetailPrice = item.NewProduct.RetailPrice,
                            LowStockThreshold = item.NewProduct.LowStockThreshold,
                            CurrentStock = 0,
                            CreationDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        _context.Products.Add(newProd);
                        await _context.SaveChangesAsync();

                        targetProductId = newProd.Id;
                        createdCount++;
                    }
                    else if (item.MatchedProductId.HasValue)
                    {
                        targetProductId = item.MatchedProductId.Value;
                        var existingProd = await _context.Products.FirstOrDefaultAsync(p => p.Id == targetProductId && !p.IsDeleted);
                        if (existingProd == null)
                        {
                            return (false, null, $"A párosított termék (ID: {targetProductId}) nem található.");
                        }

                        // Ha van megadott beszerzési ár és nagyobb mint 0, frissítjük a termék nyilvántartott beszerzési árát
                        if (item.CostPrice > 0)
                        {
                            existingProd.CostPrice = item.CostPrice;
                        }
                        matchedCount++;
                    }
                    else
                    {
                        continue;
                    }

                    // Hozzáadjuk a bevételezési bizonylathoz és frissítjük a készletet
                    var docItem = new InventoryDocumentItem
                    {
                        ProductId = targetProductId,
                        Quantity = item.Quantity,
                        CostPrice = item.CostPrice
                    };
                    document.Items.Add(docItem);

                    var productToUpdate = await _context.Products.FirstOrDefaultAsync(p => p.Id == targetProductId);
                    if (productToUpdate != null)
                    {
                        productToUpdate.CurrentStock += item.Quantity;
                    }
                }

                if (document.Items.Count == 0)
                {
                    return (false, null, "Nem lett egyetlen tétel sem kiválasztva a bevételezéshez.");
                }

                _context.InventoryDocuments.Add(document);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = new ImportDeliveryNoteResultDto
                {
                    DocumentId = document.Id,
                    CreatedProductsCount = createdCount,
                    MatchedItemsCount = matchedCount,
                    TotalItemsCount = document.Items.Count
                };

                return (true, result, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, null, $"Hiba a szállítólevél bevételezése során: {ex.Message}");
            }
        }
    }
}

