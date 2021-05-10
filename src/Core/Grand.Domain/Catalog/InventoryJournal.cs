using Grand.Domain.Common;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    public class InventoryJournal : BaseEntity
    {
        public string ObjectId { get; set; }
        public string ObjectType { get; set; }
        public string PositionId { get; set; }
        public DateTime CreateDateUtc { get; set; }
        public string ProductId { get; set; }
        public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();
        public string WarehouseId { get; set; }
        public int InQty { get; set; }
        public int OutQty { get; set; }
        public string Comments { get; set; }
        public string Reference { get; set; }
    }
}
