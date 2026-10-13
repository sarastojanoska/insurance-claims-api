using Domain.Entities.BaseEntity;
using Domain.Enums;

namespace Domain.Entities
{
    public class Claim : EntityBase
    {
        public string CoverId { get; set; }

        public string Name { get; set; }

        public ClaimType Type { get; set; }

        public decimal DamageCost { get; set; }
    }
}
