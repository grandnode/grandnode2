﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Models.ShoppingCart
{
    public class WishlistModel : BaseModel
    {
        public WishlistModel()
        {
            Items = new List<ShoppingCartItemModel>();
            Warnings = new List<string>();
        }

        public Guid CustomerGuid { get; set; }
        public string CustomerFullname { get; set; }

        public bool EmailWishlistEnabled { get; set; }

        public bool ShowSku { get; set; }

        public bool ShowProductImages { get; set; }

        public bool IsEditable { get; set; }

        public bool DisplayAddToCart { get; set; }


        public IList<ShoppingCartItemModel> Items { get; set; }

        public IList<string> Warnings { get; set; }
        
		#region Nested Classes

        public class ShoppingCartItemModel : BaseEntityModel
        {
            public ShoppingCartItemModel()
            {
                Picture = new PictureModel();
                AllowedQuantities = new List<SelectListItem>();
                Warnings = new List<string>();
            }
            public string Sku { get; set; }

            public PictureModel Picture {get;set;}

            public string ProductId { get; set; }

            public string ProductName { get; set; }

            public string ProductSeName { get; set; }

            public string ProductUrl { get; set; }

            public string UnitPrice { get; set; }

            public string SubTotal { get; set; }

            public string Discount { get; set; }

            public int Quantity { get; set; }
            public List<SelectListItem> AllowedQuantities { get; set; }
            
            public string AttributeInfo { get; set; }

            public string RecurringInfo { get; set; }

            public string RentalInfo { get; set; }
            public bool AllowItemEditing { get; set; }

            public IList<string> Warnings { get; set; }

        }

		#endregion
    }
}