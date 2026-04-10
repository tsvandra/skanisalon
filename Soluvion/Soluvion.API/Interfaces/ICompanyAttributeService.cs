using Soluvion.API.DTOs.CompanyAttributeDtos;

namespace Soluvion.API.Interfaces
{
    public interface ICompanyAttributeService
    {
        Task<List<CompanyAttributeDto>> GetAttributesAsync();
        Task<CompanyAttributeDto> CreateAttributeAsync(CreateUpdateCompanyAttributeDto dto);
        Task<CompanyAttributeDto> UpdateAttributeAsync(int id, CreateUpdateCompanyAttributeDto dto);
        Task DeleteAttributeAsync(int id);
    }
}