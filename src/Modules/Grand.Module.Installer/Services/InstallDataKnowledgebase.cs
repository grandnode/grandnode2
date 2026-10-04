using Grand.Domain.Knowledgebase;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallKnowledgebase()
    {
        // categories
        var categoryOrdersShipping = new KnowledgebaseCategory {
            Name = "Orders & Shipping",
            Description = "Delivery times, tracking, and changing or cancelling an order.",
            Published = true,
            DisplayOrder = 1,
            ParentCategoryId = ""
        };
        var categoryReturns = new KnowledgebaseCategory {
            Name = "Returns",
            Description = "Starting a return, refund timing, and returning a gift.",
            Published = true,
            DisplayOrder = 2,
            ParentCategoryId = ""
        };
        var categoryRentalsAuctions = new KnowledgebaseCategory {
            Name = "Rentals & Auctions",
            Description = "How day rentals and vintage bike auctions work on our store.",
            Published = true,
            DisplayOrder = 3,
            ParentCategoryId = ""
        };

        var categories = new List<KnowledgebaseCategory> {
            categoryOrdersShipping, categoryReturns, categoryRentalsAuctions
        };
        foreach (var category in categories)
        {
            await _knowledgebaseCategoryRepository.InsertAsync(category);
            await InsertSlug(category.Id, EntityTypes.KnowledgeBaseCategory, category.Name,
                seName => category.SeName = seName);
            await _knowledgebaseCategoryRepository.UpdateAsync(category);
        }

        const string stillNeedHelp =
            "<p>Still need help? <a href=\"/contactus\">Contact us</a> and we'll get back to you.</p>";

        // Orders & Shipping
        var articleDeliveryTime = new KnowledgebaseArticle {
            ParentCategoryId = categoryOrdersShipping.Id,
            Name = "How long does delivery take",
            Content =
                "<p>Delivery times depend on the shipping method you choose at checkout and where your order is going.</p>" +
                "<ul>" +
                "<li><strong>Standard delivery</strong> usually arrives within 3-5 business days.</li>" +
                "<li><strong>Express delivery</strong> usually arrives within 1-2 business days.</li>" +
                "<li>Orders that include a product shipped directly by one of our marketplace vendors may arrive in a separate parcel, with its own delivery window shown at checkout.</li>" +
                "</ul>" +
                "<p>Orders over 75 dollars qualify for free standard shipping automatically - no code needed. The discount is applied once your cart total reaches the threshold, before any coupon codes.</p>" +
                "<p>Delivery times are estimates and start from the day your order is dispatched, not the day it is placed. You'll get a dispatch confirmation by e-mail once your order is on its way.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = true,
            DisplayOrder = 1
        };
        var articleTracking = new KnowledgebaseArticle {
            ParentCategoryId = categoryOrdersShipping.Id,
            Name = "Tracking your order",
            Content =
                "<p>You can follow your order from the moment it's placed until it lands on your doorstep.</p>" +
                "<ul>" +
                "<li>Sign in and open <strong>My account &gt; Orders</strong> to see every order and its current status.</li>" +
                "<li>Once your order ships, we e-mail you a tracking link - the same link is also shown on the order details page.</li>" +
                "<li>Tracking information can take a few hours to appear after dispatch, so don't worry if it's not live right away.</li>" +
                "</ul>" +
                "<p>Here's what each status means:</p>" +
                "<ul>" +
                "<li><strong>Pending</strong> - we've received your order and are preparing it.</li>" +
                "<li><strong>Processing</strong> - your order is being packed.</li>" +
                "<li><strong>Shipped</strong> - your order is on its way; tracking is available.</li>" +
                "<li><strong>Delivered</strong> - your order has arrived.</li>" +
                "</ul>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 2
        };
        var articleChangeCancel = new KnowledgebaseArticle {
            ParentCategoryId = categoryOrdersShipping.Id,
            Name = "Changing or cancelling an order",
            Content =
                "<p>We start preparing orders quickly, so there's only a short window to make changes.</p>" +
                "<ul>" +
                "<li>While your order still shows as <strong>Pending</strong>, you can ask us to update the shipping address, swap an item, or cancel it entirely.</li>" +
                "<li>Once an order moves to <strong>Processing</strong> or <strong>Shipped</strong>, we can no longer change or cancel it, because it's already being packed or is on its way to you.</li>" +
                "<li>If it's too late to cancel, you're still covered by our returns policy once the order arrives.</li>" +
                "</ul>" +
                "<p>To request a change or cancellation, contact us as soon as possible with your order number - the sooner you reach out, the more likely we can catch it in time.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 3,
        };

        // Returns
        var articleStartReturn = new KnowledgebaseArticle {
            ParentCategoryId = categoryReturns.Id,
            Name = "How to start a merchandise return",
            Content =
                "<p>Changed your mind, or something isn't quite right? You can request a merchandise return from your account within 30 days of delivery.</p>" +
                "<ol>" +
                "<li>Sign in and open <strong>My account &gt; Orders</strong>.</li>" +
                "<li>Open the order that contains the item and select <strong>Return</strong>.</li>" +
                "<li>Choose the item, pick a reason, and let us know whether you'd like a refund or a replacement.</li>" +
                "<li>Submit the request - we'll e-mail you instructions for sending the item back.</li>" +
                "</ol>" +
                "<p>Items should be unused, in their original packaging, and with tags attached where applicable. You can track the status of your return from the same <strong>Orders</strong> page at any time.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = true,
            DisplayOrder = 1
        };
        var articleRefundTiming = new KnowledgebaseArticle {
            ParentCategoryId = categoryReturns.Id,
            Name = "When will I get my refund",
            Content =
                "<p>Once we receive and inspect your returned item, we process the refund to your original payment method.</p>" +
                "<ul>" +
                "<li><strong>Credit or debit card</strong> - refunds usually appear within 5-10 business days, depending on your bank.</li>" +
                "<li><strong>PayPal</strong> - refunds are typically visible within 1-3 business days.</li>" +
                "<li><strong>Store credit or gift voucher</strong> - added to your account balance right away and ready to use immediately.</li>" +
                "</ul>" +
                "<p>We send a confirmation e-mail as soon as your refund is issued, so you'll always know where things stand. If it's been longer than expected, get in touch with your order number and we'll look into it.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 2,
        };
        var articleReturningGift = new KnowledgebaseArticle {
            ParentCategoryId = categoryReturns.Id,
            Name = "Returning a gift",
            Content =
                "<p>Received something that isn't quite right for you? You can return a gift without the person who bought it being notified.</p>" +
                "<ul>" +
                "<li>Contact us with the order number (found on the gift receipt, if one was included) or the recipient's name and approximate order date.</li>" +
                "<li>We'll verify the order and start the return on your behalf.</li>" +
                "<li>Instead of a refund to the original payment method, gift returns are issued as a store credit voucher, so the giver's payment details are never touched.</li>" +
                "</ul>" +
                "<p>Your voucher is added to your account and can be used on any future order. The same 30-day return window from delivery applies to gifts as it does to any other order.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 3,
        };

        // Rentals & Auctions
        var articleDayRentals = new KnowledgebaseArticle {
            ParentCategoryId = categoryRentalsAuctions.Id,
            Name = "How day rentals work",
            Content =
                "<p>Some of our gear, like tents and e-bikes, is available to rent by the day instead of buying outright.</p>" +
                "<ul>" +
                "<li>On the product page, use the calendar to pick your pickup date. Each day is its own slot, and once a day is booked it's no longer available to other customers.</li>" +
                "<li>Choose how many days you need - the price shown updates automatically for the whole booking.</li>" +
                "<li>Add optional extras, like a sleeping pad or a helmet, right on the product page before checking out.</li>" +
                "<li>Pick up and return the gear at the times shown on your booking confirmation. Rentals are picked up in person rather than shipped.</li>" +
                "</ul>" +
                "<p>Please return gear on time and in the condition you received it. If something is damaged beyond normal wear, we'll contact you about a repair or replacement charge before charging anything.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 1
        };
        var articleAuctionBidding = new KnowledgebaseArticle {
            ParentCategoryId = categoryRentalsAuctions.Id,
            Name = "Bidding in a vintage auction",
            Content =
                "<p>A handful of restored, one-of-a-kind items - like vintage bikes - are sold by auction instead of at a fixed price.</p>" +
                "<ul>" +
                "<li>Each auction listing shows a starting price and a countdown to the auction's end time.</li>" +
                "<li>Place a bid at or above the minimum next bid shown on the page. You can be outbid, so it's worth checking back before the timer runs out.</li>" +
                "<li>When the countdown reaches zero, the highest bid wins - there's only one of each item, so once the auction ends it's no longer available to bid on.</li>" +
                "<li>If you win, we'll notify you by e-mail with a checkout link. Payment is due using the same methods as any other order, and the item ships or is ready for pickup once payment is confirmed.</li>" +
                "</ul>" +
                "<p>If you don't win, no payment is taken and you're free to bid on a future auction.</p>" +
                stillNeedHelp,
            Published = true,
            AllowComments = false,
            ShowOnHomepage = false,
            DisplayOrder = 2,
        };

        //related articles: every article links to at least two others in or near its topic
        articleDeliveryTime.RelatedArticles = [articleTracking.Id, articleChangeCancel.Id];
        articleTracking.RelatedArticles = [articleDeliveryTime.Id, articleChangeCancel.Id];
        articleChangeCancel.RelatedArticles = [articleDeliveryTime.Id, articleTracking.Id];
        articleStartReturn.RelatedArticles = [articleRefundTiming.Id, articleReturningGift.Id];
        articleRefundTiming.RelatedArticles = [articleStartReturn.Id, articleReturningGift.Id];
        articleReturningGift.RelatedArticles = [articleStartReturn.Id, articleRefundTiming.Id];
        articleDayRentals.RelatedArticles = [articleAuctionBidding.Id, articleDeliveryTime.Id];
        articleAuctionBidding.RelatedArticles = [articleDayRentals.Id, articleTracking.Id];

        var articles = new List<KnowledgebaseArticle> {
            articleDeliveryTime, articleTracking, articleChangeCancel,
            articleStartReturn, articleRefundTiming, articleReturningGift,
            articleDayRentals, articleAuctionBidding
        };
        foreach (var article in articles)
        {
            await _knowledgebaseArticleRepository.InsertAsync(article);
            await InsertSlug(article.Id, EntityTypes.KnowledgeBaseArticle, article.Name,
                seName => article.SeName = seName);
            await _knowledgebaseArticleRepository.UpdateAsync(article);
        }
    }
}
