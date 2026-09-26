using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallCategories()
    {
        var categoryLayoutInGridAndLines = _categoryLayoutRepository
            .Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
        if (categoryLayoutInGridAndLines == null)
            throw new Exception("Category layout cannot be loaded");

        var allCategories = new List<Category>();

        async Task<Category> AddDepartment(string name, string description, string metaTitle,
            string metaDescription, int displayOrder, string pictureFileName, bool flagNew = false)
        {
            var category = new Category {
                Name = name,
                Description = description,
                MetaTitle = metaTitle,
                MetaDescription = metaDescription,
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                ParentCategoryId = "",
                ShowOnHomePage = true,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = displayOrder
            };
            if (flagNew) {
                category.Flag = "New";
                category.FlagStyle = "badge-danger";
            }

            var picture = await InsertSamplePicture(pictureFileName, name, Reference.Category, category.Id);
            category.PictureId = picture.Id;

            allCategories.Add(category);
            return category;
        }

        async Task<Category> AddSubcategory(Category parent, string name, string description,
            string metaTitle, string metaDescription, int displayOrder, string pictureFileName)
        {
            var category = new Category {
                Name = name,
                Description = description,
                MetaTitle = metaTitle,
                MetaDescription = metaDescription,
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                ParentCategoryId = parent.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = displayOrder
            };

            var picture = await InsertSamplePicture(pictureFileName, name, Reference.Category, category.Id);
            category.PictureId = picture.Id;

            allCategories.Add(category);
            return category;
        }

        //Tech & Audio
        var techAudio = await AddDepartment(
            "Tech & Audio",
            "<p>Headphones, speakers, and smart devices chosen for how they sound and how well they fit into everyday life. Less clutter, better sound, quieter homes.</p>",
            "Tech & Audio",
            "Headphones, speakers, smart home devices, and wearables for a calmer, better-sounding home.",
            1, "category_tech_audio.jpg");
        await AddSubcategory(techAudio, "Headphones & Speakers",
            "<p>Over-ear, true wireless, and portable speakers built for daily listening. Rich sound without the bulk.</p>",
            "Headphones & Speakers",
            "Wireless headphones, earbuds, and speakers for music, calls, and everything in between.",
            1, "category_headphones_speakers.jpg");
        await AddSubcategory(techAudio, "Smart Home",
            "<p>Lighting, climate, and voice control that quietly makes a home easier to live in. Set it up once, forget it works.</p>",
            "Smart Home",
            "Smart bulbs, thermostats, and voice speakers that work together around the house.",
            2, "category_smart_home.jpg");
        await AddSubcategory(techAudio, "Wearables",
            "<p>GPS watches, fitness bands, and rings that track the numbers that matter without getting in the way of a run.</p>",
            "Wearables",
            "GPS watches, fitness bands, and smart rings for training, sleep, and everyday wear.",
            3, "category_wearables.jpg");

        //Computers
        var computers = await AddDepartment(
            "Computers",
            "<p>Laptops for work on the move and desktops you configure to the job, from a quiet mini PC to a creator workstation. Pick the processor, memory, and storage you need, and nothing you don't.</p>",
            "Computers",
            "Laptops and configurable desktop computers: choose the processor, memory, storage, and graphics.",
            2, "category_computers.jpg");
        await AddSubcategory(computers, "Laptops",
            "<p>Thin, light, and built to last a working day. Filter by processor, memory, storage, and screen size to find the right fit.</p>",
            "Laptops",
            "Laptops filtered by processor, memory, storage, screen size, and graphics.",
            1, "category_laptops.jpg");
        await AddSubcategory(computers, "Desktops",
            "<p>Build your own desktop: choose each component and see the price update as you go. Or start from a ready-made configuration.</p>",
            "Desktops",
            "Configurable desktop computers and workstations: build your own with the components you need.",
            2, "category_desktops.jpg");

        //Home & Living
        var homeLiving = await AddDepartment(
            "Home & Living",
            "<p>Kitchen tools, lighting, and textiles made to be used every day, not put away for best. Quiet materials, honest craft.</p>",
            "Home & Living",
            "Kitchen and coffee gear, lighting, and textiles for a home that feels considered, not staged.",
            3, "category_home_living.jpg");
        await AddSubcategory(homeLiving, "Kitchen & Coffee",
            "<p>Kettles, drippers, mugs, and beans for a slower, better morning ritual. Everything a good coffee corner needs.</p>",
            "Kitchen & Coffee",
            "Coffee gear, kettles, and handmade mugs for the kitchen counter.",
            1, "category_kitchen_coffee.jpg");
        await AddSubcategory(homeLiving, "Lighting",
            "<p>Floor, table, and pendant lamps that treat light as a material: warm, layered, and worth switching on at dusk.</p>",
            "Lighting",
            "Floor lamps, table lamps, and pendant lights for warmer evenings at home.",
            2, "category_lighting.jpg");
        await AddSubcategory(homeLiving, "Textiles & Decor",
            "<p>Throws, towels, cushions, and handmade decor pieces in natural fibres. Made to be lived in for years.</p>",
            "Textiles & Decor",
            "Linen throws, towels, cushions, and handmade decor for a softer living room.",
            3, "category_textiles_decor.jpg");

        //Fashion
        var fashion = await AddDepartment(
            "Fashion",
            "<p>Considered clothing and accessories in natural fabrics, built to last past a single season. Fewer, better pieces.</p>",
            "Fashion",
            "Women's and men's clothing, bags, and accessories in natural fabrics and honest cuts.",
            4, "category_fashion.jpg");
        await AddSubcategory(fashion, "Women",
            "<p>Knitwear, dresses, and everyday basics in merino, linen, and organic cotton, cut for movement and comfort.</p>",
            "Women's Clothing",
            "Women's knitwear, dresses, and basics in merino wool, linen, and organic cotton.",
            1, "category_women.jpg");
        await AddSubcategory(fashion, "Men",
            "<p>Waxed jackets, oxford shirts, and denim built for both the city and the field. Workwear-inspired, built to last.</p>",
            "Men's Clothing",
            "Men's jackets, shirts, and denim inspired by workwear and built for everyday use.",
            2, "category_men.jpg");
        await AddSubcategory(fashion, "Bags & Accessories",
            "<p>Leather bags, canvas totes, and small accessories that carry everything and outlast most trends.</p>",
            "Bags & Accessories",
            "Leather bags, canvas totes, wallets, and backpacks for every day and every trip.",
            3, "category_bags_accessories.jpg");

        //Outdoor & Active
        var outdoorActive = await AddDepartment(
            "Outdoor & Active",
            "<p>Tents, bikes, and running gear tested on long weekends and longer trails. Built for people who actually go outside.</p>",
            "Outdoor & Active",
            "Camping, cycling, and running gear tested on real trails and longer trips.",
            5, "category_outdoor_active.jpg", flagNew: true);
        await AddSubcategory(outdoorActive, "Camping",
            "<p>Tents, sleeping bags, and camp kitchen gear light enough to carry and tough enough to trust at altitude.</p>",
            "Camping",
            "Tents, sleeping bags, and camp gear for weekends off the grid.",
            1, "category_camping.jpg");
        await AddSubcategory(outdoorActive, "Cycling",
            "<p>Helmets, bikes, and rentals for commuting, touring, and everything in between, from vintage steel to electric.</p>",
            "Cycling",
            "Bikes, helmets, and cycling rentals for commuting, touring, and weekend rides.",
            2, "category_cycling.jpg");
        await AddSubcategory(outdoorActive, "Running",
            "<p>Trail shoes, running jackets, and hydration gear built to move with you, whatever the weather does.</p>",
            "Running",
            "Trail running shoes, jackets, and hydration gear for every kind of run.",
            3, "category_running.jpg");

        //Studio & Digital
        var studioDigital = await AddDepartment(
            "Studio & Digital",
            "<p>E-books, workshops, and gift vouchers you can send in seconds. Skills, guides, and gestures that need no shipping box.</p>",
            "Studio & Digital",
            "Downloadable guides, live workshops, and gift vouchers delivered instantly.",
            6, "category_studio_digital.jpg");
        await AddSubcategory(studioDigital, "E-books & Guides",
            "<p>Downloadable guides on coffee, hiking, lighting, and wardrobe planning, written by the people who make the products.</p>",
            "E-books & Guides",
            "Downloadable guides and e-books on coffee, hiking, lighting, and more.",
            1, "category_ebooks_guides.jpg");
        await AddSubcategory(studioDigital, "Workshops",
            "<p>Live and recorded sessions on coffee, cycling, and textile craft, taught by the brands behind our favourite products.</p>",
            "Workshops",
            "Online and in-person workshops on coffee, cycling, and craft skills.",
            2, "category_workshops.jpg");
        await AddSubcategory(studioDigital, "Gift vouchers",
            "<p>Digital and printed gift vouchers in a few sizes, ready to send for any occasion.</p>",
            "Gift Vouchers",
            "Digital and printed gift vouchers for any occasion.",
            3, "category_gift_vouchers.jpg");

        allCategories.ForEach(x => _categoryRepository.Insert(x));

        foreach (var category in allCategories)
            await InsertSlug(category.Id, EntityTypes.Category, category.Name, seName => category.SeName = seName);

        foreach (var category in allCategories)
            await _categoryRepository.UpdateAsync(category);
    }
}
