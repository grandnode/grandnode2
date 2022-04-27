using Grand.Business.Core.Commands.Messages;
using Grand.Business.Core.Extensions;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public class LiquidObjectBuilder
    {
        private readonly IMediator _mediator;
        private List<Func<LiquidObject, Task>> _chain;
        private LiquidObject _object;

        public LiquidObjectBuilder(IMediator mediator)
        {
            _object = new LiquidObject();
            _chain = new List<Func<LiquidObject, Task>>();
            _mediator = mediator;
        }

        public LiquidObjectBuilder(IMediator mediator, LiquidObject liquidObject)
        {
            _object = liquidObject;
            _chain = new List<Func<LiquidObject, Task>>();
            _mediator = mediator;
        }

        public LiquidObjectBuilder AddStoreTokens(Store store, Language language, EmailAccount emailAccount)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidStore = await _mediator.Send(new GetStoreTokensCommand() { Store = store, Language = language, EmailAccount = emailAccount });
                liquidObject.Store = liquidStore;
                await _mediator.EntityTokensAdded(store, liquidStore, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddOrderTokens(Order order, Customer customer, Store store, DomainHost host, OrderNote orderNote = null, Vendor vendor = null, double refundedAmount = 0)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidOrder = await _mediator.Send(new GetOrderTokensCommand() {
                    Order = order,
                    Customer = customer,
                    Vendor = vendor,
                    Store = store,
                    Host = host,
                    OrderNote = orderNote,
                    RefundedAmount = refundedAmount
                });
                liquidObject.Order = liquidOrder;
                await _mediator.EntityTokensAdded(order, liquidOrder, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddShipmentTokens(Shipment shipment, Order order, Store store, DomainHost host, Language language)
        {
            _chain.Add(async liquidObject =>
            {

                var liquidShipment = await _mediator.Send(new GetShipmentTokensCommand() { Shipment = shipment, Order = order, Store = store, Host = host, Language = language });
                liquidObject.Shipment = liquidShipment;
                await _mediator.EntityTokensAdded(shipment, liquidShipment, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddMerchandiseReturnTokens(MerchandiseReturn merchandiseReturn, Store store, DomainHost host, Order order, Language language, MerchandiseReturnNote merchandiseReturnNote = null)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidMerchandiseReturn = await _mediator.Send(new GetMerchandiseReturnTokensCommand() { Order = order, Language = language, MerchandiseReturn = merchandiseReturn, MerchandiseReturnNote = merchandiseReturnNote, Store = store, Host = host });
                liquidObject.MerchandiseReturn = liquidMerchandiseReturn;
                await _mediator.EntityTokensAdded(merchandiseReturn, liquidMerchandiseReturn, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddGiftVoucherTokens(GiftVoucher giftVoucher, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidGiftCart = await _mediator.Send(new GetGiftVoucherTokensCommand() { GiftVoucher = giftVoucher, Language = language });
                liquidObject.GiftVoucher = liquidGiftCart;
                await _mediator.EntityTokensAdded(giftVoucher, liquidGiftCart, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddCustomerTokens(Customer customer, Store store, DomainHost host, Language language, CustomerNote customerNote = null)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidCustomer = new LiquidCustomer(customer, store, host, customerNote);
                liquidObject.Customer = liquidCustomer;

                await _mediator.EntityTokensAdded(customer, liquidCustomer, liquidObject);
                if (customerNote != null)
                    await _mediator.EntityTokensAdded(customerNote, liquidCustomer, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddShoppingCartTokens(Customer customer, Store store, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidShoppingCart = await _mediator.Send(new GetShoppingCartTokensCommand() {
                    Customer = customer,
                    Language = language,
                    Store = store
                });
                liquidObject.ShoppingCart = liquidShoppingCart;

                await _mediator.EntityTokensAdded(customer, liquidShoppingCart, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddVendorTokens(Vendor vendor, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidVendor = await _mediator.Send(new GetVendorTokensCommand() { Vendor = vendor, Language = language });
                liquidObject.Vendor = liquidVendor;
                await _mediator.EntityTokensAdded(vendor, liquidVendor, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddNewsLetterSubscriptionTokens(NewsLetterSubscription subscription, Store store, DomainHost host)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidNewsletterSubscription = new LiquidNewsLetterSubscription(subscription, store, host);
                liquidObject.NewsLetterSubscription = liquidNewsletterSubscription;
                await _mediator.EntityTokensAdded(subscription, liquidNewsletterSubscription, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddProductReviewTokens(Product product, ProductReview productReview)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidProductReview = new LiquidProductReview(product, productReview);
                liquidObject.ProductReview = liquidProductReview;
                await _mediator.EntityTokensAdded(productReview, liquidProductReview, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddVendorReviewTokens(Vendor vendor, VendorReview vendorReview)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidVendorReview = new LiquidVendorReview(vendor, vendorReview);
                liquidObject.VendorReview = liquidVendorReview;
                await _mediator.EntityTokensAdded(vendorReview, liquidVendorReview, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddBlogCommentTokens(BlogPost blogPost, BlogComment blogComment, Store store, DomainHost host, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidBlogComment = new LiquidBlogComment(blogComment, blogPost, store, host, language);
                liquidObject.BlogComment = liquidBlogComment;
                await _mediator.EntityTokensAdded(blogComment, liquidBlogComment, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddArticleCommentTokens(KnowledgebaseArticle article, KnowledgebaseArticleComment articleComment, Store store, DomainHost host, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidKnowledgebase = new LiquidKnowledgebase(article, articleComment, store, host, language);
                liquidObject.Knowledgebase = liquidKnowledgebase;
                await _mediator.EntityTokensAdded(articleComment, liquidKnowledgebase, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddNewsCommentTokens(NewsItem newsItem, NewsComment newsComment, Store store, DomainHost host, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidNewsComment = new LiquidNewsComment(newsItem, newsComment, store, host, language);
                liquidObject.NewsComment = liquidNewsComment;
                await _mediator.EntityTokensAdded(newsComment, liquidNewsComment, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddProductTokens(Product product, Language language, Store store, DomainHost host)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidProduct = new LiquidProduct(product, language, store, host);
                liquidObject.Product = liquidProduct;
                await _mediator.EntityTokensAdded(product, liquidProduct, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddAttributeCombinationTokens(Product product, ProductAttributeCombination combination)
        {
            _chain.Add(async liquidObject =>
            {

                var liquidAttributeCombination = await _mediator.Send(new GetAttributeCombinationTokensCommand() { Product = product, Combination = combination });
                liquidObject.AttributeCombination = liquidAttributeCombination;
                await _mediator.EntityTokensAdded(combination, liquidAttributeCombination, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddOutOfStockTokens(Product product, OutOfStockSubscription subscription, Store store, DomainHost host, Language language)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidOutOfStockSubscription = new LiquidOutOfStockSubscription(product, subscription, store, host, language);
                liquidObject.OutOfStockSubscription = liquidOutOfStockSubscription;
                await _mediator.EntityTokensAdded(subscription, liquidOutOfStockSubscription, liquidObject);
            });
            return this;
        }

        public LiquidObjectBuilder AddAuctionTokens(Product product, Bid bid)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidAuctions = await _mediator.Send(new GetAuctionTokensCommand() { Product = product, Bid = bid });
                liquidObject.Auctions = liquidAuctions;
                await _mediator.EntityTokensAdded(bid, liquidAuctions, liquidObject);
            });
            return this;
        }
        public LiquidObjectBuilder AddEmailAFriendTokens(string personalMessage, string customerEmail, string friendsEmail)
        {
            _chain.Add(async liquidObject =>
            {
                var liquidEmail = new LiquidEmailAFriend(personalMessage, customerEmail, friendsEmail);
                liquidObject.EmailAFriend = liquidEmail;
                await Task.CompletedTask;
            });
            return this;
        }
        public async Task<LiquidObject> BuildAsync()
        {
            foreach (var f in _chain)
            {
                await f(_object);
            }
            return _object;
        }
    }
}
