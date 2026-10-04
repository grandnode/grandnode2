using Grand.Domain.Common;
using Grand.Domain.News;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallNews()
    {
        var news = new List<(NewsItem Item, string Image)> {
            (new NewsItem {
                Title = "Our Outdoor and Active Department Is Here",
                Short =
                    "Tents, trail shoes, and cycling gear from Trailforge and Kinetic are now in store, with 15 percent off the whole department for the launch.",
                Full =
                    "<p>We have been testing gear on long weekends all year, and today our <a href='/outdoor-active'>Outdoor &amp; Active</a> department opens with camping, cycling, and running gear from Trailforge and Kinetic.</p>" +
                    "<p>Trailforge brings lightweight tents, down sleeping bags, and camp kitchen essentials built for three season trips. Kinetic covers the moving parts: trail running shoes, a hydration vest that stays put, and a commuter helmet we actually like wearing.</p>" +
                    "<p>To celebrate, everything in the department is <strong>15 percent off</strong> for the launch, including camping, cycling, and running. The discount is applied automatically at checkout, so there is no code to remember.</p>" +
                    "<p>Not ready to buy? Some of the gear can also be rented by the day, and our help centre explains how rentals and auctions work.</p>",
                Published = true,
                AllowComments = true,
                StartDateUtc = DateTime.UtcNow.AddDays(-3),
                MetaTitle = "Our Outdoor and Active Department Is Here",
                MetaDescription = "Camping, cycling, and running gear from Trailforge and Kinetic, with 15 percent off the department for the launch."
            }, "news_outdoor_active_launch.jpg"),
            (new NewsItem {
                Title = "Rent Gear by the Day with Alpine Rental Co.",
                Short =
                    "You can now book a tent or a touring e-bike for exactly the days you need, with add-ons like a camp stove or panniers, and pick them up in Innsbruck.",
                Full =
                    "<p>Buying a tent for one trip a year never made much sense to us. So we teamed up with <a href='/alpine-rental-co'>Alpine Rental Co.</a> in Innsbruck to offer gear by the day.</p>" +
                    "<ul>" +
                    "<li><a href='/trailforge-camping-tent-rental'>Trailforge Camping Tent Rental</a>: our two person tent, with a sleeping pad or a camp stove as optional extras.</li>" +
                    "<li><a href='/touring-e-bike-rental'>Touring E-Bike Rental</a>: a comfortable touring e-bike with pannier bags, a helmet, or a child seat available as add-ons.</li>" +
                    "</ul>" +
                    "<p>Pick your dates on the product page calendar, add the extras you need, and collect everything from the Alpine Rental Co. shop on the day your booking starts. Booked days disappear from the calendar straight away, so nobody else can take your slot.</p>" +
                    "<p>If you end up loving the tent, you can buy your own from the same range.</p>",
                Published = true,
                AllowComments = true,
                StartDateUtc = DateTime.UtcNow.AddDays(-10),
                MetaTitle = "Rent Gear by the Day with Alpine Rental Co.",
                MetaDescription = "Book a tent or a touring e-bike by the day, with add-ons, and pick it up in Innsbruck."
            }, "news_rental_service.jpg"),
            (new NewsItem {
                Title = "Vintage Bike Auctions Are Now Live",
                Short =
                    "Vintage Velo is auctioning restored steel road bikes, starting with a 1984 lugged classic. Place a bid before the timer runs out.",
                Full =
                    "<p>Some bikes deserve more than a price tag. Our marketplace partner <a href='/vintage-velo'>Vintage Velo</a> restores rare steel frames in their Portland workshop, and the most special ones now go to auction.</p>" +
                    "<p>First up is the <a href='/vintage-steel-road-bike-1984'>Vintage Steel Road Bike 1984</a>: a lugged steel frame stripped, repainted in its original colour, and rebuilt with serviced original components. It has been test ridden for more than 50 km.</p>" +
                    "<p>Bidding is open for fourteen days. Every bid shows on the product page, and we will e-mail you if someone outbids you. When the timer runs out, the highest bidder wins and receives an e-mail with a link to complete checkout.</p>",
                Published = true,
                AllowComments = true,
                StartDateUtc = DateTime.UtcNow.AddDays(-17),
                MetaTitle = "Vintage Bike Auctions Are Now Live",
                MetaDescription = "Bid on restored steel road bikes from Vintage Velo, starting with a 1984 lugged classic."
            }, "news_vintage_bike_auction.jpg"),
            (new NewsItem {
                Title = "Welcome to Our Vendor Marketplace",
                Short =
                    "Independent makers and specialists now sell alongside our own range, starting with Nordic Craft Collective, Alpine Rental Co., and Vintage Velo.",
                Full =
                    "<p>We have always wanted the store to feel like a good high street: our own range, plus small independents we admire. Today that becomes real with our vendor marketplace.</p>" +
                    "<ul>" +
                    "<li><a href='/nordic-craft-collective'>Nordic Craft Collective</a>: handmade stoneware, vases, and walnut boards from a shared Copenhagen workshop.</li>" +
                    "<li><a href='/alpine-rental-co'>Alpine Rental Co.</a>: camping gear and touring e-bikes to rent by the day.</li>" +
                    "<li><a href='/vintage-velo'>Vintage Velo</a>: restored steel bikes, sold at a fixed price or by auction.</li>" +
                    "</ul>" +
                    "<p>Marketplace products use the same cart, checkout, and returns as everything else, and each vendor has its own page where you can read their story and leave a review.</p>" +
                    "<p>Are you a maker who would like to sell with us? Apply for a vendor account from the link in the footer.</p>",
                Published = true,
                AllowComments = true,
                StartDateUtc = DateTime.UtcNow.AddDays(-28),
                MetaTitle = "Welcome to Our Vendor Marketplace",
                MetaDescription = "Independent makers now sell alongside our own range: Nordic Craft Collective, Alpine Rental Co., and Vintage Velo."
            }, "news_vendor_marketplace.jpg")
        };

        //oldest first: the storefront lists news by CreatedOnUtc, which is the insert time
        foreach (var (item, image) in news.OrderBy(n => n.Item.StartDateUtc))
        {
            item.PictureId = (await InsertSamplePicture(image, item.Title, Reference.None, item.Id, item.Title)).Id;
            await _newsItemRepository.InsertAsync(item);
            await InsertSlug(item.Id, EntityTypes.NewsItem, item.Title, seName => item.SeName = seName);
            await _newsItemRepository.UpdateAsync(item);
        }
    }
}
