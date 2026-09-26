using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Computers department products: filterable laptops and configurable desktops. Inserts products
    // (with pictures, categories, brand, specs, attributes) and returns them. Does not create slugs,
    // tags, relations, or reviews.
    protected virtual async Task<List<Product>> InstallProductsComputers(SampleProductContext ctx)
    {
        var products = new List<Product>();

        Product NewComputer(string sku, string name, double price, string subcategory, int displayOrder,
            int stock, double weight, string shortDescription, string fullDescription, string metaDescription) =>
            new() {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Sku = sku,
                Name = name,
                ShortDescription = shortDescription,
                FullDescription = fullDescription,
                MetaTitle = name,
                MetaDescription = metaDescription,
                Price = price,
                TaxCategoryId = TaxCategoryId("Electronics & Software"),
                DeliveryDateId = ctx.DeliveryDateId,
                ProductLayoutId = ctx.SimpleLayoutId,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = stock,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10,
                Published = true,
                AllowCustomerReviews = true,
                IsShipEnabled = true,
                Weight = weight,
                Length = 45,
                Width = 35,
                Height = 12,
                BrandId = BrandId("Vertex Computing"),
                ProductCategories = { new ProductCategory { CategoryId = CategoryId(subcategory), DisplayOrder = displayOrder } }
            };

        async Task Save(Product product, params string[] pictures)
        {
            await AddProductPictures(product, pictures);
            await _productRepository.InsertAsync(product);
            products.Add(product);
        }

        void AddSpecs(Product product, params ProductSpecificationAttribute[] specs)
        {
            foreach (var spec in specs)
                product.ProductSpecificationAttributes.Add(spec);
        }

        void AddOptions(Product product, params ProductAttributeMapping[] mappings)
        {
            foreach (var mapping in mappings)
                product.ProductAttributeMappings.Add(mapping);
        }

        #region Laptops

        var air13 = NewComputer("CO-001", "Vertex Air 13", 899.00, "Laptops", 1, 60, 1.15,
            "A 1.15 kg 13.3-inch laptop with an all-day battery and a silent, fanless design.",
            """
            <p>Vertex Air 13 is the laptop you forget is in your bag. The fanless chassis stays silent in a
            library or a meeting, the 13.3-inch display is bright enough for a sunny window seat, and the
            battery carries a full working day with room to spare.</p>
            <ul>
                <li>Fanless design: completely silent under everyday work</li>
                <li>Up to 18 hours of battery life</li>
                <li>Two USB-C ports with charging and display output</li>
                <li>Backlit keyboard and a large glass touchpad</li>
            </ul>
            <table class="table table-bordered">
                <tbody>
                    <tr><th>Processor</th><td>Intel Core i5</td></tr>
                    <tr><th>Memory</th><td>16 GB</td></tr>
                    <tr><th>Storage</th><td>512 GB SSD</td></tr>
                    <tr><th>Display</th><td>13.3 in, 2560 x 1600</td></tr>
                    <tr><th>Weight</th><td>1.15 kg</td></tr>
                </tbody>
            </table>
            """,
            "Vertex Air 13: a silent, fanless 13.3-inch laptop with 16 GB of memory and an all-day battery.");
        air13.MarkAsNew = true;
        AddSpecs(air13,
            Spec("Processor", "Intel Core i5", 1),
            Spec("Memory", "16 GB", 2),
            Spec("Storage", "512 GB SSD", 3),
            Spec("Screen size", "13.3 in", 4),
            Spec("Graphics", "Integrated graphics", 5),
            Spec("Color family", "Grey", 6),
            Spec("Battery life", "12 to 24 hours", 7)
        );
        AddOptions(air13,
            ColorAttribute(1, ("Graphite", "#4a4a4d", 0), ("Silver", "#d9d9dc", 0))
        );
        await Save(air13, "product_vertex_air_13_1.jpg", "product_vertex_air_13_2.jpg");

        var book14 = NewComputer("CO-002", "Vertex Book 14", 1099.00, "Laptops", 2, 45, 1.4,
            "A 14-inch everyday laptop with an AMD Ryzen 7, 1 TB of storage and a memory upgrade option.",
            """
            <p>Vertex Book 14 is the everyday workhorse: a roomy 14-inch screen, a keyboard you can type on
            for hours, and enough storage for years of projects. Start with 16 GB of memory, or double it
            at checkout if you work with big spreadsheets and many browser tabs.</p>
            <ul>
                <li>AMD Ryzen 7 processor for smooth multitasking</li>
                <li>1 TB SSD for documents, photos and projects</li>
                <li>1080p webcam with a physical privacy shutter</li>
                <li>HDMI, two USB-C and one USB-A port</li>
            </ul>
            """,
            "Vertex Book 14: a 14-inch AMD Ryzen 7 laptop with 1 TB of storage and an optional memory upgrade.");
        book14.BestSeller = true;
        AddSpecs(book14,
            Spec("Processor", "AMD Ryzen 7", 1),
            Spec("Memory", "16 GB", 2),
            Spec("Storage", "1 TB SSD", 3),
            Spec("Screen size", "14 in", 4),
            Spec("Graphics", "Integrated graphics", 5),
            Spec("Color family", "Grey", 6)
        );
        AddOptions(book14,
            ComputerOptionAttribute("Memory", AttributeControlType.RadioList, 1, true,
                ("16 GB", 0), ("32 GB", 150))
        );
        await Save(book14, "product_vertex_book_14_1.jpg");

        var flip14 = NewComputer("CO-003", "Vertex Flip 14", 1199.00, "Laptops", 3, 30, 1.5,
            "A 2-in-1 14-inch touchscreen laptop that folds into a tablet, with an Intel Core i7.",
            """
            <p>Fold it back and Vertex Flip 14 becomes a tablet for sketching, reading or reviewing a
            presentation on the sofa. Fold it forward again and it is a full laptop with an Intel Core i7
            and a proper keyboard. The included pen clicks magnetically to the side.</p>
            <ul>
                <li>360-degree hinge: laptop, tent, stand and tablet modes</li>
                <li>14-inch touchscreen with pen support, pen included</li>
                <li>Intel Core i7 with 16 GB of memory</li>
            </ul>
            """,
            "Vertex Flip 14: a 2-in-1 touchscreen laptop with an Intel Core i7 and an included pen.");
        AddSpecs(flip14,
            Spec("Processor", "Intel Core i7", 1),
            Spec("Memory", "16 GB", 2),
            Spec("Storage", "512 GB SSD", 3),
            Spec("Screen size", "14 in", 4),
            Spec("Graphics", "Integrated graphics", 5),
            Spec("Color family", "Blue", 6)
        );
        await Save(flip14, "product_vertex_flip_14_1.jpg");

        var pro16 = NewComputer("CO-004", "Vertex Pro 16", 1899.00, "Laptops", 4, 25, 2.1,
            "A 16-inch creator laptop with an Intel Core i9, 32 GB of memory and GeForce RTX 4060 graphics.",
            """
            <p>Vertex Pro 16 is built for people who edit video, render 3D or compile large projects on the
            move. The Intel Core i9 and GeForce RTX 4060 are cooled by a dual-fan system that stays quiet
            under load, and the colour-accurate 16-inch display is calibrated before it leaves us.</p>
            <ul>
                <li>Intel Core i9 with 32 GB of memory</li>
                <li>NVIDIA GeForce RTX 4060 graphics</li>
                <li>16-inch 2560 x 1600 display, factory calibrated</li>
                <li>SD card reader, HDMI 2.1 and Thunderbolt-class USB-C</li>
            </ul>
            <table class="table table-bordered">
                <tbody>
                    <tr><th>Processor</th><td>Intel Core i9</td></tr>
                    <tr><th>Memory</th><td>32 GB</td></tr>
                    <tr><th>Storage</th><td>1 TB SSD</td></tr>
                    <tr><th>Graphics</th><td>NVIDIA GeForce RTX 4060</td></tr>
                    <tr><th>Display</th><td>16 in, 2560 x 1600, 165 Hz</td></tr>
                </tbody>
            </table>
            """,
            "Vertex Pro 16: a 16-inch Intel Core i9 creator laptop with 32 GB of memory and RTX 4060 graphics.");
        pro16.OldPrice = 2099.00;
        pro16.ShowOnHomePage = true;
        AddSpecs(pro16,
            Spec("Processor", "Intel Core i9", 1),
            Spec("Memory", "32 GB", 2),
            Spec("Storage", "1 TB SSD", 3),
            Spec("Screen size", "16 in", 4),
            Spec("Graphics", "NVIDIA GeForce RTX 4060", 5),
            Spec("Color family", "Black", 6)
        );
        AddOptions(pro16,
            ComputerOptionAttribute("Storage", AttributeControlType.RadioList, 1, true,
                ("1 TB SSD", 0), ("2 TB SSD", 180))
        );
        await Save(pro16, "product_vertex_pro_16_1.jpg", "product_vertex_pro_16_2.jpg");

        var gaming15 = NewComputer("CO-005", "Vertex Gaming 15", 1499.00, "Laptops", 5, 20, 2.3,
            "A 15.6-inch gaming laptop with an AMD Ryzen 9, GeForce RTX 4070 graphics and a 240 Hz display.",
            """
            <p>Vertex Gaming 15 pairs an AMD Ryzen 9 with GeForce RTX 4070 graphics and a 240 Hz display, so
            fast games stay smooth. The vapour-chamber cooling keeps the keyboard cool, and the per-key
            lighting can be dimmed to nothing when you take it to the office.</p>
            <ul>
                <li>AMD Ryzen 9 with 32 GB of memory</li>
                <li>NVIDIA GeForce RTX 4070 graphics</li>
                <li>15.6-inch 240 Hz display</li>
                <li>Vapour-chamber cooling with a quiet mode</li>
            </ul>
            """,
            "Vertex Gaming 15: a 15.6-inch AMD Ryzen 9 gaming laptop with RTX 4070 graphics and a 240 Hz display.");
        AddSpecs(gaming15,
            Spec("Processor", "AMD Ryzen 9", 1),
            Spec("Memory", "32 GB", 2),
            Spec("Storage", "1 TB SSD", 3),
            Spec("Screen size", "15.6 in", 4),
            Spec("Graphics", "NVIDIA GeForce RTX 4070", 5),
            Spec("Color family", "Black", 6)
        );
        await Save(gaming15, "product_vertex_gaming_15_1.jpg");

        #endregion

        #region Desktops

        var buildYourOwn = NewComputer("CO-006", "Vertex Build Your Own Desktop", 749.00, "Desktops", 1, 200, 8.5,
            "Configure your own desktop: choose the processor, memory, storage, graphics, operating system and software.",
            """
            <p>Start from a quiet, well-cooled case and build the desktop you actually need. Choose each
            component below and the price updates as you go. Every machine is assembled, tested and burned
            in for 24 hours before it ships.</p>
            <ul>
                <li>Processors from Intel Core i5 to AMD Ryzen 9</li>
                <li>16 GB to 64 GB of memory</li>
                <li>512 GB to 4 TB of SSD storage</li>
                <li>Integrated graphics, or GeForce RTX 4060 or 4070</li>
                <li>Windows 11 Home or Pro preinstalled, or no operating system</li>
            </ul>
            """,
            "Build your own Vertex desktop: choose processor, memory, storage, graphics, operating system and software.");
        buildYourOwn.MarkAsNew = true;
        buildYourOwn.ShowOnHomePage = true;
        AddSpecs(buildYourOwn,
            Spec("Color family", "Black", 1)
        );
        AddOptions(buildYourOwn,
            ComputerOptionAttribute("Processor", AttributeControlType.DropdownList, 1, true,
                ("Intel Core i5", 0), ("Intel Core i7", 150), ("Intel Core i9", 380),
                ("AMD Ryzen 7", 120), ("AMD Ryzen 9", 340)),
            ComputerOptionAttribute("Memory", AttributeControlType.DropdownList, 2, true,
                ("16 GB", 0), ("32 GB", 90), ("64 GB", 240)),
            ComputerOptionAttribute("Storage", AttributeControlType.RadioList, 3, true,
                ("512 GB SSD", 0), ("1 TB SSD", 60), ("2 TB SSD", 160), ("4 TB SSD", 360)),
            ComputerOptionAttribute("Graphics", AttributeControlType.DropdownList, 4, true,
                ("Integrated graphics", 0), ("NVIDIA GeForce RTX 4060", 320), ("NVIDIA GeForce RTX 4070", 560)),
            ComputerOptionAttribute("Operating system", AttributeControlType.RadioList, 5, true,
                ("Windows 11 Home", 0), ("Windows 11 Pro", 90), ("No operating system", -80)),
            ComputerOptionAttribute("Software", AttributeControlType.Checkboxes, 6, false,
                ("Productivity suite, 1-year licence", 99), ("Security suite, 1-year licence", 39),
                ("Photo editor, lifetime licence", 79))
        );
        await Save(buildYourOwn, "product_vertex_build_your_own_desktop_1.jpg",
            "product_vertex_build_your_own_desktop_2.jpg");

        var workstation = NewComputer("CO-007", "Vertex Creator Workstation", 1899.00, "Desktops", 2, 40, 11.0,
            "A creator workstation with an Intel Core i9, 32 GB of memory and GeForce RTX graphics, configurable.",
            """
            <p>For editing 8K footage, rendering scenes or training small models, the Creator Workstation
            starts where most desktops stop: an Intel Core i9, 32 GB of memory and a GeForce RTX 4060. Push
            it further with more memory, more storage or RTX 4070 graphics.</p>
            <ul>
                <li>Tool-free side panel and room for four drives</li>
                <li>Three 140 mm fans tuned for quiet operation</li>
                <li>Front USB-C, SD card reader and headphone jack</li>
            </ul>
            """,
            "Vertex Creator Workstation: an Intel Core i9 desktop with 32 GB of memory and RTX graphics, configurable.");
        AddSpecs(workstation,
            Spec("Processor", "Intel Core i9", 1),
            Spec("Memory", "32 GB", 2),
            Spec("Storage", "1 TB SSD", 3),
            Spec("Graphics", "NVIDIA GeForce RTX 4060", 4),
            Spec("Color family", "White", 5)
        );
        AddOptions(workstation,
            ComputerOptionAttribute("Processor", AttributeControlType.DropdownList, 1, true,
                ("Intel Core i9", 0), ("AMD Ryzen 9", 40)),
            ComputerOptionAttribute("Memory", AttributeControlType.DropdownList, 2, true,
                ("32 GB", 0), ("64 GB", 240)),
            ComputerOptionAttribute("Storage", AttributeControlType.RadioList, 3, true,
                ("1 TB SSD", 0), ("2 TB SSD", 100), ("4 TB SSD", 300)),
            ComputerOptionAttribute("Graphics", AttributeControlType.DropdownList, 4, true,
                ("NVIDIA GeForce RTX 4060", 0), ("NVIDIA GeForce RTX 4070", 240)),
            ComputerOptionAttribute("Operating system", AttributeControlType.RadioList, 5, true,
                ("Windows 11 Home", 0), ("Windows 11 Pro", 90))
        );
        await Save(workstation, "product_vertex_creator_workstation_1.jpg");

        var miniPc = NewComputer("CO-008", "Vertex Mini PC", 549.00, "Desktops", 3, 80, 1.2,
            "A palm-sized desktop with an Intel Core i5 that mounts behind a monitor, with memory and storage options.",
            """
            <p>Vertex Mini PC fits in the palm of your hand and mounts behind a monitor with the included
            bracket, leaving the desk clear. It is quiet enough for a bedroom and powerful enough for office
            work, streaming and light photo editing.</p>
            <ul>
                <li>Intel Core i5 with integrated graphics</li>
                <li>Two display outputs, Wi-Fi and gigabit Ethernet</li>
                <li>Monitor mounting bracket included</li>
            </ul>
            """,
            "Vertex Mini PC: a palm-sized Intel Core i5 desktop with memory and storage options.");
        AddSpecs(miniPc,
            Spec("Processor", "Intel Core i5", 1),
            Spec("Memory", "16 GB", 2),
            Spec("Storage", "512 GB SSD", 3),
            Spec("Graphics", "Integrated graphics", 4),
            Spec("Color family", "Grey", 5)
        );
        AddOptions(miniPc,
            ComputerOptionAttribute("Memory", AttributeControlType.RadioList, 1, true,
                ("16 GB", 0), ("32 GB", 90)),
            ComputerOptionAttribute("Storage", AttributeControlType.RadioList, 2, true,
                ("512 GB SSD", 0), ("1 TB SSD", 60), ("2 TB SSD", 160))
        );
        await Save(miniPc, "product_vertex_mini_pc_1.jpg");

        #endregion

        return products;
    }

    // A configurator option (processor, memory, storage, ...): the first value is preselected and each
    // value carries its price adjustment, so the product price updates as the customer configures it.
    private ProductAttributeMapping ComputerOptionAttribute(string attribute, AttributeControlType controlType,
        int displayOrder, bool required, params (string Name, double Adjustment)[] values)
    {
        var mapping = new ProductAttributeMapping {
            ProductAttributeId = ProductAttributeId(attribute),
            AttributeControlTypeId = controlType,
            IsRequired = required,
            DisplayOrder = displayOrder
        };
        for (var i = 0; i < values.Length; i++)
            mapping.ProductAttributeValues.Add(new ProductAttributeValue {
                Name = values[i].Name,
                PriceAdjustment = values[i].Adjustment,
                IsPreSelected = required && i == 0,
                DisplayOrder = i
            });
        return mapping;
    }
}
