using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Home & Living department products: inserts products (with pictures, categories, brand, vendor,
    // specs, attributes) and returns them. Does not create slugs, tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsHomeLiving(SampleProductContext ctx)
    {
        var products = new List<Product>();

        #region Kitchen & Coffee

        var nordvikGooseneckKettle = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Nordvik Gooseneck Kettle",
            ShortDescription = "A precision gooseneck kettle with temperature hold, built for slow pour over mornings.",
            FullDescription = """
                <p>Pour over lives or dies on control, and the Nordvik Gooseneck gives you plenty of
                it. The thin spout lays down a slow, steady stream so you can bloom the grounds and
                then circle in the rest of the water at your own pace, while the temperature hold
                keeps a full brew session in the sweet spot without a second trip to the stove.</p>
                <ul>
                    <li>Gooseneck spout for a precise, controllable pour</li>
                    <li>Temperature hold keeps water ready through a full brew</li>
                    <li>0.9 L capacity, enough for two generous cups</li>
                    <li>Looks equally at home on the counter or the breakfast table</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Capacity</th><td>0.9 L</td></tr>
                        <tr><th>Material</th><td>Stainless steel</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Nordvik Gooseneck Kettle",
            MetaDescription = "A precision gooseneck kettle with temperature hold and a 0.9 L capacity for slow, controlled pour over.",
            Sku = "HL-001",
            Price = 89.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 120,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.75,
            Length = 22,
            Width = 12,
            Height = 21,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 1 } },
            BrandId = BrandId("Nordvik Home"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Stainless steel", 2),
                Spec("Capacity", "0.5 to 1.5 L", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Matte Black", "#1a1a1a", 0),
                    ("Brushed Steel", "#b0b0b0", 0),
                    ("Copper", "#b87333", 15.00))
            }
        };
        await AddProductPictures(nordvikGooseneckKettle,
            "product_nordvik_gooseneck_kettle_1.jpg",
            "product_nordvik_gooseneck_kettle_2.jpg");
        await _productRepository.InsertAsync(nordvikGooseneckKettle);
        products.Add(nordvikGooseneckKettle);

        var emberCeramicPourOverDripper = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Ember Ceramic Pour Over Dripper",
            ShortDescription = "A hand-glazed ceramic dripper that holds heat through the whole brew.",
            FullDescription = """
                <p>Ceramic holds heat far better than glass or plastic, and it shows in the cup: the
                brew stays warmer for longer, and the flavour comes through cleaner. Each dripper is
                glazed by hand, so the finish varies slightly piece to piece, which is exactly the
                point.</p>
                <ul>
                    <li>Glazed stoneware body retains heat through the whole pour</li>
                    <li>Fits standard cone filters</li>
                    <li>Sits securely on mugs and carafes of most sizes</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Material</th><td>Ceramic</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Ember Ceramic Pour Over Dripper",
            MetaDescription = "A hand-glazed ceramic pour over dripper that holds heat through the brew for a cleaner cup.",
            Sku = "HL-002",
            Price = 34.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
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
            Width = 14,
            Height = 12,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 2 } },
            BrandId = BrandId("Ember & Oak"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Material", "Ceramic", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Chalk White", "#f5f1e9", 0),
                    ("Speckled Clay", "#c9a38a", 0))
            }
        };
        await AddProductPictures(emberCeramicPourOverDripper,
            "product_ember_ceramic_pour_over_dripper_1.jpg");
        await _productRepository.InsertAsync(emberCeramicPourOverDripper);
        products.Add(emberCeramicPourOverDripper);

        var emberSingleOriginCoffeeBeans = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Ember Single Origin Coffee Beans",
            ShortDescription = "Small-batch single origin beans, roasted to order and ground the way you brew.",
            FullDescription = """
                <p>One farm, one harvest, roasted in small batches so what you taste is the coffee, not
                the roast. Pick your grind and your bag size, and buy the bigger bags with confidence:
                the tier pricing rewards stocking up for the weeks ahead.</p>
                <ul>
                    <li>Single origin beans, roasted weekly in small batches</li>
                    <li>Ground to order: whole bean, espresso, filter, or French press</li>
                    <li>250 g, 500 g, or 1 kg bags</li>
                </ul>
                """,
            MetaTitle = "Ember Single Origin Coffee Beans",
            MetaDescription = "Small-batch single origin coffee beans, ground to order, with tier pricing on the larger bags.",
            Sku = "HL-003",
            Price = 18.99,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 300,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.25,
            Length = 10,
            Width = 6,
            Height = 18,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 3 } },
            BrandId = BrandId("Ember & Oak"),
            TierPrices = {
                new TierPrice { Quantity = 3, Price = 16.99 },
                new TierPrice { Quantity = 6, Price = 15.49 }
            },
            ProductAttributeMappings = {
                new ProductAttributeMapping {
                    ProductAttributeId = ProductAttributeId("Capacity"),
                    AttributeControlTypeId = AttributeControlType.RadioList,
                    IsRequired = true,
                    DisplayOrder = 1,
                    ProductAttributeValues = {
                        new ProductAttributeValue { Name = "250 g", PriceAdjustment = 0, DisplayOrder = 0, IsPreSelected = true },
                        new ProductAttributeValue { Name = "500 g", PriceAdjustment = 14.00, DisplayOrder = 1 },
                        new ProductAttributeValue { Name = "1 kg", PriceAdjustment = 29.00, DisplayOrder = 2 }
                    }
                },
                new ProductAttributeMapping {
                    ProductAttributeId = ProductAttributeId("Grind"),
                    AttributeControlTypeId = AttributeControlType.DropdownList,
                    IsRequired = true,
                    DisplayOrder = 2,
                    ProductAttributeValues = {
                        new ProductAttributeValue { Name = "Whole bean", DisplayOrder = 0, IsPreSelected = true },
                        new ProductAttributeValue { Name = "Espresso", DisplayOrder = 1 },
                        new ProductAttributeValue { Name = "Filter", DisplayOrder = 2 },
                        new ProductAttributeValue { Name = "French press", DisplayOrder = 3 }
                    }
                }
            }
        };
        await AddProductPictures(emberSingleOriginCoffeeBeans,
            "product_ember_single_origin_coffee_beans_1.jpg",
            "product_ember_single_origin_coffee_beans_2.jpg");
        await _productRepository.InsertAsync(emberSingleOriginCoffeeBeans);
        products.Add(emberSingleOriginCoffeeBeans);

        var slowMorningCoffeeBundle = new Product {
            ProductTypeId = ProductType.BundledProduct,
            VisibleIndividually = true,
            Name = "Slow Morning Coffee Bundle",
            ShortDescription = "Kettle, dripper, and beans in one gift-ready pour over set.",
            FullDescription = """
                <p>Everything a good pour over morning needs, boxed together and priced below buying
                the pieces apart. The gooseneck kettle, the ceramic dripper, and a bag of single origin
                beans arrive ready to give, or ready to keep for yourself.</p>
                <ul>
                    <li>Nordvik Gooseneck Kettle</li>
                    <li>Ember Ceramic Pour Over Dripper</li>
                    <li>Ember Single Origin Coffee Beans</li>
                </ul>
                """,
            MetaTitle = "Slow Morning Coffee Bundle",
            MetaDescription = "A gift-ready pour over bundle with a gooseneck kettle, a ceramic dripper, and single origin beans.",
            Sku = "HL-004",
            Price = 129.00,
            OldPrice = 141.99,
            ShowOnHomePage = true,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 80,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.5,
            Length = 30,
            Width = 22,
            Height = 14,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 4 } },
            BrandId = BrandId("Ember & Oak")
        };
        await AddProductPictures(slowMorningCoffeeBundle,
            "product_slow_morning_coffee_bundle_1.jpg",
            "product_slow_morning_coffee_bundle_2.jpg");
        await _productRepository.InsertAsync(slowMorningCoffeeBundle);
        slowMorningCoffeeBundle.BundleProducts.Add(new BundleProduct {
            ProductId = nordvikGooseneckKettle.Id, Quantity = 1, DisplayOrder = 1
        });
        slowMorningCoffeeBundle.BundleProducts.Add(new BundleProduct {
            ProductId = emberCeramicPourOverDripper.Id, Quantity = 1, DisplayOrder = 2
        });
        slowMorningCoffeeBundle.BundleProducts.Add(new BundleProduct {
            ProductId = emberSingleOriginCoffeeBeans.Id, Quantity = 1, DisplayOrder = 3
        });
        await _productRepository.UpdateAsync(slowMorningCoffeeBundle);
        products.Add(slowMorningCoffeeBundle);

        var stonewareMugSet = new Product {
            ProductTypeId = ProductType.GroupedProduct,
            VisibleIndividually = true,
            Name = "Stoneware Mug Set",
            ShortDescription = "Handmade stoneware mugs in three sizes, thrown one at a time.",
            FullDescription = """
                <p>No two mugs come out of the kiln quite the same, which is the whole appeal of
                buying handmade. Pick a small, medium, or large from the group below, each glazed in
                the same two finishes and thrown by the same Copenhagen studio.</p>
                <ul>
                    <li>Hand thrown stoneware, glazed and fired individually</li>
                    <li>Available in Small, Medium, and Large</li>
                    <li>Made by Nordic Craft Collective</li>
                </ul>
                """,
            MetaTitle = "Stoneware Mug Set",
            MetaDescription = "Handmade stoneware mugs in three sizes from Nordic Craft Collective, thrown one at a time.",
            Sku = "HL-005",
            Price = 0.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.GroupedLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 0,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 5 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Material", "Stoneware", 1)
            }
        };
        await AddProductPictures(stonewareMugSet,
            "product_stoneware_mug_set_1.jpg",
            "product_stoneware_mug_set_2.jpg");
        await _productRepository.InsertAsync(stonewareMugSet);
        products.Add(stonewareMugSet);

        var stonewareMugSmall = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = false,
            ParentGroupedProductId = stonewareMugSet.Id,
            Name = "Stoneware Mug Small",
            ShortDescription = "A 250 ml handmade stoneware mug, glazed in Oat or Slate.",
            FullDescription = """
                <p>The small size in the Stoneware Mug Set, a 250 ml everyday cup thrown and glazed by
                hand at Nordic Craft Collective's Copenhagen studio.</p>
                """,
            MetaTitle = "Stoneware Mug Small",
            MetaDescription = "A 250 ml handmade stoneware mug, glazed in Oat or Slate, from Nordic Craft Collective.",
            Sku = "HL-006",
            Price = 22.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 60,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.28,
            Length = 9,
            Width = 9,
            Height = 8,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 6 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Material", "Stoneware", 1),
                Spec("Capacity", "Up to 0.5 L", 2),
                Spec("Color family", "Beige", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Oat Glaze", "#d8c7a1", 0),
                    ("Slate Glaze", "#5c6b6e", 0))
            }
        };
        await AddProductPictures(stonewareMugSmall, "product_stoneware_mug_small_1.jpg");
        await _productRepository.InsertAsync(stonewareMugSmall);
        products.Add(stonewareMugSmall);

        var stonewareMugMedium = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = false,
            ParentGroupedProductId = stonewareMugSet.Id,
            Name = "Stoneware Mug Medium",
            ShortDescription = "A 350 ml handmade stoneware mug, glazed in Oat or Slate.",
            FullDescription = """
                <p>The medium size in the Stoneware Mug Set, a 350 ml mug thrown and glazed by hand at
                Nordic Craft Collective's Copenhagen studio. A favourite for a flat white.</p>
                """,
            MetaTitle = "Stoneware Mug Medium",
            MetaDescription = "A 350 ml handmade stoneware mug, glazed in Oat or Slate, from Nordic Craft Collective.",
            Sku = "HL-007",
            Price = 26.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 55,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.34,
            Length = 10,
            Width = 10,
            Height = 9,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 7 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Material", "Stoneware", 1),
                Spec("Capacity", "Up to 0.5 L", 2),
                Spec("Color family", "Beige", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Oat Glaze", "#d8c7a1", 0),
                    ("Slate Glaze", "#5c6b6e", 0))
            }
        };
        await AddProductPictures(stonewareMugMedium, "product_stoneware_mug_medium_1.jpg");
        await _productRepository.InsertAsync(stonewareMugMedium);
        products.Add(stonewareMugMedium);

        var stonewareMugLarge = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = false,
            ParentGroupedProductId = stonewareMugSet.Id,
            Name = "Stoneware Mug Large",
            ShortDescription = "A 450 ml handmade stoneware mug, glazed in Oat or Slate.",
            FullDescription = """
                <p>The large size in the Stoneware Mug Set, a 450 ml mug thrown and glazed by hand at
                Nordic Craft Collective's Copenhagen studio. Big enough for a full pour of tea.</p>
                """,
            MetaTitle = "Stoneware Mug Large",
            MetaDescription = "A 450 ml handmade stoneware mug, glazed in Oat or Slate, from Nordic Craft Collective.",
            Sku = "HL-008",
            Price = 29.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 45,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.40,
            Length = 11,
            Width = 11,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 8 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Material", "Stoneware", 1),
                Spec("Capacity", "Up to 0.5 L", 2),
                Spec("Color family", "Beige", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Oat Glaze", "#d8c7a1", 0),
                    ("Slate Glaze", "#5c6b6e", 0))
            }
        };
        await AddProductPictures(stonewareMugLarge, "product_stoneware_mug_large_1.jpg");
        await _productRepository.InsertAsync(stonewareMugLarge);
        products.Add(stonewareMugLarge);

        var walnutServingBoard = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Walnut Serving Board",
            ShortDescription = "A hand-finished walnut board, optionally engraved for a lasting gift.",
            FullDescription = """
                <p>Cut from a single piece of walnut and finished with oil over three days, this board
                is built to be used, not just displayed. Add an engraving and it turns into a keepsake:
                a wedding date, initials, or a short message, cut cleanly into the grain.</p>
                <ul>
                    <li>Solid walnut, oiled by hand</li>
                    <li>Medium or Large</li>
                    <li>Optional engraving, up to 30 characters</li>
                </ul>
                <table class="table table-bordered">
                    <tbody>
                        <tr><th>Material</th><td>Walnut wood</td></tr>
                    </tbody>
                </table>
                """,
            MetaTitle = "Walnut Serving Board",
            MetaDescription = "A hand-finished solid walnut serving board with an optional engraving, up to 30 characters.",
            Sku = "HL-009",
            Price = 64.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 70,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.9,
            Length = 40,
            Width = 20,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Kitchen & Coffee"), DisplayOrder = 9 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Brown", 1),
                Spec("Material", "Walnut wood", 2)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("Medium", 0),
                    ("Large", 18.00)),
                new ProductAttributeMapping {
                    ProductAttributeId = ProductAttributeId("Engraving"),
                    AttributeControlTypeId = AttributeControlType.TextBox,
                    IsRequired = false,
                    ValidationMaxLength = 30,
                    DisplayOrder = 2
                }
            }
        };
        await AddProductPictures(walnutServingBoard,
            "product_walnut_serving_board_1.jpg",
            "product_walnut_serving_board_2.jpg");
        await _productRepository.InsertAsync(walnutServingBoard);
        products.Add(walnutServingBoard);

        #endregion

        #region Lighting

        var lumaArcFloorLamp = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Luma Arc Floor Lamp",
            ShortDescription = "A dimmable arc lamp that reaches over an armchair from a floor-standing base.",
            FullDescription = """
                <p>The arc puts light exactly where a reading chair needs it, without a cord running
                across the floor or a side table in the way. A smooth dimmer takes it from a bright
                reading light down to an evening glow with a single twist.</p>
                <ul>
                    <li>Arc reach over a chair or sofa from a weighted floor base</li>
                    <li>Dimmable, from reading light to evening glow</li>
                    <li>Brass or Matte Black finish</li>
                </ul>
                """,
            MetaTitle = "Luma Arc Floor Lamp",
            MetaDescription = "A dimmable arc floor lamp that reaches over a reading chair, in Brass or Matte Black.",
            Sku = "HL-010",
            Price = 229.00,
            ShowOnHomePage = true,
            IsFreeShipping = true,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 40,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 8.5,
            Length = 150,
            Width = 40,
            Height = 30,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Lighting"), DisplayOrder = 1 } },
            BrandId = BrandId("Luma Lighting"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Metallic", 1),
                Spec("Material", "Brass", 2),
                Spec("Material", "Steel", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Brass", "#c9a24b", 0),
                    ("Matte Black", "#1a1a1a", 0))
            }
        };
        await AddProductPictures(lumaArcFloorLamp,
            "product_luma_arc_floor_lamp_1.jpg",
            "product_luma_arc_floor_lamp_2.jpg");
        await _productRepository.InsertAsync(lumaArcFloorLamp);
        products.Add(lumaArcFloorLamp);

        var lumaGlowTableLamp = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Luma Glow Table Lamp",
            ShortDescription = "A mouth-blown glass table lamp that casts a soft, warm pool of light.",
            FullDescription = """
                <p>Each shade is mouth-blown, so the glass carries small, honest variations that
                scatter the light softly rather than throwing a hard beam. It reads as a quiet glow on
                a nightstand or a side table, not a spotlight.</p>
                <ul>
                    <li>Mouth-blown glass shade</li>
                    <li>Soft, diffused light, ideal for a bedroom or living room</li>
                    <li>Opal White or Smoke Grey</li>
                </ul>
                """,
            MetaTitle = "Luma Glow Table Lamp",
            MetaDescription = "A mouth-blown glass table lamp with a soft, warm glow, in Opal White or Smoke Grey.",
            Sku = "HL-011",
            Price = 89.00,
            OldPrice = 109.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 90,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.2,
            Length = 25,
            Width = 25,
            Height = 35,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Lighting"), DisplayOrder = 2 } },
            BrandId = BrandId("Luma Lighting"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Material", "Glass", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Opal White", "#f2efe9", 0),
                    ("Smoke Grey", "#7c7c7c", 0))
            }
        };
        await AddProductPictures(lumaGlowTableLamp, "product_luma_glow_table_lamp_1.jpg");
        await _productRepository.InsertAsync(lumaGlowTableLamp);
        products.Add(lumaGlowTableLamp);

        var lumaPaperPendantLight = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Luma Paper Pendant Light",
            ShortDescription = "A rice paper pendant shade that softens every bulb it hangs over.",
            FullDescription = """
                <p>Hung low over a dining table, a paper shade turns an ordinary bulb into something
                closer to lantern light: soft, even, and warm at the edges. Choose the 40 cm shade for
                a smaller table, or the 60 cm for a longer one.</p>
                <ul>
                    <li>Rice paper shade over a lightweight frame</li>
                    <li>40 cm or 60 cm diameter</li>
                    <li>Best over a dining or kitchen table</li>
                </ul>
                """,
            MetaTitle = "Luma Paper Pendant Light",
            MetaDescription = "A rice paper pendant light shade for a dining table, available in 40 cm or 60 cm.",
            Sku = "HL-012",
            Price = 119.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 65,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.6,
            Length = 40,
            Width = 40,
            Height = 25,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Lighting"), DisplayOrder = 3 } },
            BrandId = BrandId("Luma Lighting"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Material", "Paper", 2)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("40 cm", 0),
                    ("60 cm", 40.00))
            }
        };
        await AddProductPictures(lumaPaperPendantLight, "product_luma_paper_pendant_light_1.jpg");
        await _productRepository.InsertAsync(lumaPaperPendantLight);
        products.Add(lumaPaperPendantLight);

        var lumaRechargeableLanternLamp = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Luma Rechargeable Lantern Lamp",
            ShortDescription = "A cordless lantern lamp for the terrace, the tent, or the table, with three dim levels.",
            FullDescription = """
                <p>No cord, no outlet: charge it over USB-C and carry it wherever the evening moves,
                from the dinner table to the terrace steps. Three brightness levels cover everything
                from a soft dinner glow to a proper reading light.</p>
                <ul>
                    <li>USB-C rechargeable, cordless use anywhere</li>
                    <li>Three dim levels</li>
                    <li>12 to 24 hours of runtime depending on brightness</li>
                </ul>
                """,
            MetaTitle = "Luma Rechargeable Lantern Lamp",
            MetaDescription = "A cordless, USB-C rechargeable lantern lamp with three dim levels for indoor or outdoor use.",
            Sku = "HL-013",
            Price = 69.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 100,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.5,
            Length = 12,
            Width = 12,
            Height = 20,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Lighting"), DisplayOrder = 4 } },
            BrandId = BrandId("Luma Lighting"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Aluminium", 2),
                Spec("Connectivity", "USB-C", 3),
                Spec("Battery life", "12 to 24 hours", 4)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Sand", "#d9c7a3", 0),
                    ("Olive", "#708238", 0),
                    ("Charcoal", "#36454f", 0))
            }
        };
        await AddProductPictures(lumaRechargeableLanternLamp,
            "product_luma_rechargeable_lantern_lamp_1.jpg",
            "product_luma_rechargeable_lantern_lamp_2.jpg");
        await _productRepository.InsertAsync(lumaRechargeableLanternLamp);
        products.Add(lumaRechargeableLanternLamp);

        #endregion

        #region Textiles & Decor

        var loomLinenThrowBlanket = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Linen Throw Blanket",
            ShortDescription = "A stonewashed linen throw that softens with every wash.",
            FullDescription = """
                <p>Linen starts out a little crisp and only gets better from there: every wash softens
                the weave a bit more, so a couple of seasons in it drapes like nothing else on the
                sofa. Generously sized at 130 by 180 cm, big enough to share.</p>
                <ul>
                    <li>Stonewashed linen, softens with every wash</li>
                    <li>130 x 180 cm</li>
                    <li>Oat, Sage, or Charcoal</li>
                </ul>
                """,
            MetaTitle = "Loom Linen Throw Blanket",
            MetaDescription = "A 130 x 180 cm stonewashed linen throw blanket that softens with every wash.",
            Sku = "HL-014",
            Price = 79.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 110,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.8,
            Length = 35,
            Width = 25,
            Height = 8,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Textiles & Decor"), DisplayOrder = 1 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Linen", 2),
                Spec("Season", "All season", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Oat", "#d8c7a1", 0),
                    ("Sage", "#9caf88", 0),
                    ("Charcoal", "#36454f", 0))
            }
        };
        await AddProductPictures(loomLinenThrowBlanket,
            "product_loom_linen_throw_blanket_1.jpg",
            "product_loom_linen_throw_blanket_2.jpg");
        await _productRepository.InsertAsync(loomLinenThrowBlanket);
        products.Add(loomLinenThrowBlanket);

        var loomWaffleBathTowelSet = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Waffle Bath Towel Set",
            ShortDescription = "Two bath towels and two hand towels in absorbent organic cotton waffle weave.",
            FullDescription = """
                <p>Waffle weave organic cotton dries faster and stays lighter than a dense terry towel,
                without giving up any softness. The set covers a full bathroom refresh: two bath
                towels, two hand towels, one calm colour.</p>
                <ul>
                    <li>Two bath towels, two hand towels</li>
                    <li>Organic cotton waffle weave</li>
                    <li>White, Clay, or Moss</li>
                </ul>
                """,
            MetaTitle = "Loom Waffle Bath Towel Set",
            MetaDescription = "A four-piece organic cotton waffle weave towel set: two bath towels, two hand towels.",
            Sku = "HL-015",
            Price = 59.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 95,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.1,
            Length = 30,
            Width = 25,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Textiles & Decor"), DisplayOrder = 2 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Material", "Organic cotton", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("White", "#ffffff", 0),
                    ("Clay", "#b66a50", 0),
                    ("Moss", "#6b8e63", 0))
            }
        };
        await AddProductPictures(loomWaffleBathTowelSet, "product_loom_waffle_bath_towel_set_1.jpg");
        await _productRepository.InsertAsync(loomWaffleBathTowelSet);
        products.Add(loomWaffleBathTowelSet);

        var handThrownCeramicVase = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Hand Thrown Ceramic Vase",
            ShortDescription = "A one-of-a-kind stoneware vase, thrown and glazed by hand.",
            FullDescription = """
                <p>Every piece comes off the wheel a little different, so the vase you get is genuinely
                the only one like it. Simple enough to sit anywhere in a room, with just enough texture
                and glaze variation to hold your attention.</p>
                <ul>
                    <li>Hand thrown stoneware, each piece unique</li>
                    <li>Ash or Rust glaze</li>
                    <li>Made by Nordic Craft Collective</li>
                </ul>
                """,
            MetaTitle = "Hand Thrown Ceramic Vase",
            MetaDescription = "A one-of-a-kind hand thrown stoneware vase from Nordic Craft Collective, in Ash or Rust.",
            Sku = "HL-016",
            Price = 48.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 35,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.6,
            Length = 15,
            Width = 15,
            Height = 20,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Textiles & Decor"), DisplayOrder = 3 } },
            VendorId = VendorId("Nordic Craft Collective"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Stoneware", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Ash", "#b7b7a4", 0),
                    ("Rust", "#b7410e", 0))
            }
        };
        await AddProductPictures(handThrownCeramicVase, "product_hand_thrown_ceramic_vase_1.jpg");
        await _productRepository.InsertAsync(handThrownCeramicVase);
        products.Add(handThrownCeramicVase);

        var woolFeltCushionCover = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Wool Felt Cushion Cover",
            ShortDescription = "A felted wool cushion cover with a hidden zip, in two sizes.",
            FullDescription = """
                <p>Wool felt has a texture and a weight that woven fabric cannot match: it holds its
                shape on the sofa and takes colour beautifully. The zip is hidden along the seam, so
                the cover stays clean-lined from every angle. Cover only, insert not included.</p>
                <ul>
                    <li>Felted wool, holds its shape</li>
                    <li>Hidden zip closure</li>
                    <li>45 x 45 cm or 50 x 50 cm</li>
                </ul>
                """,
            MetaTitle = "Wool Felt Cushion Cover",
            MetaDescription = "A felted wool cushion cover with a hidden zip, in 45 x 45 cm or 50 x 50 cm.",
            Sku = "HL-017",
            Price = 39.00,
            TaxCategoryId = TaxCategoryId("Home & Living"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 130,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.3,
            Length = 30,
            Width = 25,
            Height = 4,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Textiles & Decor"), DisplayOrder = 4 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Multicolor", 1),
                Spec("Material", "Wool felt", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Mustard", "#e1ad01", 0),
                    ("Stone", "#a6a28e", 0),
                    ("Forest", "#228b22", 0)),
                SizeAttribute(2,
                    ("45 x 45 cm", 0),
                    ("50 x 50 cm", 4.00))
            }
        };
        await AddProductPictures(woolFeltCushionCover, "product_wool_felt_cushion_cover_1.jpg");
        await _productRepository.InsertAsync(woolFeltCushionCover);
        products.Add(woolFeltCushionCover);

        #endregion

        return products;
    }
}
