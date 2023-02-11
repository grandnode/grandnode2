﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Media
{
    public class PictureModel : BaseEntityModel
    {
        public string ImageUrl { get; set; }
        public string ThumbImageUrl { get; set; }
        public string FullSizeImageUrl { get; set; }
        public string Title { get; set; }
        public string AlternateText { get; set; }
        public string Style { get; set; }
        public string ExtraField { get; set; }
    }
}