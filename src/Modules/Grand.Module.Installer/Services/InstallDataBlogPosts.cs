using Grand.Domain.Blogs;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallBlogPosts()
    {
        var guides = new BlogCategory { Name = "Guides", DisplayOrder = 1 };
        var inspiration = new BlogCategory { Name = "Inspiration", DisplayOrder = 2 };
        var makerStories = new BlogCategory { Name = "Maker Stories", DisplayOrder = 3 };

        var pourOver = new BlogPost {
            Title = "How to Brew Better Pour Over at Home",
            BodyOverview =
                "<p>Good pour over comes down to four things we can all control: fresh beans, the right grind, water just off the boil, and a slow, steady pour. Here is the everyday recipe we use, and what to change when a cup tastes sour or bitter.</p>",
            Body =
                "<p>Pour over has a reputation for being fussy, but most of the difference between a flat cup and a great one comes from four things you can control on any kitchen counter: fresh beans, the right grind, water just off the boil, and a slow, steady pour. Get those right and the rest is taste.</p>" +
                "<h2>Start with fresh beans</h2>" +
                "<p>Coffee is at its best between one and four weeks after roasting. Buy whole beans in small bags, keep them in an airtight container away from light, and grind just before you brew. Our <a href='/ember-single-origin-coffee-beans'>Ember Single Origin Coffee Beans</a> carry a roast date on every bag for exactly this reason.</p>" +
                "<h2>Grind like coarse sea salt</h2>" +
                "<p>For a single cup through a cone dripper, aim for a medium-fine grind that looks like coarse sea salt. Too fine and the water stalls, pulling out bitterness. Too coarse and it rushes through, leaving the cup thin and sour.</p>" +
                "<h2>Our everyday recipe</h2>" +
                "<ul>" +
                "<li><strong>One cup:</strong> 15 g coffee to 250 g water.</li>" +
                "<li><strong>Two cups:</strong> 28 g coffee to 460 g water.</li>" +
                "<li><strong>Water:</strong> 93 to 96 °C, about thirty seconds off the boil.</li>" +
                "</ul>" +
                "<p>Rinse the paper filter in the <a href='/ember-ceramic-pour-over-dripper'>Ember Ceramic Pour Over Dripper</a> to warm it and wash out any papery taste. Add the coffee, level the bed, and pour about twice its weight in water to let it bloom for thirty seconds. You will see it bubble as trapped gas escapes.</p>" +
                "<p>Then pour in slow circles, keeping the water level steady, until you reach your target weight. A gooseneck spout makes this far easier: the <a href='/nordvik-gooseneck-kettle'>Nordvik Gooseneck Kettle</a> holds its temperature to the degree and gives you a thin, controllable stream. The whole brew should take around three minutes.</p>" +
                "<h2>Taste and adjust</h2>" +
                "<ul>" +
                "<li><strong>Sour or thin:</strong> grind a little finer or pour a little slower.</li>" +
                "<li><strong>Bitter or dry:</strong> grind a little coarser or use slightly cooler water.</li>" +
                "<li><strong>Weak:</strong> add a gram or two of coffee rather than shortening the brew.</li>" +
                "</ul>" +
                "<p>Change one thing at a time and write it down. After a week of mornings you will know exactly how you like your cup.</p>" +
                "<p>If you are starting from scratch, the <a href='/slow-morning-coffee-bundle'>Slow Morning Coffee Bundle</a> puts the kettle, the dripper, and a bag of beans in one box, so the first brew can happen the morning it arrives.</p>",
            Tags = "coffee, how-to, pour over",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-4),
            MetaTitle = "How to Brew Better Pour Over at Home",
            MetaDescription = "A simple pour over recipe with ratios, grind size, and water temperature, plus how to fix a sour or bitter cup."
        };

        var tent = new BlogPost {
            Title = "Choosing Your First Two Person Tent",
            BodyOverview =
                "<p>Weight, weather, and floor space all matter, but not equally for every trip. We explain what the numbers on a tent spec sheet mean, and why renting for your first season can be the smartest way to find out what you need.</p>",
            Body =
                "<p>A first tent is a big purchase, and spec sheets do not make it easier. Packed weight, hydrostatic head, vestibule area: every number matters, but not equally for every trip. Here is how we think about it.</p>" +
                "<h2>Decide how you will carry it</h2>" +
                "<p>If the tent rides in a car boot, weight barely matters and you can buy for comfort. If it goes on your back, every gram counts. For backpacking, a two person tent under 2 kg is a good target, and splitting the poles and body between two packs makes it lighter still.</p>" +
                "<h2>Read the weather numbers</h2>" +
                "<ul>" +
                "<li><strong>Hydrostatic head</strong> tells you how waterproof the fabric is. 1,500 mm handles summer showers; 3,000 mm and above handles a proper storm.</li>" +
                "<li><strong>Season rating:</strong> a three season tent covers spring to autumn. Unless you camp in snow, you do not need a four season shelter.</li>" +
                "<li><strong>Pole count and geometry</strong> decide how well the tent sheds wind. More crossing poles mean more stability.</li>" +
                "</ul>" +
                "<h2>Freestanding or trekking pole?</h2>" +
                "<p>A freestanding tent stands up on its own poles and can be moved after pitching, which makes it easy on rock and sand. A trekking pole tent uses your hiking poles as its frame and saves weight, but needs good pegging. For a first tent we usually suggest freestanding: it is more forgiving while you learn.</p>" +
                "<h2>Think about space</h2>" +
                "<p>A two person tent fits two people, but not always two people and their packs. Look for two doors and two vestibules, so nobody climbs over anyone at 3 a.m. and wet boots stay outside.</p>" +
                "<p>Our <a href='/trailforge-two-person-tent'>Trailforge Two Person Tent</a> is freestanding, weighs 1.6 kg, and has a door and vestibule on each side. Pair it with the <a href='/trailforge-down-sleeping-bag'>Trailforge Down Sleeping Bag</a> for three season nights.</p>" +
                "<h2>Not sure yet? Rent first</h2>" +
                "<p>If you only camp a few weekends a year, or want to try before you commit, the <a href='/trailforge-camping-tent-rental'>Trailforge Camping Tent Rental</a> lets you book the same tent by the day and add a sleeping pad or a stove. One season of rentals will tell you more about what you need than any spec sheet.</p>",
            Tags = "camping, how-to, tents",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-12),
            MetaTitle = "Choosing Your First Two Person Tent",
            MetaDescription = "What tent weights, waterproof ratings, and designs mean, and when renting is the smarter first step."
        };

        var lighting = new BlogPost {
            Title = "Layering Light in a Small Living Room",
            BodyOverview =
                "<p>One ceiling light makes a room flat. Three layers of light, ambient, task, and accent, can make a small living room feel bigger and warmer. Here is how to plan them.</p>",
            Body =
                "<p>Most small living rooms are lit by one ceiling fixture that does everything badly. It flattens the room, throws shadows where you read, and feels like an office after dark. The fix is not more light, but light in layers.</p>" +
                "<h2>Layer one: ambient</h2>" +
                "<p>Ambient light fills the room softly so you can move around. In a small space, bounce it off walls and ceilings instead of pointing it down. A paper shade diffuses a bulb into an even glow; the <a href='/luma-paper-pendant-light'>Luma Paper Pendant Light</a> hung low over a dining corner also marks that corner as its own zone, which makes the room read as larger.</p>" +
                "<h2>Layer two: task</h2>" +
                "<p>Task light goes where you do things: reading, knitting, working on a laptop. It should be brighter and come from the side, not above, so your hands and the page are not in shadow. An arc lamp is perfect for a sofa because the head reaches over you while the base stays out of the way. The <a href='/luma-arc-floor-lamp'>Luma Arc Floor Lamp</a> swings over a reading chair without taking up floor space in front of it.</p>" +
                "<h2>Layer three: accent</h2>" +
                "<p>Accent light is the warm pool that makes a room feel lived in: a small lamp on a shelf, a glow behind a plant, a light on a picture. Keep it low and warm, around 2,700 K. The <a href='/luma-glow-table-lamp'>Luma Glow Table Lamp</a> dims down to a candle-like level for evenings.</p>" +
                "<h2>Tie it together after sunset</h2>" +
                "<ul>" +
                "<li>Switch the ceiling light off once the lamps are on.</li>" +
                "<li>Use warmer bulbs in the evening and cooler ones for daytime work.</li>" +
                "<li>Put lamps on schedules so the room shifts on its own.</li>" +
                "</ul>" +
                "<p>Smart bulbs make the last step effortless: the <a href='/luma-smart-bulb-starter-kit'>Luma Smart Bulb Starter Kit</a> lets you set a warm evening scene that turns on at dusk, so the room is ready before you are.</p>",
            Tags = "lighting, interiors, small spaces",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-21),
            MetaTitle = "Layering Light in a Small Living Room",
            MetaDescription = "How ambient, task, and accent lighting make a small living room feel bigger and warmer."
        };

        var capsule = new BlogPost {
            Title = "A Ten Piece Capsule Wardrobe for Autumn",
            BodyOverview =
                "<p>Fewer, better clothes make mornings easier. We built a ten piece autumn wardrobe around natural fibres and quiet colours that combines into more than twenty outfits, plus a planner to map out your own.</p>",
            Body =
                "<p>A capsule wardrobe is not about owning as little as possible. It is about owning pieces that all work together, so getting dressed takes a minute and every item gets worn. Autumn is the easiest season to start, because layering does half the work.</p>" +
                "<h2>Pick a palette first</h2>" +
                "<p>Choose two neutrals and one accent colour. We went with oat and charcoal, plus a deep forest green. When everything shares a palette, almost any top works with any bottom.</p>" +
                "<h2>The ten pieces</h2>" +
                "<ul>" +
                "<li><strong>Outer layers:</strong> a waxed cotton jacket and a merino wrap cardigan.</li>" +
                "<li><strong>Tops:</strong> two organic cotton tees, one oxford shirt, one fine knit jumper.</li>" +
                "<li><strong>Bottoms:</strong> wide leg trousers and straight selvedge jeans.</li>" +
                "<li><strong>Extras:</strong> a merino beanie and a canvas tote.</li>" +
                "</ul>" +
                "<h2>Why these work</h2>" +
                "<p>The <a href='/loom-merino-wrap-cardigan'>Loom Merino Wrap Cardigan</a> is warm without bulk and goes over a tee or under a coat. The <a href='/fieldstone-waxed-cotton-jacket'>Fieldstone Waxed Cotton Jacket</a> shrugs off drizzle and softens with every wear. <a href='/loom-wide-leg-trousers'>Loom Wide Leg Trousers</a> dress up with a shirt or down with a tee, and take you from a desk to a dinner.</p>" +
                "<h2>More than twenty outfits</h2>" +
                "<p>Two bottoms times four tops already gives eight combinations. Add or remove a layer and you are past twenty without buying anything new. Natural fibres help too: merino and cotton breathe, so you can wear them more often between washes.</p>" +
                "<h2>Plan your own</h2>" +
                "<p>Start with what you already own and fill only the gaps. Our <a href='/capsule-wardrobe-planner'>Capsule Wardrobe Planner</a> walks you through choosing a palette, counting what you have, and mapping outfits week by week, and a free sample page is available to download from the product page.</p>",
            Tags = "fashion, capsule wardrobe, autumn",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-30),
            MetaTitle = "A Ten Piece Capsule Wardrobe for Autumn",
            MetaDescription = "Ten natural-fibre pieces in a quiet palette that combine into more than twenty autumn outfits."
        };

        var makers = new BlogPost {
            Title = "Meet the Makers of Nordic Craft Collective",
            BodyOverview =
                "<p>We spent a morning in a shared Copenhagen workshop with the potters and woodworkers of Nordic Craft Collective, and learned why no two mugs come out of the kiln the same.</p>",
            Body =
                "<p>Nordic Craft Collective works out of a shared workshop in Copenhagen's Nørrebro district: two wheels, one kiln, a long workbench, and a radio that never seems to be switched off. We spent a morning there to see how the pieces they sell through our marketplace are made.</p>" +
                "<h2>No two mugs alike</h2>" +
                "<p>Every mug in the <a href='/stoneware-mug-set'>Stoneware Mug Set</a> is thrown by hand, trimmed the next day, and glazed in an oat glaze mixed in the studio. The glaze pools differently in every firing, so each mug has its own speckle and colour shift. That is why you choose a size rather than an exact piece.</p>" +
                "<blockquote>We could make them identical with moulds. But then you would just have a mug, not your mug.</blockquote>" +
                "<h2>Three days of oil</h2>" +
                "<p>The <a href='/walnut-serving-board'>Walnut Serving Board</a> is cut from offcuts left by a local furniture maker. After shaping and sanding, each board gets a coat of food-safe oil every day for three days, which brings out the grain and protects it from moisture. They even engrave names and dates by hand for gifts.</p>" +
                "<h2>Slow pieces, small batches</h2>" +
                "<p>A <a href='/hand-thrown-ceramic-vase'>Hand Thrown Ceramic Vase</a> takes about two weeks from clay to shelf, most of it spent drying. The collective fires once a week, so stock arrives in small batches rather than all at once.</p>" +
                "<h2>Why a marketplace</h2>" +
                "<p>For a small studio, running a web shop means photography, customer service, and shipping on top of making. Selling through our marketplace lets the collective spend their time at the wheel while we handle the rest. For you, it means handmade pieces with the same checkout, delivery, and returns as everything else in the store.</p>",
            Tags = "makers, handmade, marketplace",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-40),
            MetaTitle = "Meet the Makers of Nordic Craft Collective",
            MetaDescription = "Inside the Copenhagen workshop where our handmade mugs, vases, and walnut boards are made."
        };

        var roadBike = new BlogPost {
            Title = "Giving an Old Road Bike a Second Life",
            BodyOverview =
                "<p>Vintage Velo rescues steel frames from sheds and scrapyards and brings them back to the road. We followed one 1984 road bike from rusted frame to auction listing.</p>",
            Body =
                "<p>The bike arrived at Vintage Velo's Portland workshop in two bin bags: a rusted steel frame, a seized headset, and wheels that no longer spun. Six weeks later it was one of the most beautiful things we have ever listed. Here is what happened in between.</p>" +
                "<h2>Why steel is worth saving</h2>" +
                "<p>Lugged steel frames from the 1980s were built to be repaired. Rust on the surface can be stripped, dents can be pulled, and parts follow standards that are still easy to find. A good steel frame rides with a springy, lively feel that many riders still prefer.</p>" +
                "<h2>From frame to finish</h2>" +
                "<ul>" +
                "<li><strong>Strip and inspect:</strong> the frame is stripped to bare metal and checked for cracks around the lugs and dropouts.</li>" +
                "<li><strong>Paint:</strong> a new coat of paint in the original colour, with the old decals recreated by hand.</li>" +
                "<li><strong>Rebuild:</strong> original components are cleaned and serviced; worn parts like tyres, cables, and brake pads are replaced.</li>" +
                "<li><strong>Test ride:</strong> every bike is ridden at least 50 km before it is listed.</li>" +
                "</ul>" +
                "<h2>Why an auction?</h2>" +
                "<p>Truly rare frames have no fair fixed price, so Vintage Velo lets riders decide. The <a href='/vintage-steel-road-bike-1984'>Vintage Steel Road Bike 1984</a> is open for bids now; place yours before the timer runs out.</p>" +
                "<h2>Buying a pre-owned bike</h2>" +
                "<ul>" +
                "<li>Ask for the frame size, and check the standover height against your own.</li>" +
                "<li>Look closely at the lugs and dropouts for cracks.</li>" +
                "<li>Budget for tyres and cables if they have not been replaced.</li>" +
                "</ul>" +
                "<p>If an auction feels like too much of a gamble, the <a href='/refurbished-city-bike'>Refurbished City Bike</a> gets the same full rebuild at a fixed price. Whichever you choose, add a <a href='/kinetic-commuter-helmet'>Kinetic Commuter Helmet</a> before the first ride.</p>",
            Tags = "cycling, vintage, restoration",
            AllowComments = true,
            StartDateUtc = DateTime.UtcNow.AddDays(-52),
            MetaTitle = "Giving an Old Road Bike a Second Life",
            MetaDescription = "How a 1984 steel road bike was restored from a rusted frame, and what to check when buying a pre-owned bike."
        };

        var posts = new List<(BlogPost Post, BlogCategory Category, string Image)> {
            (pourOver, guides, "blog_brew_better_pour_over.jpg"),
            (tent, guides, "blog_first_two_person_tent.jpg"),
            (lighting, inspiration, "blog_layering_light_small_living_room.jpg"),
            (capsule, inspiration, "blog_capsule_wardrobe_autumn.jpg"),
            (makers, makerStories, "blog_meet_nordic_craft_collective.jpg"),
            (roadBike, makerStories, "blog_old_road_bike_second_life.jpg")
        };

        //oldest first: the storefront lists posts by CreatedOnUtc, which is the insert time
        foreach (var (post, category, image) in posts.OrderBy(p => p.Post.StartDateUtc))
        {
            post.PictureId = (await InsertSamplePicture(image, post.Title, Reference.Blog, post.Id, post.Title)).Id;
            await _blogPostRepository.InsertAsync(post);
            await InsertSlug(post.Id, EntityTypes.BlogPost, post.Title, seName => post.SeName = seName);
            await _blogPostRepository.UpdateAsync(post);
            category.BlogPosts.Add(new BlogCategoryPost { BlogPostId = post.Id });
        }

        foreach (var category in new[] { guides, inspiration, makerStories })
        {
            category.SeName = SeoExtensions.GenerateSlug(category.Name, false, false, false);
            await _blogCategoryRepository.InsertAsync(category);
        }
    }
}
