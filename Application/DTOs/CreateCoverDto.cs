using Domain.Enums;

namespace Application.DTOs
{
    public record CreateCoverDto(DateTime StartDate, DateTime EndDate, CoverType Type);
}
