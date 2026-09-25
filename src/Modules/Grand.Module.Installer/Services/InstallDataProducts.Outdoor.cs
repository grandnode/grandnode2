using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Sports & Outdoor department products: inserts products (with pictures, categories, brand, vendor,
    // specs, attributes) and returns them. Does not create slugs, tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsOutdoor(SampleProductContext ctx)
    {
        var taxCategoryId = TaxCategoryId("Sports & Outdoor");
        var products = new List<Product>();

        #region Camping

        var trailforgeTwoPersonTent = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Trailforge Two Person Tent",
            ShortDescription = "A lightweight three-season tent built for two, pitched in minutes at the first sign of rain.",
            FullDescription = """
                <p>Camp light without camping cold. This freestanding two-person tent pitches in under
                five minutes, holds its own in wind and rain, and still packs down small enough to
                forget it is on your back.</p>
                <ul>
                    <li>Freestanding two-pole design pitches in under five minutes</li>
                    <li>Recycled nylon flysheet keeps you dry through three seasons</li>
                    <li>Packs down to just 1.6 kg for the trail</li>
                    <li>Two doors and vestibules for easy access on either side</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Capacity</th><td>2 persons</td></tr>
                        <tr><th>Trail weight</th><td>1.6 kg</td></tr>
                        <tr><th>Season rating</th><td>Three season</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Trailforge Two Person Tent",
            MetaDescription = "Lightweight three-season tent for two, 1.6 kg trail weight, pitches in minutes. Forest Green or Sand.",
            Sku = "OA-001",
            Price = 349.00,
            MarkAsNew = true,
            ShowOnHomePage = true,
            IsFreeShipping = true,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 40,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.6,
            Length = 55,
            Width = 18,
            Height = 18,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Camping"), DisplayOrder = 1 } },
            BrandId = BrandId("Trailforge"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Green", 1),
                Spec("Material", "Recycled nylon", 2),
                Spec("Capacity", "2 persons", 3),
                Spec("Season", "Three season", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Forest Green", "#228B22", 0),
                    ("Sand", "#C2B280", 0))
            }
        };
        await AddProductPictures(trailforgeTwoPersonTent,
            "product_trailforge_two_person_tent_1.jpg",
            "product_trailforge_two_person_tent_2.jpg",
            "product_trailforge_two_person_tent_3.jpg");
        await _productRepository.InsertAsync(trailforgeTwoPersonTent);
        products.Add(trailforgeTwoPersonTent);

        var trailforgeDownSleepingBag = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Trailforge Down Sleeping Bag",
            ShortDescription = "A down-filled sleeping bag rated to -2C, packed small enough to disappear in your bag.",
            FullDescription = """
                <p>Down that earns its weight. This bag holds warmth to a -2C comfort rating without
                weighing down your pack, and compresses into its own stuff sack for the trail in.</p>
                <ul>
                    <li>Down fill keeps you warm to a -2C comfort rating</li>
                    <li>Ripstop shell resists snags around camp</li>
                    <li>Compresses into its own stuff sack</li>
                    <li>Regular or Long length for a proper fit</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Comfort rating</th><td>-2C</td></tr>
                        <tr><th>Fill</th><td>Down</td></tr>
                        <tr><th>Season rating</th><td>Three season</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Trailforge Down Sleeping Bag",
            MetaDescription = "Down sleeping bag rated to -2C comfort, Regular or Long length, three-season camping.",
            Sku = "OA-002",
            Price = 259.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 35,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.2,
            Length = 40,
            Width = 20,
            Height = 20,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Camping"), DisplayOrder = 2 } },
            BrandId = BrandId("Trailforge"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Blue", 1),
                Spec("Material", "Down", 2),
                Spec("Season", "Three season", 3)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("Regular", 0),
                    ("Long", 20.00))
            }
        };
        await AddProductPictures(trailforgeDownSleepingBag, "product_trailforge_down_sleeping_bag_1.jpg");
        await _productRepository.InsertAsync(trailforgeDownSleepingBag);
        products.Add(trailforgeDownSleepingBag);

        var trailforgeCampingTentRental = new Product {
            ProductTypeId = ProductType.Reservation,
            VisibleIndividually = true,
            Name = "Trailforge Camping Tent Rental",
            ShortDescription = "Rent a two-person tent by the day, picked up ready to pitch in Innsbruck.",
            FullDescription = """
                <p>Travelling light? Rent the gear instead of hauling it. This two-person tent is
                cleaned and checked between every rental, so it is ready to pitch the moment you pick
                it up.</p>
                <ul>
                    <li>Complete two-person tent, cleaned and checked between every rental</li>
                    <li>Add a sleeping pad, camp stove, or lantern when you book</li>
                    <li>Picked up locally in Innsbruck, no shipping required</li>
                    <li>Priced per day, book as many days as your trip needs</li>
                </ul>
                """,
            MetaTitle = "Trailforge Camping Tent Rental",
            MetaDescription = "Rent a two-person tent by the day in Innsbruck, with sleeping pad, stove, and lantern add-ons.",
            Sku = "OA-003",
            Price = 29.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 5,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            IsShipEnabled = false,
            IntervalUnitId = IntervalUnit.Day,
            Interval = 1,
            IncBothDate = false,
            Weight = 1.6,
            Length = 55,
            Width = 18,
            Height = 18,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Camping"), DisplayOrder = 3 } },
            BrandId = BrandId("Trailforge"),
            VendorId = VendorId("Alpine Rental Co."),
            ProductSpecificationAttributes = {
                Spec("Capacity", "2 persons", 1),
                Spec("Season", "Three season", 2)
            },
            ProductAttributeMappings = {
                RentalAddOnsAttribute(1,
                    ("Sleeping pad", 5.00),
                    ("Camp stove", 8.00),
                    ("Lantern", 4.00))
            }
        };
        await AddProductPictures(trailforgeCampingTentRental, "product_trailforge_camping_tent_rental_1.jpg");
        await _productRepository.InsertAsync(trailforgeCampingTentRental);
        await InsertDailyReservationSlots(trailforgeCampingTentRental.Id);
        products.Add(trailforgeCampingTentRental);

        var trailforgeInsulatedCampMug = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Trailforge Insulated Camp Mug",
            ShortDescription = "A double-wall insulated mug that keeps coffee hot at the campsite for hours.",
            FullDescription = """
                <p>The fire dies down, but your coffee does not have to go cold with it. This
                double-wall mug holds heat for hours and travels well strapped to a pack.</p>
                <ul>
                    <li>Double-wall stainless steel holds heat long after the fire dies down</li>
                    <li>350 ml capacity fits a full pour-over</li>
                    <li>Spill-resistant lid for the trail</li>
                    <li>Available in Olive, Sand, or Black</li>
                </ul>
                """,
            MetaTitle = "Trailforge Insulated Camp Mug",
            MetaDescription = "Double-wall insulated camping mug, 350 ml, Olive, Sand, or Black.",
            Sku = "OA-004",
            Price = 24.99,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 120,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.35,
            Length = 12,
            Width = 9,
            Height = 9,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Camping"), DisplayOrder = 4 } },
            BrandId = BrandId("Trailforge"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Green", 1),
                Spec("Material", "Stainless steel", 2),
                Spec("Capacity", "Up to 0.5 L", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Olive", "#556B2F", 0),
                    ("Sand", "#C2B280", 0),
                    ("Black", "#111111", 0))
            }
        };
        await AddProductPictures(trailforgeInsulatedCampMug, "product_trailforge_insulated_camp_mug_1.jpg");
        await _productRepository.InsertAsync(trailforgeInsulatedCampMug);
        products.Add(trailforgeInsulatedCampMug);

        #endregion

        #region Cycling

        var kineticCommuterHelmet = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Commuter Helmet",
            ShortDescription = "A commuter helmet with an integrated rear light, built to be seen after dark.",
            FullDescription = """
                <p>Built for the ride to work as much as the ride home after dark. An integrated
                rechargeable rear light and a secure fit dial make this helmet as practical as it is
                protective.</p>
                <ul>
                    <li>Integrated rechargeable rear light for low-light commutes</li>
                    <li>In-mould shell keeps weight low without cutting protection</li>
                    <li>Adjustable fit dial for a secure, no-slip fit</li>
                    <li>Two sizes, two colours</li>
                </ul>
                """,
            MetaTitle = "Kinetic Commuter Helmet",
            MetaDescription = "Commuter bike helmet with integrated rear light, Matte Black or Signal Orange.",
            Sku = "OA-005",
            Price = 89.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 60,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.35,
            Length = 25,
            Width = 20,
            Height = 15,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Cycling"), DisplayOrder = 1 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Season", "All season", 2)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("S/M", 0),
                    ("M/L", 0)),
                ColorAttribute(2,
                    ("Matte Black", "#28282B", 0),
                    ("Signal Orange", "#FF6103", 0))
            }
        };
        await AddProductPictures(kineticCommuterHelmet, "product_kinetic_commuter_helmet_1.jpg");
        await _productRepository.InsertAsync(kineticCommuterHelmet);
        products.Add(kineticCommuterHelmet);

        var touringEBikeRental = new Product {
            ProductTypeId = ProductType.Reservation,
            VisibleIndividually = true,
            Name = "Touring E-Bike Rental",
            ShortDescription = "Rent an electric touring bike by the day and cover more ground on the ride.",
            FullDescription = """
                <p>See more of the road without running out of legs. This pedal-assist touring e-bike
                is ready to go for a day, a weekend, or the whole trip.</p>
                <ul>
                    <li>Pedal-assist motor for longer days in the saddle</li>
                    <li>Add pannier bags, a child seat, or a helmet when you book</li>
                    <li>Picked up locally, no shipping required</li>
                    <li>Priced per day</li>
                </ul>
                """,
            MetaTitle = "Touring E-Bike Rental",
            MetaDescription = "Rent an electric touring bike by the day, with pannier bags, child seat, and helmet add-ons.",
            Sku = "OA-006",
            Price = 49.00,
            MarkAsNew = true,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 6,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            IsShipEnabled = false,
            IntervalUnitId = IntervalUnit.Day,
            Interval = 1,
            IncBothDate = false,
            Weight = 22,
            Length = 175,
            Width = 60,
            Height = 100,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Cycling"), DisplayOrder = 2 } },
            VendorId = VendorId("Alpine Rental Co."),
            ProductSpecificationAttributes = {
                Spec("Season", "Spring", 1),
                Spec("Season", "Summer", 2)
            },
            ProductAttributeMappings = {
                RentalAddOnsAttribute(1,
                    ("Pannier bags", 6.00),
                    ("Child seat", 10.00),
                    ("Helmet", 3.00))
            }
        };
        await AddProductPictures(touringEBikeRental,
            "product_touring_e_bike_rental_1.jpg",
            "product_touring_e_bike_rental_2.jpg");
        await _productRepository.InsertAsync(touringEBikeRental);
        await InsertDailyReservationSlots(touringEBikeRental.Id);
        products.Add(touringEBikeRental);

        var vintageSteelRoadBike1984 = new Product {
            ProductTypeId = ProductType.Auction,
            VisibleIndividually = true,
            Name = "Vintage Steel Road Bike 1984",
            ShortDescription = "A restored 1984 steel road bike with a lugged frame, up for auction to one new owner.",
            FullDescription = """
                <p>Built in 1984 and restored by hand, this lugged steel frame rides the way road bikes
                used to: light on its feet and full of character. There is only one, and it goes to the
                highest bidder.</p>
                <ul>
                    <li>56 cm lugged steel frame, fully restored</li>
                    <li>Period-correct components serviced and ready to ride</li>
                    <li>One of a kind, once the auction ends it is gone</li>
                    <li>Bid before the auction closes to make it yours</li>
                </ul>
                """,
            MetaTitle = "Vintage Steel Road Bike 1984",
            MetaDescription = "Restored 1984 lugged steel road bike, 56 cm frame, up for auction.",
            Sku = "OA-007",
            StartPrice = 450.00,
            Price = 0,
            AvailableStartDateTimeUtc = DateTime.UtcNow,
            AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(14),
            HighestBid = 0,
            HighestBidder = "",
            AuctionEnded = false,
            ShowOnHomePage = true,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 1,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 1,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 11,
            Length = 175,
            Width = 55,
            Height = 100,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Cycling"), DisplayOrder = 3 } },
            VendorId = VendorId("Vintage Velo"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Blue", 1),
                Spec("Material", "Steel", 2)
            }
        };
        await AddProductPictures(vintageSteelRoadBike1984,
            "product_vintage_steel_road_bike_1984_1.jpg",
            "product_vintage_steel_road_bike_1984_2.jpg");
        await _productRepository.InsertAsync(vintageSteelRoadBike1984);
        products.Add(vintageSteelRoadBike1984);

        var refurbishedCityBike = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Refurbished City Bike",
            ShortDescription = "A pre-owned city bike, fully refurbished and ready to ride around town.",
            FullDescription = """
                <p>A good city bike does not need to be new, it needs to work. Every bike here is
                inspected, serviced, and refurbished before it is listed, so you get a reliable ride
                for a fraction of the cost of new.</p>
                <ul>
                    <li>Inspected, serviced, and refurbished before it is listed</li>
                    <li>Classic step-through frame with a front basket</li>
                    <li>Available in three frame sizes</li>
                    <li>Priced well below the cost of new</li>
                </ul>
                """,
            MetaTitle = "Refurbished City Bike",
            MetaDescription = "Pre-owned, refurbished city bike with front basket, three frame sizes.",
            Sku = "OA-008",
            Price = 399.00,
            OldPrice = 649.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 3,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 13,
            Length = 175,
            Width = 55,
            Height = 100,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Cycling"), DisplayOrder = 4 } },
            VendorId = VendorId("Vintage Velo"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Green", 1),
                Spec("Material", "Steel", 2)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("52 cm", 0),
                    ("56 cm", 0),
                    ("60 cm", 0))
            }
        };
        await AddProductPictures(refurbishedCityBike, "product_refurbished_city_bike_1.jpg");
        await _productRepository.InsertAsync(refurbishedCityBike);
        products.Add(refurbishedCityBike);

        #endregion

        #region Running

        var kineticTrailRunningShoes = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Trail Running Shoes",
            ShortDescription = "Grippy trail running shoes with a 6 mm drop, built for muddy paths and long climbs.",
            FullDescription = """
                <p>Built for the trail, not the treadmill. A grippy outsole and a low 6 mm drop keep
                you stable on climbs, descents, and everything muddy in between.</p>
                <ul>
                    <li>Grippy multi-directional outsole for wet and muddy trails</li>
                    <li>6 mm drop for a natural, stable stride</li>
                    <li>Recycled polyester upper, breathable and quick-drying</li>
                    <li>Six sizes, two colourways</li>
                </ul>
                """,
            MetaTitle = "Kinetic Trail Running Shoes",
            MetaDescription = "Trail running shoes, 6 mm drop, grippy outsole, Slate or Volt.",
            Sku = "OA-009",
            Price = 139.00,
            MarkAsNew = true,
            ShowOnHomePage = true,
            BestSeller = true,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 90,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.6,
            Length = 32,
            Width = 20,
            Height = 12,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Running"), DisplayOrder = 1 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Grey", 1),
                Spec("Material", "Recycled polyester", 2),
                Spec("Season", "All season", 3)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("US 7", 0), ("US 8", 0), ("US 9", 0), ("US 10", 0), ("US 11", 0), ("US 12", 0)),
                ColorAttribute(2,
                    ("Slate", "#708090", 0),
                    ("Volt", "#CCFF00", 0))
            }
        };
        await AddProductPictures(kineticTrailRunningShoes,
            "product_kinetic_trail_running_shoes_1.jpg",
            "product_kinetic_trail_running_shoes_2.jpg");
        await _productRepository.InsertAsync(kineticTrailRunningShoes);
        products.Add(kineticTrailRunningShoes);

        var kineticLightweightRunningJacket = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Lightweight Running Jacket",
            ShortDescription = "A windproof running jacket that packs into its own pocket for unpredictable weather.",
            FullDescription = """
                <p>The jacket you forget you are carrying until you need it. Windproof and
                water-resistant, it packs into its own chest pocket so there is no excuse to leave it
                behind.</p>
                <ul>
                    <li>Windproof, water-resistant shell for exposed trails</li>
                    <li>Packs into its own chest pocket</li>
                    <li>Recycled polyester, breathable for hard efforts</li>
                    <li>Five sizes, slim fit</li>
                </ul>
                """,
            MetaTitle = "Kinetic Lightweight Running Jacket",
            MetaDescription = "Windproof, packable running jacket, slim fit, recycled polyester.",
            Sku = "OA-010",
            Price = 119.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 70,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.25,
            Length = 30,
            Width = 25,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Running"), DisplayOrder = 2 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Recycled polyester", 2),
                Spec("Fit", "Slim", 3),
                Spec("Season", "Spring", 4),
                Spec("Season", "Autumn", 5)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("XS", 0), ("S", 0), ("M", 0), ("L", 0), ("XL", 0))
            }
        };
        await AddProductPictures(kineticLightweightRunningJacket, "product_kinetic_lightweight_running_jacket_1.jpg");
        await _productRepository.InsertAsync(kineticLightweightRunningJacket);
        products.Add(kineticLightweightRunningJacket);

        var kineticHydrationVest = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Hydration Vest",
            ShortDescription = "A trail running vest with two soft flasks included, built to carry water without the bounce.",
            FullDescription = """
                <p>Water without the bounce. This vest sits close to the body on long trail efforts,
                and comes with two soft flasks ready to go straight out of the box.</p>
                <ul>
                    <li>Two soft flasks included, ready to run</li>
                    <li>Stable, low-bounce fit for long trail efforts</li>
                    <li>5 L or 10 L capacity for shorter or longer runs</li>
                    <li>Two sizes to fit most builds</li>
                </ul>
                """,
            MetaTitle = "Kinetic Hydration Vest",
            MetaDescription = "Trail running hydration vest with two soft flasks, 5 L or 10 L capacity.",
            Sku = "OA-011",
            Price = 89.00,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 55,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.4,
            Length = 30,
            Width = 25,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Running"), DisplayOrder = 3 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Recycled nylon", 2),
                Spec("Capacity", "1.5 to 10 L", 3)
            },
            ProductAttributeMappings = {
                CapacityAttribute(1,
                    ("5 L", 0),
                    ("10 L", 15.00)),
                SizeAttribute(2,
                    ("S/M", 0),
                    ("L/XL", 0))
            }
        };
        await AddProductPictures(kineticHydrationVest, "product_kinetic_hydration_vest_1.jpg");
        await _productRepository.InsertAsync(kineticHydrationVest);
        products.Add(kineticHydrationVest);

        var kineticRunningSocksThreePack = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Running Socks Three Pack",
            ShortDescription = "A three-pack of merino running socks that stay soft mile after mile.",
            FullDescription = """
                <p>The small upgrade that makes every run more comfortable. Merino wool regulates
                temperature and resists odour, so the pack stays in rotation long after cheaper socks
                give out.</p>
                <ul>
                    <li>Merino wool blend regulates temperature and resists odour</li>
                    <li>Three pairs in one pack, ready to rotate</li>
                    <li>Flat-knit toe seam helps prevent blisters</li>
                    <li>Three sizes to fit the whole household</li>
                </ul>
                """,
            MetaTitle = "Kinetic Running Socks Three Pack",
            MetaDescription = "Three-pack of merino wool running socks, S, M, or L.",
            Sku = "OA-012",
            Price = 19.99,
            TaxCategoryId = taxCategoryId,
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 200,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.1,
            Length = 20,
            Width = 15,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Running"), DisplayOrder = 4 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Multicolor", 1),
                Spec("Material", "Merino wool", 2)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("S", 0), ("M", 0), ("L", 0))
            }
        };
        await AddProductPictures(kineticRunningSocksThreePack, "product_kinetic_running_socks_three_pack_1.jpg");
        await _productRepository.InsertAsync(kineticRunningSocksThreePack);
        products.Add(kineticRunningSocksThreePack);

        #endregion

        return products;
    }

    // Optional extras on a reservation product, e.g. rental add-ons booked alongside the reservation.
    private ProductAttributeMapping RentalAddOnsAttribute(int displayOrder, params (string Name, double Adjustment)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId("Rental add-ons"),
            AttributeControlTypeId = AttributeControlType.Checkboxes,
            IsRequired = false,
            DisplayOrder = displayOrder
        };
        for (var i = 0; i < values.Length; i++)
            mapping.ProductAttributeValues.Add(new ProductAttributeValue {
                Name = values[i].Name,
                PriceAdjustment = values[i].Adjustment,
                DisplayOrder = i
            });
        return mapping;
    }

    // Inserts one vacant reservation slot per day for the next 90 days, matching how the storefront's
    // day-interval calendar (ActionCartController / GetDatesForMonth) reads free ProductReservation rows:
    // OrderId left empty marks a slot as vacant; Date carries the day.
    private async Task InsertDailyReservationSlots(string productId)
    {
        var today = DateTime.UtcNow.Date;
        for (var i = 1; i <= 90; i++)
            await _productReservationRepository.InsertAsync(new ProductReservation {
                ProductId = productId,
                Date = today.AddDays(i)
            });
    }
}
