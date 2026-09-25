using Grand.Domain.Catalog;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Adds slugs, tags, related products, cross-sells, and reviews for all sample products.
    // Bundle items and grouped-child links are added by the department files that own the
    // grouped/bundle products, not here.
    protected virtual async Task InstallProductRelations(List<Product> all, SampleProductContext ctx)
    {
        var byName = all.ToDictionary(p => p.Name);

        foreach (var product in all)
            await InsertSlug(product.Id, EntityTypes.Product, product.Name, seName => product.SeName = seName);

        foreach (var (productName, tags) in SampleProductTags)
            foreach (var tag in tags)
                await AddProductTag(byName[productName], tag);

        foreach (var (productName, related, crossSells) in SampleProductRelations)
        {
            var product = byName[productName];
            for (var i = 0; i < related.Length; i++)
                product.RelatedProducts.Add(new RelatedProduct { ProductId2 = byName[related[i]].Id, DisplayOrder = i });
            foreach (var crossSell in crossSells)
                product.CrossSellProduct.Add(byName[crossSell].Id);
        }

        foreach (var (productName, title, text, rating) in SampleProductReviews)
        {
            var product = byName[productName];
            await _productReviewRepository.InsertAsync(new ProductReview {
                CustomerId = ctx.CustomerId,
                ProductId = product.Id,
                StoreId = ctx.StoreId,
                IsApproved = true,
                Title = title,
                ReviewText = text,
                Rating = rating
            });
            product.ApprovedRatingSum += rating;
            product.ApprovedTotalReviews += 1;
            product.AvgRating = (double)product.ApprovedRatingSum / product.ApprovedTotalReviews;
        }

        foreach (var product in all.Where(p => p.ProductTypeId == ProductType.Reservation))
            if (!_productReservationRepository.Table.Any(r => r.ProductId == product.Id))
                throw new Exception($"Reservation product {product.Name} has no slots");

        foreach (var product in all)
            await _productRepository.UpdateAsync(product);
    }

    private static readonly (string Product, string[] Tags)[] SampleProductTags = [
        ("Aurora Studio Wireless Headphones", ["headphones", "wireless", "noise cancelling"]),
        ("Aurora Pulse True Wireless Earbuds", ["earbuds", "wireless", "workout"]),
        ("Aurora Room Portable Speaker", ["speaker", "portable", "outdoor"]),
        ("Aurora Vinyl Bookshelf Speakers", ["speakers", "hifi", "vinyl"]),
        ("Luma Smart Bulb Starter Kit", ["smart home", "lighting", "bulbs"]),
        ("Nordvik Smart Thermostat", ["smart home", "thermostat", "energy"]),
        ("Aurora Home Voice Speaker", ["smart speaker", "voice", "home"]),
        ("Nordvik Air Quality Monitor", ["air quality", "sensor", "home office"]),
        ("Kinetic Pace GPS Watch", ["gps watch", "running", "fitness"]),
        ("Kinetic Loop Fitness Band", ["fitness tracker", "sleep", "wellness"]),
        ("Kinetic Sleep Ring", ["smart ring", "sleep", "wellness"]),
        ("Kinetic Sport Watch Strap", ["watch strap", "accessories"]),
        ("Nordvik Gooseneck Kettle", ["coffee", "kettle", "pour over"]),
        ("Ember Ceramic Pour Over Dripper", ["coffee", "pour over", "ceramic"]),
        ("Ember Single Origin Coffee Beans", ["coffee", "beans", "single origin"]),
        ("Slow Morning Coffee Bundle", ["coffee", "gift set", "bundle"]),
        ("Stoneware Mug Set", ["mugs", "handmade", "stoneware"]),
        ("Stoneware Mug Small", ["mugs", "handmade"]),
        ("Stoneware Mug Medium", ["mugs", "handmade"]),
        ("Stoneware Mug Large", ["mugs", "handmade"]),
        ("Walnut Serving Board", ["kitchen", "serving", "engraving"]),
        ("Luma Arc Floor Lamp", ["lighting", "floor lamp", "living room"]),
        ("Luma Glow Table Lamp", ["table lamp", "bedroom", "glass"]),
        ("Luma Paper Pendant Light", ["pendant", "paper lamp", "dining"]),
        ("Luma Rechargeable Lantern Lamp", ["lantern", "portable", "outdoor"]),
        ("Loom Linen Throw Blanket", ["throw", "linen", "living room"]),
        ("Loom Waffle Bath Towel Set", ["bath", "towels", "cotton"]),
        ("Hand Thrown Ceramic Vase", ["vase", "handmade", "decor"]),
        ("Wool Felt Cushion Cover", ["cushion", "wool", "decor"]),
        ("Loom Merino Wrap Cardigan", ["knitwear", "merino", "cardigan"]),
        ("Loom Linen Midi Dress", ["dress", "linen", "summer"]),
        ("Loom Organic Cotton Tee Women", ["t-shirt", "organic cotton", "basics"]),
        ("Loom Wide Leg Trousers", ["trousers", "workwear"]),
        ("Fieldstone Waxed Cotton Jacket", ["jacket", "waxed cotton", "outerwear"]),
        ("Fieldstone Oxford Shirt", ["shirt", "oxford", "workwear"]),
        ("Fieldstone Selvedge Denim Jeans", ["jeans", "denim", "selvedge"]),
        ("Fieldstone Merino Beanie", ["beanie", "merino", "winter"]),
        ("Ember Leather Weekender Bag", ["bag", "leather", "travel"]),
        ("Loom Canvas Tote Bag", ["tote", "canvas", "everyday"]),
        ("Fieldstone Leather Card Wallet", ["wallet", "leather", "gift"]),
        ("Trailforge Roll Top Backpack", ["backpack", "commute", "waterproof"]),
        ("Trailforge Two Person Tent", ["tent", "camping", "backpacking"]),
        ("Trailforge Down Sleeping Bag", ["sleeping bag", "down", "camping"]),
        ("Trailforge Camping Tent Rental", ["rental", "tent", "camping"]),
        ("Trailforge Insulated Camp Mug", ["mug", "camping", "insulated"]),
        ("Kinetic Commuter Helmet", ["helmet", "cycling", "safety"]),
        ("Touring E-Bike Rental", ["rental", "e-bike", "touring"]),
        ("Vintage Steel Road Bike 1984", ["vintage", "road bike", "auction"]),
        ("Refurbished City Bike", ["city bike", "refurbished", "commute"]),
        ("Kinetic Trail Running Shoes", ["running", "trail", "shoes"]),
        ("Kinetic Lightweight Running Jacket", ["running", "jacket", "windproof"]),
        ("Kinetic Hydration Vest", ["hydration", "trail", "running"]),
        ("Kinetic Running Socks Three Pack", ["socks", "running", "merino"]),
        ("The Slow Morning Coffee Guide", ["coffee", "e-book", "guide"]),
        ("Weekend Trails Field Guide", ["hiking", "e-book", "guide"]),
        ("Small Space Lighting Handbook", ["lighting", "e-book", "interiors"]),
        ("Capsule Wardrobe Planner", ["wardrobe", "planner", "e-book"]),
        ("Home Barista Online Workshop", ["coffee", "workshop", "online"]),
        ("Studio Membership Monthly", ["membership", "workshop", "subscription"]),
        ("Bike Maintenance Basics Workshop", ["cycling", "workshop", "repair"]),
        ("Natural Dye Textile Workshop", ["textiles", "workshop", "natural dye"]),
        ("Gift Voucher 25", ["gift voucher", "gift"]),
        ("Gift Voucher 50", ["gift voucher", "gift"]),
        ("Gift Voucher 100", ["gift voucher", "gift"]),
        ("Printed Gift Voucher Card", ["gift voucher", "printed card"]),
    ];

    private static readonly (string Product, string[] Related, string[] CrossSells)[] SampleProductRelations = [
        ("Aurora Studio Wireless Headphones", ["Aurora Pulse True Wireless Earbuds", "Aurora Room Portable Speaker", "Aurora Vinyl Bookshelf Speakers"], ["Trailforge Roll Top Backpack"]),
        ("Aurora Pulse True Wireless Earbuds", ["Aurora Studio Wireless Headphones", "Kinetic Loop Fitness Band"], ["Kinetic Hydration Vest"]),
        ("Aurora Room Portable Speaker", ["Aurora Home Voice Speaker", "Aurora Vinyl Bookshelf Speakers"], ["Luma Rechargeable Lantern Lamp"]),
        ("Luma Smart Bulb Starter Kit", ["Nordvik Smart Thermostat", "Aurora Home Voice Speaker", "Luma Glow Table Lamp"], ["Small Space Lighting Handbook"]),
        ("Nordvik Smart Thermostat", ["Nordvik Air Quality Monitor", "Luma Smart Bulb Starter Kit"], ["Aurora Home Voice Speaker"]),
        ("Kinetic Pace GPS Watch", ["Kinetic Loop Fitness Band", "Kinetic Sleep Ring"], ["Kinetic Sport Watch Strap", "Kinetic Trail Running Shoes"]),
        ("Nordvik Gooseneck Kettle", ["Ember Ceramic Pour Over Dripper", "Slow Morning Coffee Bundle"], ["Ember Single Origin Coffee Beans"]),
        ("Ember Ceramic Pour Over Dripper", ["Nordvik Gooseneck Kettle", "Stoneware Mug Set"], ["Ember Single Origin Coffee Beans"]),
        ("Ember Single Origin Coffee Beans", ["Slow Morning Coffee Bundle", "The Slow Morning Coffee Guide"], ["Ember Ceramic Pour Over Dripper"]),
        ("Slow Morning Coffee Bundle", ["Stoneware Mug Set", "Home Barista Online Workshop", "The Slow Morning Coffee Guide"], ["Ember Single Origin Coffee Beans", "Walnut Serving Board"]),
        ("Luma Arc Floor Lamp", ["Luma Glow Table Lamp", "Luma Paper Pendant Light"], ["Luma Smart Bulb Starter Kit"]),
        ("Loom Linen Throw Blanket", ["Wool Felt Cushion Cover", "Loom Waffle Bath Towel Set"], ["Hand Thrown Ceramic Vase"]),
        ("Loom Merino Wrap Cardigan", ["Loom Wide Leg Trousers", "Loom Organic Cotton Tee Women"], ["Loom Canvas Tote Bag"]),
        ("Loom Linen Midi Dress", ["Loom Organic Cotton Tee Women", "Loom Merino Wrap Cardigan"], ["Loom Canvas Tote Bag"]),
        ("Fieldstone Waxed Cotton Jacket", ["Fieldstone Oxford Shirt", "Fieldstone Selvedge Denim Jeans"], ["Fieldstone Merino Beanie", "Fieldstone Leather Card Wallet"]),
        ("Ember Leather Weekender Bag", ["Trailforge Roll Top Backpack", "Loom Canvas Tote Bag"], ["Fieldstone Leather Card Wallet"]),
        ("Trailforge Two Person Tent", ["Trailforge Camping Tent Rental", "Trailforge Down Sleeping Bag"], ["Trailforge Insulated Camp Mug", "Luma Rechargeable Lantern Lamp"]),
        ("Trailforge Camping Tent Rental", ["Trailforge Two Person Tent", "Touring E-Bike Rental"], ["Weekend Trails Field Guide"]),
        ("Vintage Steel Road Bike 1984", ["Refurbished City Bike", "Touring E-Bike Rental"], ["Kinetic Commuter Helmet"]),
        ("Refurbished City Bike", ["Vintage Steel Road Bike 1984", "Touring E-Bike Rental"], ["Kinetic Commuter Helmet", "Bike Maintenance Basics Workshop"]),
        ("Kinetic Trail Running Shoes", ["Kinetic Lightweight Running Jacket", "Kinetic Hydration Vest"], ["Kinetic Running Socks Three Pack", "Kinetic Pace GPS Watch"]),
        ("The Slow Morning Coffee Guide", ["Home Barista Online Workshop", "Weekend Trails Field Guide"], ["Ember Single Origin Coffee Beans"]),
        ("Gift Voucher 50", ["Gift Voucher 25", "Gift Voucher 100", "Printed Gift Voucher Card"], []),
    ];

    private static readonly (string Product, string Title, string Text, int Rating)[] SampleProductReviews = [
        ("Aurora Studio Wireless Headphones", "Quiet train, finally",
            "I commute an hour each way and the noise cancelling makes it feel like half that. Battery lasts me the whole week.", 5),
        ("Aurora Studio Wireless Headphones", "Comfortable for long calls",
            "I wear these for back-to-back meetings and my ears do not ache by the afternoon. The microphone is clear enough that nobody has asked me to repeat myself.", 5),
        ("Aurora Studio Wireless Headphones", "Great sound, snug fit",
            "Sound is rich and the app EQ is useful. They clamp a little tight at first but loosened up after a week.", 4),
        ("Aurora Pulse True Wireless Earbuds", "Stay in during runs",
            "These stay put on hill sprints, which no other earbuds have managed. The case is small enough for a running belt.", 5),
        ("Aurora Pulse True Wireless Earbuds", "Good, not perfect",
            "Pairing was instant and the sound is balanced. I wish the touch controls were a little less sensitive.", 4),
        ("Aurora Room Portable Speaker", "Our picnic companion",
            "Loud enough for the garden, and it survived a rain shower on the balcony. The terracotta color looks lovely.", 5),
        ("Nordvik Smart Thermostat", "Paid for itself",
            "Installation took twenty minutes and our heating bill dropped noticeably in the first month.", 5),
        ("Kinetic Pace GPS Watch", "Battery life is unreal",
            "I charged it before a four-day hike and came home with battery to spare. The offline maps got me back on trail twice.", 5),
        ("Kinetic Pace GPS Watch", "Accurate GPS",
            "Distances match my old watch and the route markers are spot on. The 46 mm size is big but easy to read on the move.", 5),
        ("Kinetic Pace GPS Watch", "Solid, steep learning curve",
            "It does everything I need, but it took a weekend to figure out all the menus. Worth it once set up.", 4),
        ("Kinetic Sleep Ring", "Easy to forget it is there",
            "Light enough that I sleep with it without noticing. The sleep scores have helped me fix my bedtime.", 4),
        ("Nordvik Gooseneck Kettle", "Pour control is excellent",
            "The spout gives a thin steady stream and the temperature hold means I can take my time. It also looks good on the counter.", 5),
        ("Nordvik Gooseneck Kettle", "Lovely but small",
            "Perfect for two cups of pour over. If you make tea for a crowd you will be refilling it often.", 4),
        ("Ember Single Origin Coffee Beans", "Fresh and fruity",
            "The roast date was four days before delivery and you can taste it. Bright, berry notes without any sourness.", 5),
        ("Ember Single Origin Coffee Beans", "Reordering monthly",
            "We buy six bags at a time now, the tier price makes it an easy habit. Filter grind is consistent.", 5),
        ("Ember Single Origin Coffee Beans", "Nice, a bit light for espresso",
            "Great in the pour over, but I prefer a darker roast for espresso. Will stick to it for filter.", 3),
        ("Slow Morning Coffee Bundle", "Perfect gift",
            "Bought this for my sister who was new to pour over and she has not stopped sending me coffee photos. Everything arrived beautifully packed.", 5),
        ("Slow Morning Coffee Bundle", "Great starter set",
            "Good value compared with buying the pieces separately. I would love a filter paper pack in the box.", 4),
        ("Stoneware Mug Set", "Every mug is slightly different",
            "You can tell they are handmade, in the best way. The medium size is my favourite for a flat white.", 5),
        ("Stoneware Mug Set", "Heavy in a good way",
            "They keep coffee warm for a long time and feel solid in the hand. The slate glaze is darker than the photo.", 4),
        ("Walnut Serving Board", "The engraving is beautiful",
            "Ordered with our wedding date engraved as an anniversary gift. The lettering is crisp and the wood grain is stunning.", 5),
        ("Luma Arc Floor Lamp", "Transformed our reading corner",
            "The arc reaches right over the armchair and the dimmer lets us go from reading light to evening glow.", 5),
        ("Luma Arc Floor Lamp", "Assembly needs two people",
            "Looks fantastic once up, but the base is heavy and the arm is long. Get a friend to help.", 4),
        ("Loom Linen Throw Blanket", "Softer every wash",
            "It started a little crisp and gets softer each time. The sage color works with everything on our sofa.", 5),
        ("Loom Linen Throw Blanket", "Good size",
            "Big enough for two people on the sofa. Wish it came in a darker green.", 4),
        ("Loom Merino Wrap Cardigan", "My autumn uniform",
            "Warm without being bulky and it has not pilled after a month of wear. I sized down and it fits perfectly.", 5),
        ("Loom Merino Wrap Cardigan", "Beautiful colour",
            "The oat is a lovely warm neutral. Runs a touch long in the sleeves for me.", 4),
        ("Fieldstone Waxed Cotton Jacket", "Built to last",
            "Wore it through a wet week in the hills and stayed dry. It already looks better with a few creases.", 5),
        ("Fieldstone Waxed Cotton Jacket", "Worth the backorder",
            "My size in navy was out of stock but it arrived within two weeks as promised. Fits true to size.", 5),
        ("Fieldstone Waxed Cotton Jacket", "Stiff at first",
            "The wax makes it stiff for the first few wears, which I expected. Pockets are deep and well placed.", 4),
        ("Ember Leather Weekender Bag", "Fits a long weekend",
            "Three days of clothes, shoes, and a laptop, and it still fits in the overhead bin. The monogram was a nice touch.", 5),
        ("Trailforge Two Person Tent", "Up in five minutes",
            "Pitched it alone in the wind on the first try. Plenty of room for two and our packs in the vestibules.", 5),
        ("Trailforge Two Person Tent", "Light and dry",
            "Survived a night of heavy rain without a drop inside. Only wish the stuff sack was a little bigger.", 4),
        ("Trailforge Camping Tent Rental", "Easy pickup and return",
            "Booked for a long weekend, the tent was clean and complete, and the camp stove add-on saved us buying one.", 5),
        ("Kinetic Trail Running Shoes", "Grip for days",
            "Handled wet roots and loose gravel with confidence. Comfortable straight out of the box.", 5),
        ("Kinetic Trail Running Shoes", "Great on trails, firm on road",
            "Excellent off road, but they feel a bit firm on long tarmac stretches.", 4),
        ("Kinetic Trail Running Shoes", "Size up half a size",
            "Toe box is snug if you have wide feet. Once I swapped for the next size they were perfect.", 4),
        ("The Slow Morning Coffee Guide", "Clear and friendly",
            "Finally understood why my coffee was bitter. The brew ratio charts are now taped inside my cupboard.", 5),
    ];
}
