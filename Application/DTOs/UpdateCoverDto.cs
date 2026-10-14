using Domain.Enums;

namespace Application.DTOs
{
    public record UpdateCoverDto(DateTime StartDate, DateTime EndDate, CoverType Type);
}
