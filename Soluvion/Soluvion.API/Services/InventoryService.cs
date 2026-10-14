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
        private readonly IUserContext _userContext;

        public InventoryService(AppDbContext context, ITenantContext tenantContext, IUserContext userContext)
        {
            _context = context;
            _tenantContext = tenantContext;
            _userContext = userContext;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        private static string? ExtractCustomerName(CompanyCustomer? c)
        {
            if (c?.Attributes == null) return null;
            if (c.Attributes.TryGetValue("FullName", out var fn) && !string.IsNullOrWhiteSpace(fn)) return fn;
            if (c.Attributes.TryGetValue("Name", out var n) && !string.IsNullOrWhiteSpace(n)) return n;
            if (c.Attributes.TryGetValue("Phone", out var ph) && !string.IsNullOrWhiteSpace(ph)) return ph;
            if (c.Attributes.TryGetValue("Email", out var em) && !string.IsNullOrWhiteSpace(em)) return em;
            return null;
        }

        public async Task<IEnumerable<InventoryDocumentDto>> GetDocumentsAsync()
        {
            var docs = await _context.InventoryDocuments
                .AsNoTrackingWithIdentityResolution()
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .Include(d => d.Appointment)
                    .ThenInclude(a => a!.Customer)
                .Include(d => d.Appointment)
                    .ThenInclude(a => a!.Employee)
                        .ThenInclude(e => e!.User)
                .OrderByDescending(d => d.CreatedAt)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            return docs.Select(d => new InventoryDocumentDto
            {
                Id = d.Id,
                Type = d.Type,
                CreatedAt = d.CreatedAt,
                Note = d.Note,
                AppointmentId = d.AppointmentId,
                CustomerName = ExtractCustomerName(d.Appointment?.Customer),
                AppointmentDate = d.Appointment?.StartDateTime,
                CreatedByUserId = d.CreatedByUserId,
                CreatedByName = !string.IsNullOrWhiteSpace(d.CreatedByName)
                    ? d.CreatedByName
                    : d.Appointment?.Employee?.User?.Username,
                IsReversed = d.IsReversed,
                ReversalOfDocumentId = d.ReversalOfDocumentId,
                ItemsCount = d.Items.Count,
                TotalValue = d.Items.Sum(i => i.Quantity * i.CostPrice),
                Items = d.Items.Select(i => new InventoryDocumentItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Ismeretlen termék",
                    ProductShade = i.Product?.Shade,
                    ProductEan = i.Product?.EAN,
                    ProductImageUrl = i.Product?.ImageUrl,
                    ProductUnit = i.Product?.Unit ?? UnitOfMeasure.Piece,
                    ProductPackageSize = i.Product?.PackageSize ?? 1,
                    Quantity = i.Quantity,
                    CostPrice = i.CostPrice,
                    ItemNote = i.ItemNote,
                    StockBefore = i.StockBefore,
                    StockAfter = i.StockAfter
                }).ToList()
            });
        }

        public async Task<InventoryDocumentDto?> GetDocumentByIdAsync(int id)
        {
            var doc = await _context.InventoryDocuments
                .AsNoTrackingWithIdentityResolution()
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .Include(d => d.Appointment)
                    .ThenInclude(a => a!.Customer)
                .Include(d => d.Appointment)
                    .ThenInclude(a => a!.Employee)
                        .ThenInclude(e => e!.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return null;

            return new InventoryDocumentDto
            {
                Id = doc.Id,
                Type = doc.Type,
                CreatedAt = doc.CreatedAt,
                Note = doc.Note,
                AppointmentId = doc.AppointmentId,
                CustomerName = ExtractCustomerName(doc.Appointment?.Customer),
                AppointmentDate = doc.Appointment?.StartDateTime,
                CreatedByUserId = doc.CreatedByUserId,
                CreatedByName = !string.IsNullOrWhiteSpace(doc.CreatedByName)
                    ? doc.CreatedByName
                    : doc.Appointment?.Employee?.User?.Username,
                IsReversed = doc.IsReversed,
                ReversalOfDocumentId = doc.ReversalOfDocumentId,
                ItemsCount = doc.Items.Count,
                TotalValue = doc.Items.Sum(i => i.Quantity * i.CostPrice),
                Items = doc.Items.Select(i => new InventoryDocumentItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Ismeretlen termék",
                    ProductShade = i.Product?.Shade,
                    ProductEan = i.Product?.EAN,
                    ProductImageUrl = i.Product?.ImageUrl,
                    ProductUnit = i.Product?.Unit ?? UnitOfMeasure.Piece,
                    ProductPackageSize = i.Product?.PackageSize ?? 1,
                    Quantity = i.Quantity,
                    CostPrice = i.CostPrice,
                    ItemNote = i.ItemNote,
                    StockBefore = i.StockBefore,
                    StockAfter = i.StockAfter
                }).ToList()
            };
        }

        public async Task<ProductHistoryResponseDto?> GetProductHistoryAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return null;

            var items = await _context.InventoryDocumentItems
                .AsNoTrackingWithIdentityResolution()
                .Include(i => i.InventoryDocument)
                    .ThenInclude(d => d!.Appointment)
                        .ThenInclude(a => a!.Customer)
                .Include(i => i.InventoryDocument)
                    .ThenInclude(d => d!.Appointment)
                        .ThenInclude(a => a!.Employee)
                            .ThenInclude(e => e!.User)
                .Where(i => i.ProductId == productId && i.InventoryDocument != null)
                .OrderByDescending(i => i.InventoryDocument!.CreatedAt)
                .ThenByDescending(i => i.InventoryDocumentId)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            var docIds = items.Select(i => i.InventoryDocumentId).Distinct().ToList();
            var docItemCounts = docIds.Count > 0
                ? await _context.InventoryDocumentItems
                    .AsNoTracking()
                    .Where(i => docIds.Contains(i.InventoryDocumentId))
                    .GroupBy(i => i.InventoryDocumentId)
                    .Select(g => new { DocumentId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.DocumentId, x => x.Count)
                : new Dictionary<int, int>();

            decimal runningStock = product.CurrentStock;
            decimal totalReceived = 0;
            decimal totalIssued = 0;
            var receiptCostPrices = new List<decimal>();

            var movementDtos = new List<ProductMovementHistoryItemDto>();

            foreach (var item in items)
            {
                var doc = item.InventoryDocument!;
                bool isReceipt = doc.Type == InventoryDocumentType.Receipt;

                if (!doc.IsReversed && !doc.ReversalOfDocumentId.HasValue)
                {
                    if (isReceipt)
                    {
                        totalReceived += item.Quantity;
                        if (item.CostPrice > 0)
                        {
                            receiptCostPrices.Add(item.CostPrice);
                        }
                    }
                    else
                    {
                        totalIssued += item.Quantity;
                    }
                }
                else
                {
                    // Ha sztornózott vagy sztornó tétel, a tényleges forgalomban kompenzálják egymást
                    if (isReceipt) totalReceived += item.Quantity;
                    else totalIssued += item.Quantity;
                }

                decimal stockAfter;
                decimal stockBefore;

                if (item.StockBefore.HasValue && item.StockAfter.HasValue)
                {
                    stockBefore = item.StockBefore.Value;
                    stockAfter = item.StockAfter.Value;
                    runningStock = stockBefore;
                }
                else
                {
                    stockAfter = runningStock;
                    if (isReceipt)
                    {
                        stockBefore = Math.Max(0, stockAfter - item.Quantity);
                    }
                    else
                    {
                        stockBefore = stockAfter + item.Quantity;
                    }
                    runningStock = stockBefore;
                }

                movementDtos.Add(new ProductMovementHistoryItemDto
                {
                    ItemId = item.Id,
                    DocumentId = doc.Id,
                    Type = doc.Type,
                    CreatedAt = doc.CreatedAt,
                    Quantity = item.Quantity,
                    SignedQuantity = isReceipt ? item.Quantity : -item.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = stockAfter,
                    CostPrice = item.CostPrice,
                    DocumentNote = doc.Note,
                    ItemNote = item.ItemNote,
                    CreatedByName = !string.IsNullOrWhiteSpace(doc.CreatedByName)
                        ? doc.CreatedByName
                        : doc.Appointment?.Employee?.User?.Username,
                    AppointmentId = doc.AppointmentId,
                    CustomerName = ExtractCustomerName(doc.Appointment?.Customer),
                    AppointmentDate = doc.Appointment?.StartDateTime,
                    IsReversed = doc.IsReversed,
                    ReversalOfDocumentId = doc.ReversalOfDocumentId,
                    DocumentItemsCount = docItemCounts.TryGetValue(doc.Id, out var cnt) ? cnt : 1
                });
            }

            return new ProductHistoryResponseDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Shade = product.Shade,
                ImageUrl = product.ImageUrl,
                Unit = product.Unit,
                PackageSize = product.PackageSize,
                CurrentStock = product.CurrentStock,
                CostPrice = product.CostPrice,
                RetailPrice = product.RetailPrice,
                TotalReceivedQty = totalReceived,
                TotalIssuedQty = totalIssued,
                MovementsCount = movementDtos.Count,
                LastCostPrice = receiptCostPrices.Count > 0 ? receiptCostPrices[0] : (product.CostPrice > 0 ? product.CostPrice : null),
                PreviousCostPrice = receiptCostPrices.Count > 1 ? receiptCostPrices[1] : null,
                Movements = movementDtos
            };
        }

        public async Task<bool> UpdateDocumentNoteAsync(int documentId, string? note)
        {
            var doc = await _context.InventoryDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return false;

            doc.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDocumentItemNoteAsync(int itemId, string? itemNote)
        {
            var item = await _context.InventoryDocumentItems.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return false;

            item.ItemNote = string.IsNullOrWhiteSpace(itemNote) ? null : itemNote.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, InventoryDocumentDto? Document, string? Error)> CreateDocumentAsync(CreateInventoryDocumentDto dto)
        {
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
                    CreatedByUserId = _userContext.UserId,
                    CreatedByName = _userContext.Username,
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

                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                    .ToDictionaryAsync(p => p.Id);

                foreach (var itemDto in dto.Items)
                {
                    if (itemDto.Quantity <= 0)
                        return (false, null, $"A mennyiségnek nagyobbnak kell lennie 0-nál (Termék ID: {itemDto.ProductId}).");

                    if (!products.TryGetValue(itemDto.ProductId, out var product))
                        return (false, null, $"Termék nem található vagy törölve lett (ID: {itemDto.ProductId}).");

                    decimal stockBefore = product.CurrentStock;

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
                        product.CurrentStock = Math.Max(0, product.CurrentStock - itemDto.Quantity);
                    }

                    decimal stockAfter = product.CurrentStock;

                    var documentItem = new InventoryDocumentItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        CostPrice = itemDto.CostPrice,
                        ItemNote = string.IsNullOrWhiteSpace(itemDto.ItemNote) ? null : itemDto.ItemNote.Trim(),
                        StockBefore = stockBefore,
                        StockAfter = stockAfter
                    };

                    document.Items.Add(documentItem);
                }

                _context.InventoryDocuments.Add(document);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var resultDto = new InventoryDocumentDto
                {
                    Id = document.Id,
                    Type = document.Type,
                    CreatedAt = document.CreatedAt,
                    Note = document.Note,
                    AppointmentId = document.AppointmentId,
                    CreatedByUserId = document.CreatedByUserId,
                    CreatedByName = document.CreatedByName,
                    IsReversed = document.IsReversed,
                    ReversalOfDocumentId = document.ReversalOfDocumentId,
                    ItemsCount = document.Items.Count,
                    TotalValue = document.Items.Sum(i => i.Quantity * i.CostPrice),
                    Items = document.Items.Select(i => new InventoryDocumentItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = products.TryGetValue(i.ProductId, out var p) ? p.Name : "Ismeretlen termék",
                        ProductShade = products.TryGetValue(i.ProductId, out var p2) ? p2.Shade : null,
                        Quantity = i.Quantity,
                        CostPrice = i.CostPrice,
                        ItemNote = i.ItemNote,
                        StockBefore = i.StockBefore,
                        StockAfter = i.StockAfter
                    }).ToList()
                };

                return (true, resultDto, null);
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
                        CreatedByUserId = _userContext.UserId,
                        CreatedByName = _userContext.Username,
                        Items = new List<InventoryDocumentItem>()
                    };

                    foreach (var item in original.Items)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        decimal stockBefore = product?.CurrentStock ?? 0;

                        if (product != null)
                        {
                            product.CurrentStock += item.Quantity;
                        }

                        decimal stockAfter = product?.CurrentStock ?? (stockBefore + item.Quantity);

                        reversal.Items.Add(new InventoryDocumentItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            CostPrice = item.CostPrice,
                            StockBefore = stockBefore,
                            StockAfter = stockAfter
                        });
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
                var receivedItemsList = new List<ReceivedProductSummaryDto>();

                var document = new InventoryDocument
                {
                    CompanyId = companyId,
                    Type = InventoryDocumentType.Receipt,
                    CreatedAt = DateTime.UtcNow,
                    Note = $"Szállítólevél bevételezés: {dto.DocumentNumber} - {dto.Supplier}. {dto.Note}".Trim(),
                    CreatedByUserId = _userContext.UserId,
                    CreatedByName = _userContext.Username,
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

                    var isNew = (item.Action == "create" && item.NewProduct != null);

                    var productToUpdate = await _context.Products.FirstOrDefaultAsync(p => p.Id == targetProductId);
                    decimal stockBefore = productToUpdate?.CurrentStock ?? 0;

                    if (productToUpdate != null)
                    {
                        productToUpdate.CurrentStock += item.Quantity;
                        receivedItemsList.Add(new ReceivedProductSummaryDto
                        {
                            ProductId = targetProductId,
                            Name = productToUpdate.Name,
                            Shade = productToUpdate.Shade,
                            QuantityAdded = item.Quantity,
                            NewStock = productToUpdate.CurrentStock,
                            IsNewProduct = isNew
                        });
                    }

                    decimal stockAfter = productToUpdate?.CurrentStock ?? (stockBefore + item.Quantity);

                    // Hozzáadjuk a bevételezési bizonylathoz
                    var docItem = new InventoryDocumentItem
                    {
                        ProductId = targetProductId,
                        Quantity = item.Quantity,
                        CostPrice = item.CostPrice,
                        StockBefore = stockBefore,
                        StockAfter = stockAfter
                    };
                    document.Items.Add(docItem);
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
                    TotalItemsCount = document.Items.Count,
                    ReceivedItems = receivedItemsList
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


