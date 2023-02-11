﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public class CollectionNavigationModel : BaseModel
    {
        public CollectionNavigationModel()
        {
            Collections = new List<CollectionBriefInfoModel>();
        }

        public IList<CollectionBriefInfoModel> Collections { get; set; }

        public int TotalCollections { get; set; }
    }

    public class CollectionBriefInfoModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
    }
}