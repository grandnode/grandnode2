using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallBrands()
    {
        var brandLayoutInGridAndLines = _brandLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
        if (brandLayoutInGridAndLines == null)
            throw new Exception("Brand layout cannot be loaded");

        var allBrands = new List<Brand> {
            new() {
                Name = "Aurora Audio",
                Description =
                    "<p>Aurora Audio started in a converted rehearsal studio, where a small group of sound " +
                    "engineers kept running into the same problem: every set of headphones or speakers they " +
                    "tried seemed to want credit for the music, adding a signature of its own instead of " +
                    "getting out of the way. So they built their own, tuning each driver by ear across " +
                    "hundreds of late-night listening sessions until the sound simply disappeared into the " +
                    "room.</p><p>That founding obsession still shapes everything we make: headphones, " +
                    "earbuds, and speakers designed to sit quietly in your day rather than announce " +
                    "themselves. We choose warm, honest materials over flashy finishes, and every product " +
                    "ships only once our engineers would happily wear it on their own commute.</p>",
                MetaTitle = "Aurora Audio",
                MetaKeywords = "Aurora Audio, headphones, speakers, audio",
                MetaDescription = "Aurora Audio builds headphones and speakers that disappear into a room, never into the background.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                ShowOnHomePage = true,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 1
            },
            new() {
                Name = "Nordvik Home",
                Description =
                    "<p>Nordvik Home began with a shared frustration among a group of Scandinavian product " +
                    "designers: the smart gadgets and kitchen tools they brought home always ended up back " +
                    "in a drawer within a month, too clumsy or too cold to leave out. They set out to design " +
                    "the opposite kind of object, one that earns a permanent spot on the counter because it " +
                    "is genuinely good to look at and to use every single day.</p><p>Today that means " +
                    "kettles, thermostats, and air monitors built from matte metal and quiet color palettes, " +
                    "with interfaces simple enough that nobody needs the manual. We test every piece in our " +
                    "own kitchens first, and if it does not earn its counter space there, it does not ship.</p>",
                MetaTitle = "Nordvik Home",
                MetaKeywords = "Nordvik Home, kitchen, smart home",
                MetaDescription = "Nordvik Home makes quiet, well-made kitchen and smart home tools designed to live on the counter.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 2
            },
            new() {
                Name = "Ember & Oak",
                Description =
                    "<p>Ember & Oak grew out of a shared workshop between a coffee roaster and a " +
                    "leatherworker who kept trading goods across the same wooden counter: a bag of beans " +
                    "for a repaired strap, a fresh roast for a new wallet. They realized their crafts shared " +
                    "the same philosophy, small batches, honest materials, and things that only look better " +
                    "with age, so they put both names on the door.</p><p>We still roast in small batches " +
                    "every week and cut and stitch leather goods by hand in the same building. A weekender " +
                    "bag from us will crease and darken with use, and a bag of beans will always carry a " +
                    "roast date you can trust, because the people who make them still taste and touch " +
                    "everything before it leaves the shop.</p>",
                MetaTitle = "Ember & Oak",
                MetaKeywords = "Ember & Oak, coffee, leather goods",
                MetaDescription = "Ember & Oak roasts small-batch coffee and crafts leather goods that get better with use.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                ShowOnHomePage = true,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 3
            },
            new() {
                Name = "Luma Lighting",
                Description =
                    "<p>Luma Lighting was founded by a pair of lighting designers who had spent years working " +
                    "on hotel lobbies and restaurant interiors, and who noticed how rarely that same care " +
                    "made it into ordinary homes. They left commercial design to build lamps that treat light " +
                    "the way a good interior treats fabric or wood, as a material to be layered, softened, " +
                    "and shaped rather than simply switched on.</p><p>Every lamp we make, from a brass floor " +
                    "lamp to a paper pendant, is designed around the hour it gets switched on: the warm, " +
                    "low-angle glow of dusk. We test color temperature and dimming curves as carefully as we " +
                    "test durability, because a lamp that looks right in a showroom but harsh at home has " +
                    "failed at its only job.</p>",
                MetaTitle = "Luma Lighting",
                MetaKeywords = "Luma Lighting, lamps, lighting",
                MetaDescription = "Luma Lighting makes lamps that treat light as a material, soft, layered, and made for dusk.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 4
            },
            new() {
                Name = "Loom & Thread",
                Description =
                    "<p>Loom & Thread was started by a weaver who learned the craft from her grandmother and " +
                    "grew tired of watching beautifully made fabric turned into clothes and homewares that " +
                    "fell apart within a season. She began weaving linen and organic cotton on her own looms, " +
                    "then teamed up with a small pattern-cutting studio to turn those bolts of cloth into " +
                    "clothing and textiles built to be worn and washed for years, not one summer.</p><p>Every " +
                    "cardigan, dress, and throw blanket we sell starts with natural fibres and honest cuts, " +
                    "nothing overworked, nothing that needs a special detergent. We would rather you wear a " +
                    "Loom & Thread piece until it softens and fades exactly the way you like it than buy a " +
                    "replacement next year.</p>",
                MetaTitle = "Loom & Thread",
                MetaKeywords = "Loom & Thread, textiles, natural fibres, apparel",
                MetaDescription = "Loom & Thread weaves natural fibres into honest cuts and textiles made to be lived in for years.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 5
            },
            new() {
                Name = "Fieldstone Apparel",
                Description =
                    "<p>Fieldstone Apparel was founded by a former workwear buyer who wanted clothes with the " +
                    "durability of a work jacket and the fit of something you would actually wear to dinner. " +
                    "Working with a small pattern room, he built a first collection of waxed cotton jackets " +
                    "and selvedge denim designed to survive a wet hike on Saturday and still look sharp on " +
                    "Monday morning.</p><p>That balance between field and city is still the brief for every " +
                    "piece we make. We use waxed cotton, raw denim, and merino wool because they age well and " +
                    "perform hard, and we cut everything to a fit that works equally under a rain shell or a " +
                    "blazer. If a stitch cannot survive a weekend outdoors, it does not go into a Fieldstone " +
                    "garment.</p>",
                MetaTitle = "Fieldstone Apparel",
                MetaKeywords = "Fieldstone Apparel, menswear, workwear",
                MetaDescription = "Fieldstone Apparel makes workwear-inspired menswear in waxed cotton, denim, and merino, built for city and field.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 6
            },
            new() {
                Name = "Trailforge",
                Description =
                    "<p>Trailforge began with three friends who spent every long weekend somewhere on a trail " +
                    "and kept coming home with gear that had failed them: zippers that jammed, packs that dug " +
                    "into their shoulders, tents that flooded in the first real storm. Frustrated, they " +
                    "started prototyping their own gear in a garage, testing each version on the same trails " +
                    "that had exposed the last one's weaknesses.</p><p>That habit never stopped. Every tent, " +
                    "sleeping bag, and backpack we sell is carried on real trips before it ever reaches a " +
                    "product page, and we keep the weight down and the materials tough because the people " +
                    "designing this gear are still the ones carrying it up the hill. Long weekends turned " +
                    "into longer trails, and Trailforge grew to match.</p>",
                MetaTitle = "Trailforge",
                MetaKeywords = "Trailforge, camping, hiking, outdoor gear",
                MetaDescription = "Trailforge makes lightweight camping and carry gear, tested on long weekends and longer trails.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                ShowOnHomePage = true,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 7
            },
            new() {
                Name = "Kinetic",
                Description =
                    "<p>Kinetic was founded by a pair of amateur triathletes who kept strapping mismatched " +
                    "gadgets to their wrists and bikes to track a single training block, and decided the " +
                    "data and the gear should come from the same place. They started with a GPS watch built " +
                    "specifically for runners and cyclists who care about pace and route more than notifications, " +
                    "then expanded into the shoes, jackets, and helmets that go with it.</p><p>Every product we " +
                    "make, from a trail running shoe to a fitness band, is tested by our own training group " +
                    "across real routes and real weather, not a lab treadmill. We build gear and wearables " +
                    "that work together, so a Kinetic watch can talk to a Kinetic vest, and every kilometre " +
                    "gets measured the way you actually run or ride it.</p>",
                MetaTitle = "Kinetic",
                MetaKeywords = "Kinetic, running, cycling, wearables",
                MetaDescription = "Kinetic makes running and cycling gear, plus the wearables that measure every kilometre.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 8
            },
            new() {
                Name = "Vertex Computing",
                Description =
                    "<p>Vertex Computing was founded by a handful of systems engineers who were tired of " +
                    "choosing between computers that were powerful but loud and computers that were quiet " +
                    "but compromised. They started building machines around cooling first, then picked " +
                    "every component to match: processors, memory, and storage chosen for how they behave " +
                    "under a real working day, not just in a benchmark.</p><p>Today we make laptops that " +
                    "last from the first meeting to the last train home, and desktops you configure part by " +
                    "part, so you pay for the performance you need and nothing you don't. Every machine is " +
                    "assembled, tested, and burned in before it ships.</p>",
                MetaTitle = "Vertex Computing",
                MetaKeywords = "Vertex Computing, laptops, desktops, workstation, build your own PC",
                MetaDescription = "Vertex Computing builds quiet, well-cooled laptops and desktops you configure part by part.",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Published = true,
                DisplayOrder = 9
            }
        };

        var images = new Dictionary<string, string> {
            ["Aurora Audio"] = "brand_aurora_audio.jpg",
            ["Nordvik Home"] = "brand_nordvik_home.jpg",
            ["Ember & Oak"] = "brand_ember_oak.jpg",
            ["Luma Lighting"] = "brand_luma_lighting.jpg",
            ["Loom & Thread"] = "brand_loom_thread.jpg",
            ["Fieldstone Apparel"] = "brand_fieldstone_apparel.jpg",
            ["Trailforge"] = "brand_trailforge.jpg",
            ["Kinetic"] = "brand_kinetic.jpg",
            ["Vertex Computing"] = "brand_vertex_computing.jpg"
        };

        foreach (var brand in allBrands)
        {
            await _brandRepository.InsertAsync(brand);
            brand.PictureId = (await InsertSamplePicture(images[brand.Name], brand.Name, Reference.Brand, brand.Id)).Id;
            await InsertSlug(brand.Id, EntityTypes.Brand, brand.Name, seName => brand.SeName = seName);
            await _brandRepository.UpdateAsync(brand);
        }
    }
}
