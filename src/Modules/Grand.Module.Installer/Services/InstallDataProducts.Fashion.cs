using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Fashion department products: inserts products (with pictures, categories, brand, vendor,
    // specs, attributes) and returns them. Does not create slugs, tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsFashion(SampleProductContext ctx)
    {
        var products = new List<Product>();

        #region Women

        // 30 - Loom Merino Wrap Cardigan: Size x Color attribute combinations, per-value pictures.
        var cardigan = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Merino Wrap Cardigan",
            ShortDescription = "A wrap cardigan in brushed merino, soft enough to live in from autumn through winter.",
            FullDescription = """
                <p>Knitted from brushed merino wool, this wrap cardigan layers over everything without
                ever feeling bulky. The relaxed fit and self-tie waist make it as easy over a t-shirt
                as it is over a midi dress.</p>
                <ul>
                    <li>Brushed merino wool, relaxed fit</li>
                    <li>Self-tie wrap front with two patch pockets</li>
                    <li>Ribbed cuffs and hem for a close, warm fit</li>
                </ul>
                """,
            MetaTitle = "Loom Merino Wrap Cardigan",
            MetaDescription = "Brushed merino wrap cardigan in Oat or Charcoal, sizes XS to XL, relaxed fit for layering.",
            Sku = "FA-001",
            Price = 139.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.45,
            Length = 30,
            Width = 25,
            Height = 5,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Women"), DisplayOrder = 1 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Merino wool", 2),
                Spec("Fit", "Relaxed", 3),
                Spec("Season", "Autumn", 4),
                Spec("Season", "Winter", 5)
            }
        };
        await AddProductPictures(cardigan,
            "product_loom_merino_wrap_cardigan_1.jpg",
            "product_loom_merino_wrap_cardigan_2.jpg",
            "product_loom_merino_wrap_cardigan_3.jpg");
        var cardiganPictures = cardigan.ProductPictures.OrderBy(p => p.DisplayOrder).ToList();

        var cardiganSize = SizeAttribute(1,
            ("XS", 0), ("S", 0), ("M", 0), ("L", 0), ("XL", 0));
        cardiganSize.Combination = true;
        var cardiganColor = ColorAttributeWithPictures(2, combination: true,
            ("Oat", "#D9CBB3", cardiganPictures[0].PictureId),
            ("Charcoal", "#3B3B3D", cardiganPictures[1].PictureId));
        cardigan.ProductAttributeMappings.Add(cardiganSize);
        cardigan.ProductAttributeMappings.Add(cardiganColor);

        AddSizeColorCombinations(cardigan, cardiganSize, cardiganColor,
            ("XS", "Oat", 6, false), ("S", "Oat", 8, false), ("M", "Oat", 8, false),
            ("L", "Oat", 5, false), ("XL", "Oat", 3, false),
            ("XS", "Charcoal", 0, false), ("S", "Charcoal", 4, false), ("M", "Charcoal", 6, false),
            ("L", "Charcoal", 6, false), ("XL", "Charcoal", 2, false));

        await _productRepository.InsertAsync(cardigan);
        products.Add(cardigan);

        // 31 - Loom Linen Midi Dress: per-value pictures, no combinations.
        var dress = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Linen Midi Dress",
            ShortDescription = "A relaxed linen midi dress cut for long, warm days.",
            FullDescription = """
                <p>Washed linen falls into an easy midi silhouette, with a touch of stretch at the
                waist and side pockets deep enough for keys and a phone. Wear it barefoot or layered
                under a cardigan once the evening cools down.</p>
                <ul>
                    <li>100% washed linen, relaxed fit</li>
                    <li>Side seam pockets, elasticated waist</li>
                    <li>Midi length, machine washable</li>
                </ul>
                """,
            MetaTitle = "Loom Linen Midi Dress",
            MetaDescription = "Relaxed linen midi dress in Sand or Olive, sizes XS to XL, summer weight.",
            Sku = "FA-002",
            Price = 119.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 60,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            MarkAsNew = true,
            Weight = 0.3,
            Length = 35,
            Width = 25,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Women"), DisplayOrder = 2 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Linen", 2),
                Spec("Fit", "Relaxed", 3),
                Spec("Season", "Summer", 4)
            }
        };
        await AddProductPictures(dress,
            "product_loom_linen_midi_dress_1.jpg",
            "product_loom_linen_midi_dress_2.jpg");
        var dressPictures = dress.ProductPictures.OrderBy(p => p.DisplayOrder).ToList();

        dress.ProductAttributeMappings.Add(SizeAttribute(1,
            ("XS", 0), ("S", 0), ("M", 0), ("L", 0), ("XL", 0)));
        dress.ProductAttributeMappings.Add(ColorAttributeWithPictures(2, combination: false,
            ("Sand", "#C9B79C", dressPictures[0].PictureId),
            ("Olive", "#6B6E42", dressPictures[1].PictureId)));

        await _productRepository.InsertAsync(dress);
        products.Add(dress);

        // 32 - Loom Organic Cotton Tee Women
        var tee = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Organic Cotton Tee Women",
            ShortDescription = "An everyday organic cotton tee with a clean, regular fit.",
            FullDescription = """
                <p>Our take on the basic tee: heavyweight organic cotton, a regular fit that holds its
                shape wash after wash, and a crew neck that never goes out of style.</p>
                <ul>
                    <li>100% organic cotton, mid-weight jersey</li>
                    <li>Regular fit, reinforced crew neck</li>
                    <li>Pre-shrunk, machine washable</li>
                </ul>
                """,
            MetaTitle = "Loom Organic Cotton Tee Women",
            MetaDescription = "Organic cotton crew neck tee for women in White, Black, or Sage, sizes XS to XL.",
            Sku = "FA-003",
            Price = 34.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 150,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            BestSeller = true,
            Weight = 0.18,
            Length = 25,
            Width = 20,
            Height = 2,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Women"), DisplayOrder = 3 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "White", 1),
                Spec("Material", "Organic cotton", 2),
                Spec("Fit", "Regular", 3),
                Spec("Season", "All season", 4)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("XS", 0), ("S", 0), ("M", 0), ("L", 0), ("XL", 0)),
                ColorAttribute(2,
                    ("White", "#F5F4F0", 0), ("Black", "#1E1E1E", 0), ("Sage", "#9CAF88", 0))
            }
        };
        await AddProductPictures(tee, "product_loom_organic_cotton_tee_women_1.jpg");
        await _productRepository.InsertAsync(tee);
        products.Add(tee);

        // 33 - Loom Wide Leg Trousers
        var trousers = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Wide Leg Trousers",
            ShortDescription = "Wide leg cotton trousers with a workwear-inspired cut.",
            FullDescription = """
                <p>A high-waisted, wide leg trouser in sturdy cotton twill, with a workwear-inspired
                cut that moves easily from desk to weekend. Deep pockets, a clean waistband, and a
                drape that works with flats or boots.</p>
                <ul>
                    <li>Cotton twill, relaxed wide leg</li>
                    <li>High waist with belt loops, deep side pockets</li>
                    <li>Machine washable</li>
                </ul>
                """,
            MetaTitle = "Loom Wide Leg Trousers",
            MetaDescription = "Wide leg cotton trousers in Navy or Camel, sizes XS to XL, workwear-inspired cut.",
            Sku = "FA-004",
            Price = 98.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 70,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.4,
            Length = 35,
            Width = 25,
            Height = 4,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Women"), DisplayOrder = 4 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Blue", 1),
                Spec("Material", "Cotton", 2),
                Spec("Fit", "Relaxed", 3),
                Spec("Season", "Spring", 4)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("XS", 0), ("S", 0), ("M", 0), ("L", 0), ("XL", 0)),
                ColorAttribute(2,
                    ("Navy", "#2B3A55", 0), ("Camel", "#C19A6B", 0))
            }
        };
        await AddProductPictures(trousers, "product_loom_wide_leg_trousers_1.jpg");
        await _productRepository.InsertAsync(trousers);
        products.Add(trousers);

        #endregion

        #region Men

        // 34 - Fieldstone Waxed Cotton Jacket: Size x Color combinations, per-value pictures, backorders.
        var jacket = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Fieldstone Waxed Cotton Jacket",
            ShortDescription = "A waxed cotton field jacket built for city commutes and muddy trailheads alike.",
            FullDescription = """
                <p>Waxed cotton sheds rain and wind while it slowly breaks in around you. This field
                jacket keeps the classic four-pocket cut, a corduroy collar, and a quilted lining warm
                enough for the shoulder seasons on either side of winter.</p>
                <ul>
                    <li>Waxed cotton shell, corduroy collar</li>
                    <li>Quilted lining, four bellows pockets</li>
                    <li>Re-waxable for years of use</li>
                </ul>
                """,
            MetaTitle = "Fieldstone Waxed Cotton Jacket",
            MetaDescription = "Waxed cotton field jacket in Olive or Navy, sizes S to XXL, quilted lining.",
            Sku = "FA-005",
            Price = 229.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            BackorderModeId = BackorderMode.AllowQtyBelowZero,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.9,
            Length = 40,
            Width = 30,
            Height = 6,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Men"), DisplayOrder = 1 } },
            BrandId = BrandId("Fieldstone Apparel"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Green", 1),
                Spec("Material", "Waxed cotton", 2),
                Spec("Fit", "Regular", 3),
                Spec("Season", "Spring", 4),
                Spec("Season", "Autumn", 5)
            }
        };
        await AddProductPictures(jacket,
            "product_fieldstone_waxed_cotton_jacket_1.jpg",
            "product_fieldstone_waxed_cotton_jacket_2.jpg",
            "product_fieldstone_waxed_cotton_jacket_3.jpg");
        var jacketPictures = jacket.ProductPictures.OrderBy(p => p.DisplayOrder).ToList();

        var jacketSize = SizeAttribute(1,
            ("S", 0), ("M", 0), ("L", 0), ("XL", 0), ("XXL", 0));
        jacketSize.Combination = true;
        var jacketColor = ColorAttributeWithPictures(2, combination: true,
            ("Olive", "#5B5F3D", jacketPictures[0].PictureId),
            ("Navy", "#22304A", jacketPictures[1].PictureId));
        jacket.ProductAttributeMappings.Add(jacketSize);
        jacket.ProductAttributeMappings.Add(jacketColor);

        AddSizeColorCombinations(jacket, jacketSize, jacketColor,
            ("S", "Olive", 7, false), ("M", "Olive", 8, false), ("L", "Olive", 6, false),
            ("XL", "Olive", 4, false), ("XXL", "Olive", 2, false),
            ("S", "Navy", 5, false), ("M", "Navy", 6, false), ("L", "Navy", 6, false),
            ("XL", "Navy", 3, false), ("XXL", "Navy", 0, true));

        await _productRepository.InsertAsync(jacket);
        products.Add(jacket);

        // 35 - Fieldstone Oxford Shirt
        var oxfordShirt = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Fieldstone Oxford Shirt",
            ShortDescription = "A slim-fit oxford shirt in brushed cotton, sharp enough for the office, soft enough for the weekend.",
            FullDescription = """
                <p>Woven from brushed oxford cotton, this shirt keeps its shape through a full day and
                softens with every wash. A button-down collar and single chest pocket keep it easy to
                dress up or down.</p>
                <ul>
                    <li>Brushed oxford cotton, slim fit</li>
                    <li>Button-down collar, single chest pocket</li>
                    <li>Machine washable</li>
                </ul>
                """,
            MetaTitle = "Fieldstone Oxford Shirt",
            MetaDescription = "Slim-fit oxford cotton shirt in White or Light Blue, sizes S to XXL.",
            Sku = "FA-006",
            Price = 79.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 90,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.25,
            Length = 30,
            Width = 25,
            Height = 3,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Men"), DisplayOrder = 2 } },
            BrandId = BrandId("Fieldstone Apparel"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Blue", 1),
                Spec("Material", "Cotton", 2),
                Spec("Fit", "Slim", 3),
                Spec("Season", "All season", 4)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("S", 0), ("M", 0), ("L", 0), ("XL", 0), ("XXL", 0)),
                ColorAttribute(2,
                    ("White", "#F5F4F0", 0), ("Light Blue", "#A9C4DE", 0))
            }
        };
        await AddProductPictures(oxfordShirt, "product_fieldstone_oxford_shirt_1.jpg");
        await _productRepository.InsertAsync(oxfordShirt);
        products.Add(oxfordShirt);

        // 36 - Fieldstone Selvedge Denim Jeans (size is waist in inches, no color attribute)
        var jeans = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Fieldstone Selvedge Denim Jeans",
            ShortDescription = "Raw selvedge denim jeans that fade with wear, cut slim through the leg.",
            FullDescription = """
                <p>Woven on old shuttle looms, this raw selvedge denim is left unwashed so it fades
                exactly where you wear it. A slim, straight leg and a slightly higher rise keep it
                timeless rather than trendy.</p>
                <ul>
                    <li>Raw selvedge denim, slim straight leg</li>
                    <li>Copper rivets, chain-stitched hem</li>
                    <li>Sized by waist in inches</li>
                </ul>
                """,
            MetaTitle = "Fieldstone Selvedge Denim Jeans",
            MetaDescription = "Raw selvedge denim jeans, slim fit, waist sizes 30 to 36 inches.",
            Sku = "FA-007",
            Price = 129.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 80,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.6,
            Length = 35,
            Width = 25,
            Height = 5,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Men"), DisplayOrder = 3 } },
            BrandId = BrandId("Fieldstone Apparel"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Blue", 1),
                Spec("Material", "Denim", 2),
                Spec("Fit", "Slim", 3)
            },
            ProductAttributeMappings = {
                SizeAttribute(1,
                    ("30", 0), ("32", 0), ("34", 0), ("36", 0))
            }
        };
        await AddProductPictures(jeans, "product_fieldstone_selvedge_denim_jeans_1.jpg");
        await _productRepository.InsertAsync(jeans);
        products.Add(jeans);

        // 37 - Fieldstone Merino Beanie (color only, no size)
        var beanie = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Fieldstone Merino Beanie",
            ShortDescription = "A ribbed merino beanie for the coldest months, one size fits most.",
            FullDescription = """
                <p>Fine-gauge merino wool, ribbed the whole way through, keeps this beanie warm
                without the itch. A close, unlined fit works under a hood just as well as on its
                own.</p>
                <ul>
                    <li>100% merino wool, ribbed knit</li>
                    <li>Unlined, close fit</li>
                    <li>One size fits most</li>
                </ul>
                """,
            MetaTitle = "Fieldstone Merino Beanie",
            MetaDescription = "Ribbed merino wool beanie in Charcoal, Rust, or Navy.",
            Sku = "FA-008",
            Price = 29.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 130,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.1,
            Length = 20,
            Width = 15,
            Height = 8,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Men"), DisplayOrder = 4 } },
            BrandId = BrandId("Fieldstone Apparel"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Grey", 1),
                Spec("Material", "Merino wool", 2),
                Spec("Season", "Winter", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Charcoal", "#3B3B3D", 0), ("Rust", "#B4552F", 0), ("Navy", "#22304A", 0))
            }
        };
        await AddProductPictures(beanie, "product_fieldstone_merino_beanie_1.jpg");
        await _productRepository.InsertAsync(beanie);
        products.Add(beanie);

        #endregion

        #region Bags & Accessories

        // 38 - Ember Leather Weekender Bag: Color + Engraving (TextBox)
        var weekenderBag = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Ember Leather Weekender Bag",
            ShortDescription = "A full-grain leather weekender roomy enough for two nights away.",
            FullDescription = """
                <p>Cut from full-grain leather and built on a reinforced base, this weekender is made
                to be thrown in the trunk and pulled out again for decades. A wide zip opening, an
                internal laptop sleeve, and a detachable strap round it out.</p>
                <ul>
                    <li>Full-grain leather, brass hardware</li>
                    <li>Internal laptop sleeve, wide zip opening</li>
                    <li>Optional monogram engraving</li>
                </ul>
                """,
            MetaTitle = "Ember Leather Weekender Bag",
            MetaDescription = "Full-grain leather weekender bag in Cognac or Dark Brown, with optional monogram engraving.",
            Sku = "FA-009",
            Price = 289.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 40,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            ShowOnHomePage = true,
            IsFreeShipping = true,
            Weight = 1.4,
            Length = 55,
            Width = 28,
            Height = 28,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Bags & Accessories"), DisplayOrder = 1 } },
            BrandId = BrandId("Ember & Oak"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Brown", 1),
                Spec("Material", "Leather", 2),
                Spec("Capacity", "Over 30 L", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Cognac", "#A0522D", 0), ("Dark Brown", "#3B2A20", 0)),
                // Engraving is a TextBox attribute: the control type has no selectable values, so
                // the manifest's +15.00 cannot be modeled as a ProductAttributeValue price
                // adjustment (ProductAttributeMapping has no price field, and pricing only reads
                // values for control types that ShouldHaveValues()). Captured as an optional,
                // length-limited custom text field instead, the same way HomeLiving does it for the
                // Walnut Serving Board; the price note lives in the description only.
                new ProductAttributeMapping {
                    ProductAttributeId = ProductAttributeId("Engraving"),
                    AttributeControlTypeId = AttributeControlType.TextBox,
                    IsRequired = false,
                    ValidationMaxLength = 30,
                    DisplayOrder = 2
                }
            }
        };
        await AddProductPictures(weekenderBag,
            "product_ember_leather_weekender_bag_1.jpg",
            "product_ember_leather_weekender_bag_2.jpg");
        await _productRepository.InsertAsync(weekenderBag);
        products.Add(weekenderBag);

        // 39 - Loom Canvas Tote Bag
        var toteBag = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Loom Canvas Tote Bag",
            ShortDescription = "A sturdy organic cotton canvas tote for market runs and daily carry.",
            FullDescription = """
                <p>Heavy organic cotton canvas, stitched with reinforced handles and a flat base that
                stands up on its own. Big enough for groceries, sturdy enough for books.</p>
                <ul>
                    <li>Heavyweight organic cotton canvas</li>
                    <li>Reinforced handles, flat standing base</li>
                    <li>Interior slip pocket</li>
                </ul>
                """,
            MetaTitle = "Loom Canvas Tote Bag",
            MetaDescription = "Organic cotton canvas tote bag in Natural or Black, everyday carry.",
            Sku = "FA-010",
            Price = 39.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 160,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.3,
            Length = 40,
            Width = 35,
            Height = 10,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Bags & Accessories"), DisplayOrder = 2 } },
            BrandId = BrandId("Loom & Thread"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Beige", 1),
                Spec("Material", "Organic cotton", 2),
                Spec("Capacity", "10 to 30 L", 3)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Natural", "#E7DFCF", 0), ("Black", "#1E1E1E", 0))
            }
        };
        await AddProductPictures(toteBag, "product_loom_canvas_tote_bag_1.jpg");
        await _productRepository.InsertAsync(toteBag);
        products.Add(toteBag);

        // 40 - Fieldstone Leather Card Wallet
        var cardWallet = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Fieldstone Leather Card Wallet",
            ShortDescription = "A slim leather card wallet that thins your pocket instead of filling it.",
            FullDescription = """
                <p>Cut from a single piece of vegetable-tanned leather and folded around four card
                slots and a cash pocket, this wallet is built to slim down over years of carry, not
                fall apart.</p>
                <ul>
                    <li>Vegetable-tanned leather, hand-stitched</li>
                    <li>Four card slots, one cash pocket</li>
                    <li>Ages and darkens with use</li>
                </ul>
                """,
            MetaTitle = "Fieldstone Leather Card Wallet",
            MetaDescription = "Slim leather card wallet in Tan or Black, four card slots and a cash pocket.",
            Sku = "FA-011",
            Price = 45.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 180,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 0.08,
            Length = 11,
            Width = 9,
            Height = 2,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Bags & Accessories"), DisplayOrder = 3 } },
            BrandId = BrandId("Fieldstone Apparel"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Brown", 1),
                Spec("Material", "Leather", 2)
            },
            ProductAttributeMappings = {
                ColorAttribute(1,
                    ("Tan", "#C8A165", 0), ("Black", "#1E1E1E", 0))
            }
        };
        await AddProductPictures(cardWallet, "product_fieldstone_leather_card_wallet_1.jpg");
        await _productRepository.InsertAsync(cardWallet);
        products.Add(cardWallet);

        // 41 - Trailforge Roll Top Backpack: Capacity (RadioList, price adjustment) + Color
        var backpack = new Product {
            ProductTypeId = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = "Trailforge Roll Top Backpack",
            ShortDescription = "A weatherproof roll top backpack for the daily commute, with room to spare.",
            FullDescription = """
                <p>Recycled ripstop nylon and a roll top closure keep the weather out on the ride in.
                A padded 16-inch laptop sleeve, a quick-access front pocket, and reflective trim make
                it as practical after dark as it is at nine in the morning.</p>
                <ul>
                    <li>Recycled ripstop nylon, roll top closure</li>
                    <li>Padded 16-inch laptop sleeve</li>
                    <li>Reflective trim, quick-access front pocket</li>
                </ul>
                """,
            MetaTitle = "Trailforge Roll Top Backpack",
            MetaDescription = "Weatherproof roll top commuter backpack, 20 L or 28 L, in Black or Olive.",
            Sku = "FA-012",
            Price = 149.00,
            TaxCategoryId = TaxCategoryId("Apparel"),
            DeliveryDateId = ctx.DeliveryDateId,
            ProductLayoutId = ctx.SimpleLayoutId,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 75,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = true,
            AllowCustomerReviews = true,
            Weight = 1.1,
            Length = 48,
            Width = 30,
            Height = 15,
            ProductCategories = { new ProductCategory { CategoryId = CategoryId("Bags & Accessories"), DisplayOrder = 4 } },
            BrandId = BrandId("Trailforge"),
            ProductSpecificationAttributes = {
                Spec("Color family", "Black", 1),
                Spec("Material", "Recycled nylon", 2),
                Spec("Capacity", "10 to 30 L", 3)
            },
            ProductAttributeMappings = {
                CapacityAttribute(1,
                    ("20 L", 0), ("28 L", 20.00)),
                ColorAttribute(2,
                    ("Black", "#1E1E1E", 0), ("Olive", "#6B6E42", 0))
            }
        };
        await AddProductPictures(backpack,
            "product_trailforge_roll_top_backpack_1.jpg",
            "product_trailforge_roll_top_backpack_2.jpg");
        await _productRepository.InsertAsync(backpack);
        products.Add(backpack);

        #endregion

        return products;
    }

    // Builds a Color attribute mapping whose values carry a picture (one of the product's own
    // pictures), for products where the manifest calls out per-color-value pictures.
    private ProductAttributeMapping ColorAttributeWithPictures(int displayOrder, bool combination,
        params (string Name, string Hex, string PictureId)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId("Color"),
            AttributeControlTypeId = AttributeControlType.ColorSquares,
            IsRequired = true,
            DisplayOrder = displayOrder,
            Combination = combination
        };
        for (var i = 0; i < values.Length; i++)
            mapping.ProductAttributeValues.Add(new ProductAttributeValue {
                Name = values[i].Name,
                ColorSquaresRgb = values[i].Hex,
                PictureId = values[i].PictureId,
                DisplayOrder = i,
                IsPreSelected = i == 0
            });
        return mapping;
    }

    private ProductAttributeMapping CapacityAttribute(int displayOrder, params (string Name, double Adjustment)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId("Capacity"),
            AttributeControlTypeId = AttributeControlType.RadioList,
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

    // Adds one ProductAttributeCombination per (size, color) pair with its own stock, and sets the
    // product's own StockQuantity to their sum (ManageStockByAttributes tracks stock per combination,
    // but the parent Product.StockQuantity is still read in a few places as the overall total).
    private static void AddSizeColorCombinations(Product product, ProductAttributeMapping sizeAttribute,
        ProductAttributeMapping colorAttribute, params (string Size, string Color, int Stock, bool AllowBackorder)[] combinations)
    {
        foreach (var combination in combinations)
        {
            var sizeValue = sizeAttribute.ProductAttributeValues.Single(v => v.Name == combination.Size);
            var colorValue = colorAttribute.ProductAttributeValues.Single(v => v.Name == combination.Color);
            product.ProductAttributeCombinations.Add(new ProductAttributeCombination {
                Attributes = new List<CustomAttribute> {
                    new() { Key = sizeAttribute.Id, Value = sizeValue.Id },
                    new() { Key = colorAttribute.Id, Value = colorValue.Id }
                },
                StockQuantity = combination.Stock,
                AllowOutOfStockOrders = combination.AllowBackorder
            });
        }
        product.StockQuantity = combinations.Sum(c => c.Stock);
    }
}
