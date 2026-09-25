using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Tech & Audio department products: inserts products (with pictures, categories, brand, vendor,
    // specs, attributes) and returns them. Does not create slugs, tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsTechAudio(SampleProductContext ctx)
    {
        var products = new List<Product>();

        #region Headphones & Speakers

        var auroraStudioWirelessHeadphones = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Aurora Studio Wireless Headphones",
            ShortDescription = "Over-ear wireless headphones with active noise cancelling and a 40-hour battery.",
            FullDescription = """
                <p>Slip these on and the room falls away. Aurora Studio pairs a deep, even noise
                cancelling circuit with memory-foam ear cushions, so a train carriage or an open-plan
                office turns into your own quiet corner. The 40mm drivers give vocals room to breathe
                and bass that stays controlled, not boomy, and the companion app lets you dial in the
                EQ to taste.</p>
                <ul>
                    <li>Active noise cancelling with a transparency mode for quick conversations</li>
                    <li>40-hour battery life, with a 10-minute charge good for 5 hours</li>
                    <li>Foldable hinge and a hard travel case included</li>
                    <li>Multipoint Bluetooth pairing for switching between laptop and phone</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Driver size</th><td>40 mm</td></tr>
                        <tr><th>Battery life</th><td>Up to 40 hours (ANC on)</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3, USB-C</td></tr>
                        <tr><th>Weight</th><td>265 g</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Aurora Studio Wireless Headphones",
            MetaDescription = "Over-ear wireless headphones with active noise cancelling, a 40-hour battery, and a foldable, travel-ready design.",
            Sku = "TA-001",
            Price = 249.00,
            OldPrice = 299.00,
            ShowOnHomePage = true,
            BestSeller = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 140,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.27,
            Length = 20,
            Width = 18,
            Height = 8,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Headphones & Speakers"), DisplayOrder = 1 } },
            BrandId = BrandId("Aurora Audio"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Connectivity", "Bluetooth 5.3", 2),
                Spec("Connectivity", "USB-C", 3),
                Spec("Battery life", "24 to 48 hours", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Midnight Black", "#1a1a1a", 0),
                    ("Arctic White", "#f5f5f5", 0),
                    ("Sage Green", "#9caf88", 10.00))
            }
        };
        await AddProductPictures(auroraStudioWirelessHeadphones,
            "product_aurora_studio_wireless_headphones_1.jpg",
            "product_aurora_studio_wireless_headphones_2.jpg",
            "product_aurora_studio_wireless_headphones_3.jpg");
        await _productRepository.InsertAsync(auroraStudioWirelessHeadphones);
        products.Add(auroraStudioWirelessHeadphones);

        var auroraPulseTrueWirelessEarbuds = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Aurora Pulse True Wireless Earbuds",
            ShortDescription = "Sweat-resistant true wireless earbuds built to stay put through a workout.",
            FullDescription = """
                <p>Pulse was built for movement first. The angled tip locks into your ear so it stays
                there through hill sprints and burpees alike, and the IPX4 rating shrugs off sweat and
                a sudden shower. Eight hours on a single charge covers most days outright, and the
                pocket-sized case tops you back up to full whenever you need it.</p>
                <ul>
                    <li>Secure, angled fit tested through high-intensity training</li>
                    <li>8 hours per charge, 32 hours total with the case</li>
                    <li>IPX4 sweat and splash resistance</li>
                    <li>Touch controls for calls, skip, and volume</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Battery life</th><td>8 h (32 h with case)</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3</td></tr>
                        <tr><th>Water resistance</th><td>IPX4</td></tr>
                        <tr><th>Weight</th><td>5 g per earbud</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Aurora Pulse True Wireless Earbuds",
            MetaDescription = "Sweat-resistant true wireless earbuds with a secure fit, 32 hours of total battery life, and touch controls.",
            Sku = "TA-002",
            Price = 129.00,
            MarkAsNew = true,
            BestSeller = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 200,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.06,
            Length = 7,
            Width = 5,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Headphones & Speakers"), DisplayOrder = 2 } },
            BrandId = BrandId("Aurora Audio"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Connectivity", "Bluetooth 5.3", 2),
                Spec("Battery life", "Under 12 hours", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Graphite", "#4a4a4a", 0),
                    ("Pearl White", "#f2efe9", 0),
                    ("Sand", "#d8c3a5", 0))
            }
        };
        await AddProductPictures(auroraPulseTrueWirelessEarbuds,
            "product_aurora_pulse_true_wireless_earbuds_1.jpg",
            "product_aurora_pulse_true_wireless_earbuds_2.jpg");
        await _productRepository.InsertAsync(auroraPulseTrueWirelessEarbuds);
        products.Add(auroraPulseTrueWirelessEarbuds);

        var auroraRoomPortableSpeaker = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Aurora Room Portable Speaker",
            ShortDescription = "A splash-proof portable speaker with 20 hours of battery for the garden or the kitchen.",
            FullDescription = """
                <p>Room fills a garden, a kitchen, or a picnic blanket with warm, balanced sound
                without needing to be babied. The fabric shell is splash-proof, the corners are
                reinforced against the odd drop, and the battery runs for a full day of use, so you
                can carry it from breakfast to the last song of the evening without hunting for an
                outlet.</p>
                <ul>
                    <li>Splash-proof fabric shell, safe near the pool or in light rain</li>
                    <li>20-hour battery on a single charge</li>
                    <li>Pair two speakers together for stereo sound</li>
                    <li>Built-in handle strap for carrying room to room</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Battery life</th><td>Up to 20 hours</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3, Wi-Fi</td></tr>
                        <tr><th>Water resistance</th><td>Splash-proof</td></tr>
                        <tr><th>Weight</th><td>0.6 kg</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Aurora Room Portable Speaker",
            MetaDescription = "A splash-proof, 20-hour portable speaker for the garden, kitchen, or picnic, with stereo pairing.",
            Sku = "TA-003",
            Price = 179.00,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 110,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.6,
            Length = 18,
            Width = 9,
            Height = 9,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Headphones & Speakers"), DisplayOrder = 3 } },
            BrandId = BrandId("Aurora Audio"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Grey", 1),
                Spec("Connectivity", "Bluetooth 5.3", 2),
                Spec("Connectivity", "Wi-Fi", 3),
                Spec("Battery life", "12 to 24 hours", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Charcoal", "#36454f", 0),
                    ("Stone Grey", "#8c8c86", 0),
                    ("Terracotta", "#c1502e", 10.00))
            }
        };
        await AddProductPictures(auroraRoomPortableSpeaker,
            "product_aurora_room_portable_speaker_1.jpg",
            "product_aurora_room_portable_speaker_2.jpg");
        await _productRepository.InsertAsync(auroraRoomPortableSpeaker);
        products.Add(auroraRoomPortableSpeaker);

        var auroraVinylBookshelfSpeakers = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Aurora Vinyl Bookshelf Speakers",
            ShortDescription = "A powered bookshelf pair with a phono input, built for a record player.",
            FullDescription = """
                <p>Built for a turntable first, Vinyl still sounds equally at home streaming from a
                laptop. One speaker houses the amplifier and the phono stage, so there is no separate
                receiver to find room for, just a pair of cabinets, a set of speaker cables, and
                whatever record you reach for first. The walnut veneer is real wood, not a printed
                wrap, and it ages the way good furniture does.</p>
                <ul>
                    <li>Built-in phono stage, plug a turntable straight in</li>
                    <li>Real walnut veneer cabinets with a soft-touch matte finish</li>
                    <li>Bluetooth 5.3 for streaming when the deck is packed away</li>
                    <li>Powered pair, no separate amplifier needed</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Inputs</th><td>Phono, RCA, Bluetooth 5.3</td></tr>
                        <tr><th>Cabinet</th><td>Walnut veneer, MDF core</td></tr>
                        <tr><th>Power</th><td>Powered pair, Class D amplifier</td></tr>
                        <tr><th>Weight</th><td>4.8 kg per pair</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Aurora Vinyl Bookshelf Speakers",
            MetaDescription = "Powered walnut bookshelf speakers with a built-in phono stage for a turntable, plus Bluetooth streaming.",
            Sku = "TA-004",
            Price = 349.00,
            IsFreeShipping = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 60,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 4.8,
            Length = 35,
            Width = 22,
            Height = 20,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Headphones & Speakers"), DisplayOrder = 4 } },
            BrandId = BrandId("Aurora Audio"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Brown", 1),
                Spec("Material", "Walnut wood", 2),
                Spec("Connectivity", "Bluetooth 5.3", 3),
                Spec("Connectivity", "Wired", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Walnut", "#5c4033", 0),
                    ("Black Ash", "#2b2b2b", 0))
            }
        };
        await AddProductPictures(auroraVinylBookshelfSpeakers,
            "product_aurora_vinyl_bookshelf_speakers_1.jpg",
            "product_aurora_vinyl_bookshelf_speakers_2.jpg");
        await _productRepository.InsertAsync(auroraVinylBookshelfSpeakers);
        products.Add(auroraVinylBookshelfSpeakers);

        #endregion

        #region Smart Home

        var lumaSmartBulbStarterKit = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Luma Smart Bulb Starter Kit",
            ShortDescription = "Three tunable white bulbs and a bridge, ready to dim and schedule from your phone.",
            FullDescription = """
                <p>Everything you need to start automating your lighting is in the box: three E27
                bulbs that shift from crisp daylight to warm evening tones, and a small bridge that
                connects them to your network. Set a wake-up schedule, dim the living room for a film,
                or bring every light on at once when you walk in the door.</p>
                <ul>
                    <li>Tunable white from warm 2700K to cool 6500K</li>
                    <li>Works over Wi-Fi or Zigbee, no separate hub subscription</li>
                    <li>Schedules, scenes, and group control from the app</li>
                    <li>Standard E27 fitting, fits most existing lamps</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Bulbs included</th><td>3, E27, 9 W each</td></tr>
                        <tr><th>Connectivity</th><td>Wi-Fi, Zigbee</td></tr>
                        <tr><th>Color temperature</th><td>2700K to 6500K</td></tr>
                        <tr><th>Lifetime</th><td>Rated 25,000 hours</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Luma Smart Bulb Starter Kit",
            MetaDescription = "Three tunable-white E27 smart bulbs and a bridge, with app schedules, scenes, and group control.",
            Sku = "TA-005",
            Price = 59.99,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 160,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.35,
            Length = 14,
            Width = 10,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Smart Home"), DisplayOrder = 1 } },
            BrandId = BrandId("Luma Lighting"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Connectivity", "Wi-Fi", 2),
                Spec("Connectivity", "Zigbee", 3)
            }
        };
        await AddProductPictures(lumaSmartBulbStarterKit, "product_luma_smart_bulb_starter_kit_1.jpg");
        await _productRepository.InsertAsync(lumaSmartBulbStarterKit);
        products.Add(lumaSmartBulbStarterKit);

        var nordvikSmartThermostat = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Nordvik Smart Thermostat",
            ShortDescription = "A learning thermostat that builds your heating schedule and trims your bill.",
            FullDescription = """
                <p>Nordvik watches when your home actually needs heat, not just what a fixed timer
                assumes, and adjusts the schedule over the first couple of weeks until it fits how you
                live. Turn the heating down from the app on your commute home, get a monthly summary
                of what you used, and let the thermostat quietly find the savings you would not have
                noticed on your own.</p>
                <ul>
                    <li>Learns your routine and builds a schedule automatically</li>
                    <li>App and voice control from anywhere</li>
                    <li>Monthly energy reports with saving tips</li>
                    <li>Simple DIY install, compatible with most systems</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Connectivity</th><td>Wi-Fi</td></tr>
                        <tr><th>Display</th><td>Backlit LCD</td></tr>
                        <tr><th>Installation</th><td>DIY, most heating systems</td></tr>
                        <tr><th>Weight</th><td>180 g</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Nordvik Smart Thermostat",
            MetaDescription = "A learning smart thermostat with app control, automatic scheduling, and monthly energy reports.",
            Sku = "TA-006",
            Price = 199.00,
            MarkAsNew = true,
            IsFreeShipping = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 90,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.18,
            Length = 10,
            Width = 10,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Smart Home"), DisplayOrder = 2 } },
            BrandId = BrandId("Nordvik Home"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Connectivity", "Wi-Fi", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Snow White", "#fafafa", 0),
                    ("Graphite", "#4a4a4a", 0))
            }
        };
        await AddProductPictures(nordvikSmartThermostat,
            "product_nordvik_smart_thermostat_1.jpg",
            "product_nordvik_smart_thermostat_2.jpg");
        await _productRepository.InsertAsync(nordvikSmartThermostat);
        products.Add(nordvikSmartThermostat);

        var auroraHomeVoiceSpeaker = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Aurora Home Voice Speaker",
            ShortDescription = "A fabric-wrapped voice speaker that also controls your smart bulbs and thermostat.",
            FullDescription = """
                <p>Home sits quietly on the kitchen counter until you need it, then answers questions,
                plays music, and reads out timers without you touching a screen. Pair it with a Luma
                bulb or a Nordvik thermostat and it becomes the single voice that runs the room: lights
                down for dinner, heating up before you're out of bed.</p>
                <ul>
                    <li>Full-range driver tuned for spoken word and music alike</li>
                    <li>Controls Luma bulbs and the Nordvik thermostat by voice</li>
                    <li>Far-field microphones pick up your voice across the room</li>
                    <li>Soft fabric finish that fits a kitchen counter or a shelf</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Connectivity</th><td>Wi-Fi, Bluetooth 5.3</td></tr>
                        <tr><th>Power</th><td>Mains powered</td></tr>
                        <tr><th>Finish</th><td>Woven fabric shell</td></tr>
                        <tr><th>Weight</th><td>0.4 kg</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Aurora Home Voice Speaker",
            MetaDescription = "A fabric-wrapped voice speaker that plays music and controls Luma bulbs and the Nordvik thermostat.",
            Sku = "TA-007",
            Price = 99.00,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 130,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.4,
            Length = 12,
            Width = 12,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Smart Home"), DisplayOrder = 3 } },
            BrandId = BrandId("Aurora Audio"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Grey", 1),
                Spec("Connectivity", "Wi-Fi", 2),
                Spec("Connectivity", "Bluetooth 5.3", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Chalk", "#e8e6e1", 0),
                    ("Charcoal", "#36454f", 0))
            }
        };
        await AddProductPictures(auroraHomeVoiceSpeaker, "product_aurora_home_voice_speaker_1.jpg");
        await _productRepository.InsertAsync(auroraHomeVoiceSpeaker);
        products.Add(auroraHomeVoiceSpeaker);

        var nordvikAirQualityMonitor = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Nordvik Air Quality Monitor",
            ShortDescription = "A desktop monitor that tracks CO2, humidity, and temperature in real time.",
            FullDescription = """
                <p>Air you cannot see is easy to ignore until a stuffy afternoon meeting or a bedroom
                that never quite feels fresh in the morning. This monitor tracks CO2, humidity, and
                temperature continuously and nudges you to crack a window before the numbers creep up,
                logging a history in the app so you can see what actually changes when you do.</p>
                <ul>
                    <li>Tracks CO2, humidity, and temperature continuously</li>
                    <li>Colour-coded display, glance and know if the air needs freshening</li>
                    <li>Battery backup keeps logging through a power cut</li>
                    <li>History and trends in the companion app</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Sensors</th><td>CO2, humidity, temperature</td></tr>
                        <tr><th>Connectivity</th><td>Wi-Fi, USB-C</td></tr>
                        <tr><th>Battery life</th><td>2 to 7 days on battery backup</td></tr>
                        <tr><th>Weight</th><td>220 g</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Nordvik Air Quality Monitor",
            MetaDescription = "A desktop air quality monitor tracking CO2, humidity, and temperature with app history and trends.",
            Sku = "TA-008",
            Price = 89.00,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 95,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.22,
            Length = 9,
            Width = 9,
            Height = 12,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Smart Home"), DisplayOrder = 4 } },
            BrandId = BrandId("Nordvik Home"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Connectivity", "Wi-Fi", 2),
                Spec("Connectivity", "USB-C", 3),
                Spec("Battery life", "2 to 7 days", 4)
            }
        };
        await AddProductPictures(nordvikAirQualityMonitor, "product_nordvik_air_quality_monitor_1.jpg");
        await _productRepository.InsertAsync(nordvikAirQualityMonitor);
        products.Add(nordvikAirQualityMonitor);

        #endregion

        #region Wearables

        var kineticPaceGpsWatch = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Pace GPS Watch",
            ShortDescription = "A GPS running watch with a 14-day battery and offline maps for trail routes.",
            FullDescription = """
                <p>Pace is built for runners who go past the edge of the phone signal. Offline maps
                mean a wrong turn on an unfamiliar trail is a detour, not a problem, and the 14-day
                battery means the watch is ready when you are, not on the charger. Multi-band GPS
                keeps the route accurate even between tall buildings or under tree cover.</p>
                <ul>
                    <li>14-day battery in watch mode, 30 hours in full GPS tracking</li>
                    <li>Offline maps for trail and street routes</li>
                    <li>Multi-band GPS for accurate tracking in tough conditions</li>
                    <li>Heart rate, sleep, and recovery tracking built in</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Battery life</th><td>Up to 14 days</td></tr>
                        <tr><th>Case</th><td>Aluminium, 42 mm or 46 mm</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3, NFC</td></tr>
                        <tr><th>Water resistance</th><td>5 ATM</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Kinetic Pace GPS Watch",
            MetaDescription = "A GPS running watch with offline trail maps, multi-band GPS, and a 14-day battery.",
            Sku = "TA-009",
            Price = 299.00,
            OldPrice = 349.00,
            ShowOnHomePage = true,
            BestSeller = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 100,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.06,
            Length = 5,
            Width = 5,
            Height = 2,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Wearables"), DisplayOrder = 1 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Aluminium", 2),
                Spec("Connectivity", "Bluetooth 5.3", 3),
                Spec("Connectivity", "NFC", 4),
                Spec("Battery life", "Over 7 days", 5)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Obsidian", "#0b0b0f", 0),
                    ("Glacier", "#a9c6c9", 0)),
                SizeAttribute(2,
                    ("42 mm", 0),
                    ("46 mm", 30.00))
            }
        };
        await AddProductPictures(kineticPaceGpsWatch,
            "product_kinetic_pace_gps_watch_1.jpg",
            "product_kinetic_pace_gps_watch_2.jpg");
        await _productRepository.InsertAsync(kineticPaceGpsWatch);
        products.Add(kineticPaceGpsWatch);

        var kineticLoopFitnessBand = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Loop Fitness Band",
            ShortDescription = "A light everyday fitness band tracking steps, heart rate, and sleep.",
            FullDescription = """
                <p>Loop stays on your wrist through the whole day and most of the night, quietly
                counting steps, tracking heart rate, and watching how well you actually sleep. It is
                thin enough to forget about under a jacket cuff and the battery goes days between
                charges, so it is genuinely there every morning rather than left on the nightstand.</p>
                <ul>
                    <li>Continuous heart rate and sleep stage tracking</li>
                    <li>Up to 7 days of battery on a single charge</li>
                    <li>Slim silicone band, comfortable for all-day wear</li>
                    <li>Smart alarm wakes you at the lightest point in your sleep cycle</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Battery life</th><td>2 to 7 days</td></tr>
                        <tr><th>Material</th><td>Silicone band</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3</td></tr>
                        <tr><th>Water resistance</th><td>Swim-proof</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Kinetic Loop Fitness Band",
            MetaDescription = "A slim everyday fitness band with continuous heart rate, sleep tracking, and a smart alarm.",
            Sku = "TA-010",
            Price = 79.00,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 170,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.03,
            Length = 24,
            Width = 2,
            Height = 1,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Wearables"), DisplayOrder = 2 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Silicone", 2),
                Spec("Connectivity", "Bluetooth 5.3", 3),
                Spec("Battery life", "2 to 7 days", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Black", "#1a1a1a", 0),
                    ("Coral", "#ff6f61", 0),
                    ("Sage", "#9caf88", 0))
            }
        };
        await AddProductPictures(kineticLoopFitnessBand, "product_kinetic_loop_fitness_band_1.jpg");
        await _productRepository.InsertAsync(kineticLoopFitnessBand);
        products.Add(kineticLoopFitnessBand);

        var kineticSleepRing = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Sleep Ring",
            ShortDescription = "A titanium sleep tracking ring, light enough to forget you're wearing it.",
            FullDescription = """
                <p>Sleep Ring trades the usual wrist strap for a smooth titanium band, so light it
                disappears within the first night. It tracks sleep stages, overnight heart rate, and
                skin temperature, and turns the numbers into a simple readiness score each morning, so
                you know whether today calls for a hard session or an easy one. A free sizing kit ships
                with every order to make sure your first ring fits.</p>
                <ul>
                    <li>Sleep stages, heart rate, and skin temperature overnight</li>
                    <li>Titanium body, scratch-resistant and hypoallergenic</li>
                    <li>Up to 7 days of battery, full charge in under an hour</li>
                    <li>Free sizing kit included so you order the right fit</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Material</th><td>Titanium</td></tr>
                        <tr><th>Battery life</th><td>2 to 7 days</td></tr>
                        <tr><th>Connectivity</th><td>Bluetooth 5.3</td></tr>
                        <tr><th>Water resistance</th><td>10 ATM</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Kinetic Sleep Ring",
            MetaDescription = "A titanium sleep tracking ring with overnight heart rate, skin temperature, and a daily readiness score.",
            Sku = "TA-011",
            Price = 199.00,
            MarkAsNew = true,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 80,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.005,
            Length = 2,
            Width = 2,
            Height = 1,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Wearables"), DisplayOrder = 3 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Metallic", 1),
                Spec("Material", "Titanium", 2),
                Spec("Connectivity", "Bluetooth 5.3", 3),
                Spec("Battery life", "2 to 7 days", 4)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("7", 0), ("8", 0), ("9", 0), ("10", 0), ("11", 0), ("12", 0)),
                ColorAttribute(2,
                    ("Matte Black", "#1c1c1c", 0),
                    ("Silver", "#c0c0c0", 0))
            }
        };
        await AddProductPictures(kineticSleepRing, "product_kinetic_sleep_ring_1.jpg");
        await _productRepository.InsertAsync(kineticSleepRing);
        products.Add(kineticSleepRing);

        var kineticSportWatchStrap = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Kinetic Sport Watch Strap",
            ShortDescription = "A soft silicone replacement strap, sized to fit the Kinetic Pace GPS Watch.",
            FullDescription = """
                <p>Swap the strap and the watch feels new again. This soft silicone band is quick to
                fit, breathes better than the stock strap over a long run, and comes in colours that
                go from the trail to the office without a second thought. It is cut to fit the Kinetic
                Pace GPS Watch exactly, holes and all.</p>
                <ul>
                    <li>Soft, breathable silicone for long runs</li>
                    <li>Quick-release pins, no tools needed to swap</li>
                    <li>Sized to fit the Kinetic Pace GPS Watch</li>
                    <li>Available in two widths for either case size</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Material</th><td>Silicone</td></tr>
                        <tr><th>Widths</th><td>20 mm, 22 mm</td></tr>
                        <tr><th>Compatibility</th><td>Kinetic Pace GPS Watch</td></tr>
                        <tr><th>Weight</th><td>18 g</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Kinetic Sport Watch Strap",
            MetaDescription = "A soft, breathable silicone replacement strap sized to fit the Kinetic Pace GPS Watch.",
            Sku = "TA-012",
            Price = 24.99,
            TaxCategoryId = TaxCategoryId("Electronics & Software"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 220,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.02,
            Length = 24,
            Width = 3,
            Height = 1,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Wearables"), DisplayOrder = 4 } },
            BrandId = BrandId("Kinetic"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Multicolor", 1),
                Spec("Material", "Silicone", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Black", "#1a1a1a", 0),
                    ("Ocean Blue", "#1f6f8b", 0),
                    ("Sage", "#9caf88", 0)),
                SizeAttribute(2,
                    ("20 mm", 0),
                    ("22 mm", 0))
            }
        };
        await AddProductPictures(kineticSportWatchStrap, "product_kinetic_sport_watch_strap_1.jpg");
        await _productRepository.InsertAsync(kineticSportWatchStrap);
        products.Add(kineticSportWatchStrap);

        #endregion

        return products;
    }

    private ProductAttributeMapping ColorAttribute(int displayOrder, params (string Name, string Hex, double Adjustment)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId("Color"),
            AttributeControlTypeId = AttributeControlType.ColorSquares,
            IsRequired = true,
            DisplayOrder = displayOrder
        };
        for (var i = 0; i < values.Length; i++)
            mapping.ProductAttributeValues.Add(new ProductAttributeValue {
                Name = values[i].Name,
                ColorSquaresRgb = values[i].Hex,
                PriceAdjustment = values[i].Adjustment,
                DisplayOrder = i,
                IsPreSelected = i == 0
            });
        return mapping;
    }

    private ProductAttributeMapping SizeAttribute(int displayOrder, params (string Name, double Adjustment)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId("Size"),
            AttributeControlTypeId = AttributeControlType.DropdownList,
            IsRequired = true,
            DisplayOrder = displayOrder
        };
        for (var i = 0; i < values.Length; i++)
            mapping.ProductAttributeValues.Add(new ProductAttributeValue {
                Name = values[i].Name,
                PriceAdjustment = values[i].Adjustment,
                DisplayOrder = i,
                IsPreSelected = i == 0
            });
        return mapping;
    }
}
