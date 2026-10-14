using Domain.Entities.BaseEntity;
using Domain.Enums;

namespace Domain.Entities;

public class Cover : EntityBase
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public CoverType Type { get; set; }

    public decimal Premium { get; set; }
}
