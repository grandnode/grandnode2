using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallCollections()
    {
        var collectionLayoutInGridAndLines = _collectionLayoutRepository
            .Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
        if (collectionLayoutInGridAndLines == null)
            throw new Exception("Collection layout cannot be loaded");

        var collectionSummerEscape = new Collection {
            Name = "Summer Escape",
            CollectionLayoutId = collectionLayoutInGridAndLines.Id,
            Description =
                "<p>Everything for long light evenings, lake weekends, and trips that start on a bike. " +
                "We picked the pieces that pack small, layer easily, and keep working after the sun goes down.</p>" +
                "<p>Pitch a tent by the water, light a lantern, and let the evening run late.</p>",
            MetaTitle = "Summer Escape",
            MetaKeywords = "summer, outdoor, camping, evening",
            MetaDescription = "Everything for long light evenings, lake weekends, and trips that start on a bike.",
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ShowOnHomePage = true,
            IncludeInMenu = false,
            Published = true,
            DisplayOrder = 1
        };
        await _collectionRepository.InsertAsync(collectionSummerEscape);
        await InsertSlug(collectionSummerEscape.Id, EntityTypes.Collection, collectionSummerEscape.Name,
            seName => collectionSummerEscape.SeName = seName);
        var summerEscapePicture = await InsertSamplePicture("collection_summer_escape.jpg", collectionSummerEscape.Name,
            Reference.Collection, collectionSummerEscape.Id);
        collectionSummerEscape.PictureId = summerEscapePicture.Id;
        await _collectionRepository.UpdateAsync(collectionSummerEscape);

        var collectionSlowMorning = new Collection {
            Name = "Slow Morning",
            CollectionLayoutId = collectionLayoutInGridAndLines.Id,
            Description =
                "<p>Kettle on, phone off: the pieces that make the first hour of the day worth waking up for. " +
                "A good pour-over, a warm blanket, and light that doesn't hurt to look at.</p>" +
                "<p>Small rituals, kept simple, make the rest of the day easier.</p>",
            MetaTitle = "Slow Morning",
            MetaKeywords = "coffee, morning, home, cozy",
            MetaDescription = "Kettle on, phone off: the pieces that make the first hour of the day worth waking up for.",
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ShowOnHomePage = true,
            IncludeInMenu = false,
            Published = true,
            DisplayOrder = 2
        };
        await _collectionRepository.InsertAsync(collectionSlowMorning);
        await InsertSlug(collectionSlowMorning.Id, EntityTypes.Collection, collectionSlowMorning.Name,
            seName => collectionSlowMorning.SeName = seName);
        var slowMorningPicture = await InsertSamplePicture("collection_slow_morning.jpg", collectionSlowMorning.Name,
            Reference.Collection, collectionSlowMorning.Id);
        collectionSlowMorning.PictureId = slowMorningPicture.Id;
        await _collectionRepository.UpdateAsync(collectionSlowMorning);

        var collectionGiftsUnder50 = new Collection {
            Name = "Gifts Under $50",
            CollectionLayoutId = collectionLayoutInGridAndLines.Id,
            Description =
                "<p>Thoughtful presents that feel like more than they cost. Every price here is below $50, " +
                "from a good mug to a field guide that actually gets read.</p>" +
                "<p>Pick one, wrap it, done - no last-minute scramble required.</p>",
            MetaTitle = "Gifts Under $50",
            MetaKeywords = "gifts, presents, budget",
            MetaDescription = "Thoughtful presents that feel like more than they cost. Every price here is below $50.",
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ShowOnHomePage = false,
            IncludeInMenu = false,
            Published = true,
            DisplayOrder = 3
        };
        await _collectionRepository.InsertAsync(collectionGiftsUnder50);
        await InsertSlug(collectionGiftsUnder50.Id, EntityTypes.Collection, collectionGiftsUnder50.Name,
            seName => collectionGiftsUnder50.SeName = seName);
        var giftsUnder50Picture = await InsertSamplePicture("collection_gifts_under_50.jpg", collectionGiftsUnder50.Name,
            Reference.Collection, collectionGiftsUnder50.Id);
        collectionGiftsUnder50.PictureId = giftsUnder50Picture.Id;
        await _collectionRepository.UpdateAsync(collectionGiftsUnder50);

        var collectionWorkFromAnywhere = new Collection {
            Name = "Work From Anywhere",
            CollectionLayoutId = collectionLayoutInGridAndLines.Id,
            Description =
                "<p>A calm, portable setup for the kitchen table, the train, or the cabin with good Wi-Fi. " +
                "Headphones that block the room out, a bag that carries the rest.</p>" +
                "<p>Everything here is built to move with you and still feel put together when it arrives.</p>",
            MetaTitle = "Work From Anywhere",
            MetaKeywords = "remote work, portable, office",
            MetaDescription = "A calm, portable setup for the kitchen table, the train, or the cabin with good Wi-Fi.",
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ShowOnHomePage = false,
            IncludeInMenu = false,
            Published = true,
            DisplayOrder = 4
        };
        await _collectionRepository.InsertAsync(collectionWorkFromAnywhere);
        await InsertSlug(collectionWorkFromAnywhere.Id, EntityTypes.Collection, collectionWorkFromAnywhere.Name,
            seName => collectionWorkFromAnywhere.SeName = seName);
        var workFromAnywherePicture = await InsertSamplePicture("collection_work_from_anywhere.jpg", collectionWorkFromAnywhere.Name,
            Reference.Collection, collectionWorkFromAnywhere.Id);
        collectionWorkFromAnywhere.PictureId = workFromAnywherePicture.Id;
        await _collectionRepository.UpdateAsync(collectionWorkFromAnywhere);

        await AddProductsToCollection(collectionSummerEscape, [
            "Loom Linen Midi Dress",
            "Trailforge Two Person Tent",
            "Trailforge Camping Tent Rental",
            "Luma Rechargeable Lantern Lamp",
            "Aurora Room Portable Speaker",
            "Touring E-Bike Rental",
            "Weekend Trails Field Guide",
            "Trailforge Insulated Camp Mug"
        ]);

        await AddProductsToCollection(collectionSlowMorning, [
            "Nordvik Gooseneck Kettle",
            "Ember Ceramic Pour Over Dripper",
            "Ember Single Origin Coffee Beans",
            "Slow Morning Coffee Bundle",
            "Stoneware Mug Set",
            "Loom Linen Throw Blanket",
            "Loom Waffle Bath Towel Set",
            "Luma Glow Table Lamp",
            "The Slow Morning Coffee Guide"
        ]);

        await AddProductsToCollection(collectionGiftsUnder50, [
            "Kinetic Sport Watch Strap",
            "Ember Ceramic Pour Over Dripper",
            "Ember Single Origin Coffee Beans",
            "Hand Thrown Ceramic Vase",
            "Wool Felt Cushion Cover",
            "Loom Organic Cotton Tee Women",
            "Fieldstone Merino Beanie",
            "Loom Canvas Tote Bag",
            "Fieldstone Leather Card Wallet",
            "Trailforge Insulated Camp Mug",
            "Kinetic Running Socks Three Pack",
            "The Slow Morning Coffee Guide",
            "Weekend Trails Field Guide",
            "Gift Voucher 25"
        ]);

        await AddProductsToCollection(collectionWorkFromAnywhere, [
            "Aurora Studio Wireless Headphones",
            "Aurora Pulse True Wireless Earbuds",
            "Nordvik Air Quality Monitor",
            "Luma Glow Table Lamp",
            "Trailforge Roll Top Backpack",
            "Ember Leather Weekender Bag",
            "Loom Canvas Tote Bag",
            "Studio Membership Monthly"
        ]);
    }

    private async Task AddProductsToCollection(Collection collection, string[] productNames)
    {
        for (var i = 0; i < productNames.Length; i++)
        {
            var product = ProductByName(productNames[i]);
            product.ProductCollections.Add(new ProductCollection { CollectionId = collection.Id, DisplayOrder = i });
            await _productRepository.UpdateAsync(product);
        }
    }
}
