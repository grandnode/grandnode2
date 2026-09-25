using Grand.Domain.Catalog;
using Grand.Domain.Media;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Digital department products (e-books, workshops, membership, gift vouchers): inserts
    // products (with pictures, categories, brand, specs, downloads) and returns them. Does not
    // create slugs, tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsDigital(SampleProductContext ctx)
    {
        var products = new List<Product>();
        var taxCategoryId = TaxCategoryId("Downloadable Products");

        async Task<Download> InsertDownload(string url, string extension, string contentType, string filename)
        {
            var download = new Download {
                DownloadGuid = Guid.NewGuid(),
                ContentType = contentType,
                UseDownloadUrl = true,
                DownloadUrl = url,
                Extension = extension,
                Filename = filename,
                DownloadType = DownloadType.Product
            };
            await _downloadRepository.InsertAsync(download);
            return download;
        }

        // Shared boilerplate for every product in this department: none of them ship except the
        // printed gift voucher card (set separately on that product), and all use the
        // Downloadable Products tax category.
        Product NewDigitalProduct(string sku, string name, double price, string subcategory, int stockQuantity) =>
            new() {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Sku = sku,
                Name = name,
                Price = price,
                ProductLayoutId = ctx.SimpleLayoutId,
                DeliveryDateId = ctx.DeliveryDateId,
                TaxCategoryId = taxCategoryId,
                IsShipEnabled = false,
                Weight = 0,
                Length = 0,
                Width = 0,
                Height = 0,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = stockQuantity,
                NotifyAdminForQuantityBelow = 5,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 100,
                Published = true,
                AllowCustomerReviews = true,
                MetaTitle = name,
                ProductCategories = {
                    new ProductCategory { CategoryId = CategoryId(subcategory), DisplayOrder = 1 }
                }
            };

        #region E-books & Guides

        // 54: The Slow Morning Coffee Guide — download + sample
        var downloadCoffeeGuideMain = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_cyberpunk_1.zip",
            ".zip", "application/x-zip-co", "The Slow Morning Coffee Guide");
        var downloadCoffeeGuideSample = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_cyberpunk_2.txt",
            ".txt", "text/plain", "The Slow Morning Coffee Guide (sample)");

        var productCoffeeGuide = NewDigitalProduct("SD-001", "The Slow Morning Coffee Guide", 14.99,
            "E-books & Guides", 5000);
        productCoffeeGuide.BrandId = BrandId("Ember & Oak");
        productCoffeeGuide.ShortDescription =
            "A 64-page PDF guide to slowing down your morning brew, from bean to cup.";
        productCoffeeGuide.FullDescription =
            "<p>Written with our roastery partners, this 64-page guide walks you through choosing beans, " +
            "dialling in a grind, and pulling a better pour over or French press at home. No jargon, just " +
            "the habits that make the first cup of the day worth the extra five minutes.</p>" +
            "<ul>" +
            "<li>64-page PDF, readable on any device</li>" +
            "<li>Bean-to-cup basics: storage, grind, water temperature</li>" +
            "<li>Recipes for pour over, French press, and stovetop moka</li>" +
            "<li>A printable sample chapter to try before you buy</li>" +
            "</ul>";
        productCoffeeGuide.MetaDescription =
            "Download The Slow Morning Coffee Guide: a 64-page PDF on brewing a better cup at home.";
        productCoffeeGuide.IsDownload = true;
        productCoffeeGuide.DownloadId = downloadCoffeeGuideMain.Id;
        productCoffeeGuide.DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid;
        productCoffeeGuide.UnlimitedDownloads = true;
        productCoffeeGuide.HasSampleDownload = true;
        productCoffeeGuide.SampleDownloadId = downloadCoffeeGuideSample.Id;
        await AddProductPictures(productCoffeeGuide, "product_the_slow_morning_coffee_guide_1.jpg");
        await _productRepository.InsertAsync(productCoffeeGuide);
        products.Add(productCoffeeGuide);

        // 55: Weekend Trails Field Guide — download + sample
        var downloadTrailsGuideMain = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_GTA_1.zip",
            ".zip", "application/x-zip-co", "Weekend Trails Field Guide");
        var downloadTrailsGuideSample = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_GTA_2.txt",
            ".txt", "text/plain", "Weekend Trails Field Guide (sample)");

        var productTrailsGuide = NewDigitalProduct("SD-002", "Weekend Trails Field Guide", 12.99,
            "E-books & Guides", 5000);
        productTrailsGuide.BrandId = BrandId("Trailforge");
        productTrailsGuide.ShortDescription =
            "Twenty two-day hiking routes, packing lists, and campsite notes in one downloadable guide.";
        productTrailsGuide.FullDescription =
            "<p>Twenty routes we have hiked and camped ourselves, each built for a Saturday-to-Sunday " +
            "weekend: trailheads, water sources, campsite notes, and a packing list sized for two days " +
            "out. Download it once and keep it on your phone for the whole season.</p>" +
            "<ul>" +
            "<li>20 two-day routes with maps and elevation notes</li>" +
            "<li>Campsite and water-source notes for each route</li>" +
            "<li>A weekend packing checklist</li>" +
            "<li>A free sample route to try before you buy</li>" +
            "</ul>";
        productTrailsGuide.MetaDescription =
            "Weekend Trails Field Guide: 20 two-day hiking routes with maps, campsites, and packing lists.";
        productTrailsGuide.IsDownload = true;
        productTrailsGuide.DownloadId = downloadTrailsGuideMain.Id;
        productTrailsGuide.DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid;
        productTrailsGuide.UnlimitedDownloads = true;
        productTrailsGuide.HasSampleDownload = true;
        productTrailsGuide.SampleDownloadId = downloadTrailsGuideSample.Id;
        await AddProductPictures(productTrailsGuide, "product_weekend_trails_field_guide_1.jpg");
        await _productRepository.InsertAsync(productTrailsGuide);
        products.Add(productTrailsGuide);

        // 56: Small Space Lighting Handbook — download, no sample
        var downloadLightingHandbook = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_cod_1.zip",
            ".zip", "application/x-zip-co", "Small Space Lighting Handbook");

        var productLightingHandbook = NewDigitalProduct("SD-003", "Small Space Lighting Handbook", 9.99,
            "E-books & Guides", 5000);
        productLightingHandbook.BrandId = BrandId("Luma Lighting");
        productLightingHandbook.ShortDescription =
            "A short, practical PDF on layering light in small apartments and studios.";
        productLightingHandbook.FullDescription =
            "<p>Good lighting is not about more lamps, it is about the right lamps in the right places. " +
            "This handbook shows you how to layer ambient, task, and accent light in a small apartment " +
            "without cluttering the floor plan or the outlet.</p>" +
            "<ul>" +
            "<li>Downloadable PDF, instant access after checkout</li>" +
            "<li>Layering light in studios and one-bedroom flats</li>" +
            "<li>Where to place floor, table, and pendant lamps</li>" +
            "<li>Bulb colour temperature explained in plain language</li>" +
            "</ul>";
        productLightingHandbook.MetaDescription =
            "Small Space Lighting Handbook: a practical PDF guide to layering light in small apartments.";
        productLightingHandbook.IsDownload = true;
        productLightingHandbook.DownloadId = downloadLightingHandbook.Id;
        productLightingHandbook.DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid;
        productLightingHandbook.UnlimitedDownloads = true;
        await AddProductPictures(productLightingHandbook, "product_small_space_lighting_handbook_1.jpg");
        await _productRepository.InsertAsync(productLightingHandbook);
        products.Add(productLightingHandbook);

        // 57: Capsule Wardrobe Planner — download, no sample
        var downloadWardrobePlanner = await InsertDownload(
            "https://raw.githubusercontent.com/grandnode/sample/refs/heads/main/product_cyberpunk_1.zip",
            ".zip", "application/x-zip-co", "Capsule Wardrobe Planner");

        var productWardrobePlanner = NewDigitalProduct("SD-004", "Capsule Wardrobe Planner", 7.99,
            "E-books & Guides", 5000);
        productWardrobePlanner.BrandId = BrandId("Loom & Thread");
        productWardrobePlanner.ShortDescription =
            "A downloadable planner and worksheet for building a capsule wardrobe you actually wear.";
        productWardrobePlanner.FullDescription =
            "<p>A short, worksheet-driven planner for paring your wardrobe down to pieces that mix and " +
            "match across every season. Print it or fill it in on screen: either way, you end the process " +
            "with a list of what to keep, what to let go, and what is actually worth buying next.</p>" +
            "<ul>" +
            "<li>Fillable PDF worksheet, print or complete on screen</li>" +
            "<li>A capsule-building framework by season</li>" +
            "<li>Keep, donate, and shopping-list templates</li>" +
            "</ul>";
        productWardrobePlanner.MetaDescription =
            "Capsule Wardrobe Planner: a downloadable worksheet for building a wardrobe you actually wear.";
        productWardrobePlanner.IsDownload = true;
        productWardrobePlanner.DownloadId = downloadWardrobePlanner.Id;
        productWardrobePlanner.DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid;
        productWardrobePlanner.UnlimitedDownloads = true;
        await AddProductPictures(productWardrobePlanner, "product_capsule_wardrobe_planner_1.jpg");
        await _productRepository.InsertAsync(productWardrobePlanner);
        products.Add(productWardrobePlanner);

        #endregion

        #region Workshops

        // 58: Home Barista Online Workshop — virtual, live 90-minute session
        var productBaristaWorkshop = NewDigitalProduct("SD-005", "Home Barista Online Workshop", 45.00,
            "Workshops", 200);
        productBaristaWorkshop.BrandId = BrandId("Ember & Oak");
        productBaristaWorkshop.ShortDescription =
            "A live, 90-minute online workshop on pulling better espresso and milk texture at home.";
        productBaristaWorkshop.FullDescription =
            "<p>Join our roasters for a live 90-minute video workshop on dialling in espresso and " +
            "texturing milk at home, whatever machine you own. Bring your own setup, ask questions in " +
            "real time, and leave with a routine you can repeat every morning.</p>" +
            "<ul>" +
            "<li>Live, 90-minute video session, small group</li>" +
            "<li>Espresso dial-in and milk texturing basics</li>" +
            "<li>Works with any home espresso machine</li>" +
            "<li>Booking confirmation and joining link by e-mail</li>" +
            "</ul>";
        productBaristaWorkshop.MetaDescription =
            "Home Barista Online Workshop: a live 90-minute session on espresso and milk texture at home.";
        await AddProductPictures(productBaristaWorkshop, "product_home_barista_online_workshop_1.jpg");
        await _productRepository.InsertAsync(productBaristaWorkshop);
        products.Add(productBaristaWorkshop);

        // 59: Studio Membership Monthly — recurring, virtual
        var productMembership = NewDigitalProduct("SD-006", "Studio Membership Monthly", 12.99,
            "Workshops", 5000);
        productMembership.MarkAsNew = true;
        productMembership.ShortDescription =
            "One live workshop a month plus our full recorded library, billed monthly.";
        productMembership.FullDescription =
            "<p>A standing seat at one live workshop every month, plus unlimited access to the recorded " +
            "library of everything we have run before. Cancel any time; access continues for the rest of " +
            "the billing cycle you have already paid for.</p>" +
            "<ul>" +
            "<li>One live workshop a month, any topic in the schedule</li>" +
            "<li>Unlimited access to the recorded workshop library</li>" +
            "<li>Billed monthly, cancel any time</li>" +
            "</ul>";
        productMembership.MetaDescription =
            "Studio Membership Monthly: one live workshop a month plus our full recorded library.";
        productMembership.IsRecurring = true;
        productMembership.RecurringCycleLength = 1;
        productMembership.RecurringCyclePeriodId = RecurringCyclePeriod.Months;
        productMembership.RecurringTotalCycles = 12;
        await AddProductPictures(productMembership, "product_studio_membership_monthly_1.jpg");
        await _productRepository.InsertAsync(productMembership);
        products.Add(productMembership);

        // 60: Bike Maintenance Basics Workshop — in-person, three hours
        var productBikeWorkshop = NewDigitalProduct("SD-007", "Bike Maintenance Basics Workshop", 59.00,
            "Workshops", 60);
        productBikeWorkshop.BrandId = BrandId("Kinetic");
        productBikeWorkshop.ShortDescription =
            "A three-hour, in-person workshop covering the repairs every rider should know.";
        productBikeWorkshop.FullDescription =
            "<p>A hands-on, three-hour workshop covering the repairs every rider should be able to do " +
            "themselves: fixing a flat, adjusting brakes and gears, and keeping a chain running clean. " +
            "Tools and stands are provided; bring your own bike if you have one.</p>" +
            "<ul>" +
            "<li>In-person, three-hour session</li>" +
            "<li>Flats, brakes, gears, and chain maintenance</li>" +
            "<li>Tools and repair stands provided</li>" +
            "<li>Small groups, booking confirmation by e-mail</li>" +
            "</ul>";
        productBikeWorkshop.MetaDescription =
            "Bike Maintenance Basics Workshop: a three-hour, in-person class on flats, brakes, and gears.";
        await AddProductPictures(productBikeWorkshop, "product_bike_maintenance_basics_workshop_1.jpg");
        await _productRepository.InsertAsync(productBikeWorkshop);
        products.Add(productBikeWorkshop);

        // 61: Natural Dye Textile Workshop — in-person, half day
        var productDyeWorkshop = NewDigitalProduct("SD-008", "Natural Dye Textile Workshop", 65.00,
            "Workshops", 40);
        productDyeWorkshop.BrandId = BrandId("Loom & Thread");
        productDyeWorkshop.ShortDescription =
            "A half-day, in-person workshop on dyeing natural fabric with plant-based colour.";
        productDyeWorkshop.FullDescription =
            "<p>A half-day, hands-on session on dyeing natural fibres with plant-based colour: onion skin, " +
            "madder root, and indigo among them. You will leave with your own dyed textile piece and the " +
            "notes to repeat the process at home.</p>" +
            "<ul>" +
            "<li>In-person, half-day session</li>" +
            "<li>Plant-based dye techniques on natural fibres</li>" +
            "<li>Take home a piece you dye yourself</li>" +
            "<li>Materials and aprons provided</li>" +
            "</ul>";
        productDyeWorkshop.MetaDescription =
            "Natural Dye Textile Workshop: a half-day, in-person class on dyeing fabric with natural colour.";
        await AddProductPictures(productDyeWorkshop, "product_natural_dye_textile_workshop_1.jpg");
        await _productRepository.InsertAsync(productDyeWorkshop);
        products.Add(productDyeWorkshop);

        #endregion

        #region Gift vouchers

        // 62-64: virtual gift vouchers, tax exempt
        var productVoucher25 = NewDigitalProduct("SD-009", "Gift Voucher 25", 25.00, "Gift vouchers", 5000);
        productVoucher25.ShortDescription = "A $25 gift voucher, delivered by e-mail and redeemable on any order.";
        productVoucher25.FullDescription =
            "<p>Let them choose. This $25 voucher is delivered by e-mail right after checkout and can be " +
            "redeemed against any order, any time.</p>" +
            "<ul>" +
            "<li>$25 value, delivered by e-mail</li>" +
            "<li>Redeemable against any order</li>" +
            "<li>No expiry</li>" +
            "</ul>";
        productVoucher25.MetaDescription = "Gift Voucher 25: a $25 e-mail gift voucher, redeemable on any order.";
        productVoucher25.IsGiftVoucher = true;
        productVoucher25.GiftVoucherTypeId = GiftVoucherType.Virtual;
        productVoucher25.IsTaxExempt = true;
        await AddProductPictures(productVoucher25, "product_gift_voucher_25_1.jpg");
        await _productRepository.InsertAsync(productVoucher25);
        products.Add(productVoucher25);

        var productVoucher50 = NewDigitalProduct("SD-010", "Gift Voucher 50", 50.00, "Gift vouchers", 5000);
        productVoucher50.ShortDescription = "A $50 gift voucher, delivered by e-mail and redeemable on any order.";
        productVoucher50.FullDescription =
            "<p>Let them choose. This $50 voucher is delivered by e-mail right after checkout and can be " +
            "redeemed against any order, any time.</p>" +
            "<ul>" +
            "<li>$50 value, delivered by e-mail</li>" +
            "<li>Redeemable against any order</li>" +
            "<li>No expiry</li>" +
            "</ul>";
        productVoucher50.MetaDescription = "Gift Voucher 50: a $50 e-mail gift voucher, redeemable on any order.";
        productVoucher50.IsGiftVoucher = true;
        productVoucher50.GiftVoucherTypeId = GiftVoucherType.Virtual;
        productVoucher50.IsTaxExempt = true;
        await AddProductPictures(productVoucher50, "product_gift_voucher_50_1.jpg");
        await _productRepository.InsertAsync(productVoucher50);
        products.Add(productVoucher50);

        var productVoucher100 = NewDigitalProduct("SD-011", "Gift Voucher 100", 100.00, "Gift vouchers", 5000);
        productVoucher100.ShortDescription = "A $100 gift voucher, delivered by e-mail and redeemable on any order.";
        productVoucher100.FullDescription =
            "<p>Let them choose. This $100 voucher is delivered by e-mail right after checkout and can be " +
            "redeemed against any order, any time.</p>" +
            "<ul>" +
            "<li>$100 value, delivered by e-mail</li>" +
            "<li>Redeemable against any order</li>" +
            "<li>No expiry</li>" +
            "</ul>";
        productVoucher100.MetaDescription = "Gift Voucher 100: a $100 e-mail gift voucher, redeemable on any order.";
        productVoucher100.IsGiftVoucher = true;
        productVoucher100.GiftVoucherTypeId = GiftVoucherType.Virtual;
        productVoucher100.IsTaxExempt = true;
        await AddProductPictures(productVoucher100, "product_gift_voucher_100_1.jpg");
        await _productRepository.InsertAsync(productVoucher100);
        products.Add(productVoucher100);

        // 65: Printed Gift Voucher Card — physical, shipped, tax exempt
        var productVoucherCard = NewDigitalProduct("SD-012", "Printed Gift Voucher Card", 75.00,
            "Gift vouchers", 300);
        productVoucherCard.ShortDescription =
            "A $75 gift voucher printed on a card and posted in an envelope, ready to give.";
        productVoucherCard.FullDescription =
            "<p>For when an e-mail does not feel like enough: this $75 voucher is printed on a card and " +
            "posted to the recipient in an envelope, ready to hand over or place under a tree.</p>" +
            "<ul>" +
            "<li>$75 value, printed card, posted by mail</li>" +
            "<li>Redeemable against any order</li>" +
            "<li>No expiry</li>" +
            "</ul>";
        productVoucherCard.MetaDescription =
            "Printed Gift Voucher Card: a $75 voucher printed on a card and posted to the recipient.";
        productVoucherCard.IsGiftVoucher = true;
        productVoucherCard.GiftVoucherTypeId = GiftVoucherType.Physical;
        productVoucherCard.IsTaxExempt = true;
        productVoucherCard.IsShipEnabled = true;
        productVoucherCard.Weight = 0.05;
        productVoucherCard.Length = 15;
        productVoucherCard.Width = 10.5;
        productVoucherCard.Height = 0.3;
        await AddProductPictures(productVoucherCard, "product_printed_gift_voucher_card_1.jpg");
        await _productRepository.InsertAsync(productVoucherCard);
        products.Add(productVoucherCard);

        #endregion

        return products;
    }
}
