using Grand.Business.Common.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallProducts(string defaultUserEmail)
        {
            var pictureService = _serviceProvider.GetRequiredService<IPictureService>();
            var downloadService = _serviceProvider.GetRequiredService<IDownloadService>();

            var productLayoutSimple = _productLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Simple product");
            if (productLayoutSimple == null)
                throw new Exception("Simple product layout could not be loaded");
            var productLayoutGrouped = _productLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Grouped product (with variants)");
            if (productLayoutGrouped == null)
                throw new Exception("Simple product layout could not be loaded");

            //delivery date
            var deliveryDate = _deliveryDateRepository.Table.FirstOrDefault();
            if (deliveryDate == null)
                throw new Exception("No default deliveryDate could be loaded");

            //default customer/user
            var defaultCustomer = _customerRepository.Table.FirstOrDefault(x => x.Email == defaultUserEmail);
            if (defaultCustomer == null)
                throw new Exception("Cannot load default customer");

            //pictures
            var sampleImagesPath = GetSamplesPath();

            //downloads
            var sampleDownloadsPath = GetSamplesPath();

            //default store
            var defaultStore = _storeRepository.Table.FirstOrDefault();
            if (defaultStore == null)
                throw new Exception("No default store could be loaded");

            //products
            var allProducts = new List<Product>();

            #region Computers


            var productBuildComputer = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Build your own computer",
                ShortDescription = "Build it",
                FullDescription = "<p>Fight back against cluttered workspaces with the stylish DELL Inspiron desktop PC, featuring powerful computing resources and a stunning 20.1-inch widescreen display with stunning XBRITE-HiColor LCD technology. The black IBM zBC12 has a built-in microphone and MOTION EYE camera with face-tracking technology that allows for easy communication with friends and family. And it has a built-in DVD burner and Sony's Movie Store software so you can create a digital entertainment library for personal viewing at your convenience. Easy to setup and even easier to use, this JS-series All-in-One includes an elegantly designed keyboard and a USB mouse.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1200,
                IsShipEnabled = true,
                IsFreeShipping = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                ShowOnHomePage = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Processor").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "2.2 GHz Intel Pentium Dual-Core E2200",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "2.5 GHz Intel Pentium Dual-Core E2200",
                                IsPreSelected = true,
                                PriceAdjustment = 15,
                                DisplayOrder = 2,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "RAM").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "2 GB",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "4GB",
                                PriceAdjustment = 20,
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "8GB",
                                PriceAdjustment = 60,
                                DisplayOrder = 3,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "HDD").Id,
                        AttributeControlTypeId = AttributeControlType.RadioList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "320 GB",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "400 GB",
                                PriceAdjustment = 100,
                                DisplayOrder = 2,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "OS").Id,
                        AttributeControlTypeId = AttributeControlType.RadioList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Vista Home",
                                PriceAdjustment = 50,
                                IsPreSelected = true,
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Vista Premium",
                                PriceAdjustment = 60,
                                DisplayOrder = 2,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Software").Id,
                        AttributeControlTypeId = AttributeControlType.Checkboxes,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Microsoft Office",
                                PriceAdjustment = 50,
                                IsPreSelected = true,
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Acrobat Reader",
                                PriceAdjustment = 10,
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Total Commander",
                                PriceAdjustment = 5,
                                DisplayOrder = 2,
                            }
                        }
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productBuildComputer);

            var picture1 = await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_desktop_1.png"), "image/png", pictureService.GetPictureSeName(productBuildComputer.Name), reference: Domain.Common.Reference.Product, objectId: productBuildComputer.Id);
            var picture2 = await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_desktop_2.png"), "image/png", pictureService.GetPictureSeName(productBuildComputer.Name), reference: Domain.Common.Reference.Product, objectId: productBuildComputer.Id);

            await _productRepository.InsertAsync(productBuildComputer);

            var productpicture1 = new ProductPicture
            {
                PictureId = picture1.Id,
                DisplayOrder = 1
            };
            var productpicture2 = new ProductPicture
            {
                PictureId = picture2.Id,
                DisplayOrder = 1
            };
            productBuildComputer.ProductPictures.Add(productpicture1);
            productBuildComputer.ProductPictures.Add(productpicture2);
            await _productRepository.UpdateAsync(productBuildComputer);

            var productSonyPS5Pad = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Playstation 5 Gamepad",
                ShortDescription = "The DualSense wireless controller for PS5 offers realistic touch effects2, adaptive 'Trigger' effects and a built-in microphone - all integrated into an iconic design.",
                FullDescription = "<p>The DualSense wireless controller for PS5 offers realistic touch effects2, adaptive 'Trigger' effects and a built-in microphone - all integrated into an iconic design.</p><p>Feel a physical reaction to your in-game actions thanks to dual actuators that replace traditional vibration motors. Such dynamic vibrations in your hands can simulate the tactile sensations of many things, from the world around you to the recoil of various weapons.</p><p>Enjoy intuitive motion controls for selected games thanks to the built-in accelerometer and gyroscope.</p><p>Chat with friends online using the built-in microphone or by plugging a headset into the 3.5mm jack. With the dedicated MUTE button you can disable voice recording.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 59,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                }

            };

            allProducts.Add(productSonyPS5Pad);
            productSonyPS5Pad.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_sony_ps5_pad_1.png"), "image/png", pictureService.GetPictureSeName(productSonyPS5Pad.Name), reference: Domain.Common.Reference.Product, objectId: productSonyPS5Pad.Id)).Id,
                DisplayOrder = 1,
            });
            productSonyPS5Pad.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_sony_ps5_pad_2.png"), "image/png", pictureService.GetPictureSeName(productSonyPS5Pad.Name), reference: Domain.Common.Reference.Product, objectId: productSonyPS5Pad.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productSonyPS5Pad);

            var productLenovoIdeaPadDual = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lenovo IdeaPad Dual 3i",
                ShortDescription = "Get dependable performance for work and play from the Dual 3i’s Intel® Pentium® processor, which gives you the ability to effortlessly multitask with multi-screen capabilities.",
                FullDescription = "<p>Get dependable performance for work and play from the Duet 3i’s Intel® Pentium® processor, which gives you the ability to effortlessly multitask with multi-screen capabilities, communicate easily with friends and family, and take all your favorite entertainment to go.</p><p>Work more freely on the elegant IdeaPad Duet 3i than on a regular laptop. The detachable Bluetooth keyboard allows you to easily switch between laptop and tablet modes, and the stand makes it easy to position your computer on any surface. The laptop runs on the power of an Intel® Pentium® processor, and the HD touchscreen, Dolby Audio ™ sound and optional LTE connectivity will keep you entertained anywhere.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 99,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Tablets").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLenovoIdeaPadDual);
            productLenovoIdeaPadDual.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_ideapad_dual_1.png"), "image/png", pictureService.GetPictureSeName(productLenovoIdeaPadDual.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoIdeaPadDual.Id)).Id,
                DisplayOrder = 1,
            });
            productLenovoIdeaPadDual.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_ideapad_dual_2.png"), "image/png", pictureService.GetPictureSeName(productLenovoIdeaPadDual.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoIdeaPadDual.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productLenovoIdeaPadDual);

            #endregion

            #region Notebooks

            var productMiNotebook = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Mi Notebook 14",
                ShortDescription = "Designed with utmost patience and craftsmanship, the Mi NoteBook 14 is so beautiful that you can't help but notice it. Weighing just 1.5kg, the sleek unibody metal chassis and an anodized sandblasted coating makes your device sturdy and gives it a svelte look.",
                FullDescription = "<p>Designed with utmost patience and craftsmanship, the Mi NoteBook 14 is so beautiful that you can't help but notice it. Weighing just 1.5kg, the sleek unibody metal chassis and an anodized sandblasted coating makes your device sturdy and gives it a slim look.</p><p>With the power-efficient NVIDIA® GeForce® MX250 graphics, now enjoy incredible HD photo and video editing, faster and smoother gaming. The powerful graphics engine and next-gen technologies gives you performance you desire.</p><p>The Mi Notebook 14 offers great clock speeds at 2666MHz, and thus makes you say goodbye to slow and insufficient memory. This helps you multi-task with your favorite editing/productivity tools and casual games.</p><p>Comes with a wider air intake area of 2530mm² and a large diameter fan which brings excellent cooling to the whole machine. This keeps your machine cool so that you can hold onto yours. The maximum sound of the fan is a mere 37 dB even when the system is loaded to its max.</p><p>Based on the scissor mechanism, the keys have ABS texture and a travel distance of 1.3mm which makes typing a lot more comfortable and low-profile on this device. The in-built dust protection layer is also an excellent addition.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1800,
                OldPrice = 2000,
                IsShipEnabled = true,
                IsFreeShipping = true,
                Weight = 3,
                Length = 3,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                ShowOnHomePage = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Notebooks").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                ProductSpecificationAttributes =
                {
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "13.0''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i5").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 3,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").SpecificationAttributeOptions.Single(sao => sao.Name == "4 GB").Id
                    }
                }
            };
            allProducts.Add(productMiNotebook);
            productMiNotebook.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_mi_notebook_1.png"), "image/png", pictureService.GetPictureSeName(productMiNotebook.Name), reference: Domain.Common.Reference.Product, objectId: productMiNotebook.Id)).Id,
                DisplayOrder = 1,
            });
            productMiNotebook.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_mi_notebook_2.png"), "image/png", pictureService.GetPictureSeName(productMiNotebook.Name), reference: Domain.Common.Reference.Product, objectId: productMiNotebook.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productMiNotebook);


            var productLenovoLegionY740 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lenovo Legion Y740",
                ShortDescription = "The 17.3 ”Y740 Legion is a gaming masterpiece. You will be immersed in the action thanks to best-in-class Corsair iCUE lighting and Dolby realistic image and surround sound technologies.",
                FullDescription = "<p>The 17.3 ”Y740 Legion is a gaming masterpiece. You will be immersed in the action thanks to best-in-class Corsair iCUE lighting and Dolby realistic image and surround sound technologies. Check out what the Lenovo Legion Y740 looks like in reality.Grab the photo below and drag it left or right to rotate the product, or use the navigation buttons.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1500,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Notebooks").Id,
                        DisplayOrder = 1,
                    }
                },
                ProductSpecificationAttributes =
                {
                     new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "15.6''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i7").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 3,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").SpecificationAttributeOptions.Single(sao => sao.Name == "16 GB").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 4,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").SpecificationAttributeOptions.Single(sao => sao.Name == "1 TB").Id
                    }
                }
            };
            allProducts.Add(productLenovoLegionY740);
            productLenovoLegionY740.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_legion_y740_1.png"), "image/png", pictureService.GetPictureSeName(productLenovoLegionY740.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoLegionY740.Id)).Id,
                DisplayOrder = 1,
            });
            productLenovoLegionY740.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_legion_y740_2.png"), "image/png", pictureService.GetPictureSeName(productLenovoLegionY740.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoLegionY740.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productLenovoLegionY740);


            var productPs5Camera = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Playstation 5 Camera",
                ShortDescription = "Use the new Sony HD Camera for PlayStation 5 to show other players your reactions during the game.",
                FullDescription = "<p>Use the new Sony HD Camera for PlayStation 5 to show other players your reactions during the game. Equipped with two lenses, the camera can record images in 1080p quality and works seamlessly with the PS5 background removal tools. They put you in the spotlight of viewers. In addition, the camera has been equipped with a stand that makes it easy to mount it above or below the TV.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 150,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                },
                ProductSpecificationAttributes =
                {
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "15.0''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i5").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 3,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").SpecificationAttributeOptions.Single(sao => sao.Name == "8 GB").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 4,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").SpecificationAttributeOptions.Single(sao => sao.Name == "128 GB").Id
                    }
                }
            };
            allProducts.Add(productPs5Camera);
            productPs5Camera.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_ps5_camera_1.png"), "image/png", pictureService.GetPictureSeName(productPs5Camera.Name), reference: Domain.Common.Reference.Product, objectId: productPs5Camera.Id)).Id,
                DisplayOrder = 1,
            });
            productPs5Camera.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_ps5_camera_2.png"), "image/png", pictureService.GetPictureSeName(productPs5Camera.Name), reference: Domain.Common.Reference.Product, objectId: productPs5Camera.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productPs5Camera);

            var productAcerNitro = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Acer Nitro 5",
                ShortDescription = "Experience a new dimension of gameplay with the Acer Nitro 5 laptop. Equipped with a powerful Intel Core i5 processor and GTX1050 graphics card, it is able to cope with even the most demanding tasks.",
                FullDescription = "<p>Experience a new dimension of gameplay with the Acer Nitro 5 laptop. Equipped with a powerful Intel Core i5 processor and GTX1050 graphics card, it is able to cope with even the most demanding tasks. Additionally, the matrix in IPS technology will ensure high quality of the displayed image, good color reproduction and wide viewing angles. The Acer Nitro 5 laptop is the perfect choice for both gaming and work. Dominate the virtual battlefield with the GeForce GTX 1050 graphics card, featuring the groundbreaking NVIDIA Pascal architecture. Excellent performance, innovative gaming technologies and support for DirectX 12 libraries will allow you to immerse yourself in phenomenal 4K resolution, enriched with HDR mode, or play in an amazingly realistic VR scenery. Every time without cuts or delays, every time on high details. Play the latest, most challenging games the way they deserve.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1350,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Notebooks").Id,
                        DisplayOrder = 1,
                    }
                },
                ProductSpecificationAttributes =
                {
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "13.3''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i5").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 3,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").SpecificationAttributeOptions.Single(sao => sao.Name == "4 GB").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 4,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").SpecificationAttributeOptions.Single(sao => sao.Name == "128 GB").Id
                    }
                }
            };
            allProducts.Add(productAcerNitro);
            productAcerNitro.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_nitro_1.png"), "image/png", pictureService.GetPictureSeName(productAcerNitro.Name), reference: Domain.Common.Reference.Product, objectId: productAcerNitro.Id)).Id,
                DisplayOrder = 1,
            });
            productAcerNitro.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_nitro_2.png"), "image/png", pictureService.GetPictureSeName(productAcerNitro.Name), reference: Domain.Common.Reference.Product, objectId: productAcerNitro.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productAcerNitro);


            var productDellG5 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Dell Inspiron G5",
                ShortDescription = "The Dell Inspiron G5 is a 15-inch gaming notebook with fantastic graphics capabilities, thanks to the NVIDIA® GeForce® GTX 1060 Max-Q graphics card, the latest 8th generation Intel® Core ™ i7 hexa-core processor and efficient DDR4 2666MHz RAM memory. The Inspiron G5 is designed specifically with the specific, demanding needs of gaming enthusiasts in mind.",
                FullDescription = "The Dell Inspiron G5 is a 15-inch gaming notebook with fantastic graphics capabilities, thanks to the NVIDIA® GeForce® GTX 1060 Max-Q graphics card, the latest 8th generation Intel® Core ™ i7 hexa-core processor and efficient DDR4 2666MHz RAM memory. The Inspiron G5 is designed specifically with the specific, demanding needs of gaming enthusiasts in mind.</p><p>The enormous performance, speed and dynamics of the eighth generation Intel Core i7 Coffee Lake processor is a guarantee of the highest performance in gaming and smooth operation with advanced applications. When more power is needed, Turbo Boost 2.0 technology intelligently speeds up the clock speed, unleashing the maximum potential of each CPU core. In addition, the unit flawlessly supports the highest definition video as well as spherical videos, while ensuring the security of transactions concluded on the network.</p><p>Play the latest, most demanding games with GeForce GTX 1060 Max-Q. Pull the sliders to the maximum and admire virtual worlds in 4K quality, enriched with HDR mode and DirectX 12 functions. All this with excellent smoothness of the image, without lag and clipping, thanks to the breakthrough architecture of Pascal GPU, packed with technologies for players.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1460,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Notebooks").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Dell").Id,
                ProductSpecificationAttributes =
                {
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "15.6''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i7").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 3,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Memory").SpecificationAttributeOptions.Single(sao => sao.Name == "8 GB").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 4,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Hardrive").SpecificationAttributeOptions.Single(sao => sao.Name == "500 GB").Id
                    }
                }
            };
            allProducts.Add(productDellG5);
            productDellG5.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_dell_g5_1.png"), "image/png", pictureService.GetPictureSeName(productDellG5.Name), reference: Domain.Common.Reference.Product, objectId: productDellG5.Id)).Id,
                DisplayOrder = 1,
            });
            productDellG5.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_dell_g5_2.png"), "image/png", pictureService.GetPictureSeName(productDellG5.Name), reference: Domain.Common.Reference.Product, objectId: productDellG5.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDellG5);


            var productDellXPS = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Dell XPS",
                ShortDescription = "Dell laptop with a screen size of 13.4 inches and a resolution of 1920 x 1200 pixels. It is equipped with an Intel Core i7-1065G7 processor with a clock frequency of 1.3 - 3.9 GHz, DDR4 RAM memory with a size of 16 GB. 1000 GB SSD hard drive. Intel HD Graphics.",
                FullDescription = "<p>Dell laptop with a screen size of 13.4 inches and a resolution of 1920 x 1200 pixels. It is equipped with an Intel Core i7-1065G7 processor with a clock frequency of 1.3 - 3.9 GHz, DDR4 RAM memory with a size of 16 GB. 1000 GB SSD hard drive. Intel HD Graphics. Graphics card size shared with RA, integrated. The installed operating system is Windows 10 Home.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1360,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Notebooks").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Dell").Id,
                ProductSpecificationAttributes =
                {
                   new ProductSpecificationAttribute
                    {
                        AllowFiltering = false,
                        ShowOnProductPage = true,
                        DisplayOrder = 1,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "Screensize").SpecificationAttributeOptions.Single(sao => sao.Name == "14.0''").Id
                    },
                    new ProductSpecificationAttribute
                    {
                        AllowFiltering = true,
                        ShowOnProductPage = true,
                        DisplayOrder = 2,
                        SpecificationAttributeId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").Id,
                        SpecificationAttributeOptionId = _specificationAttributeRepository.Table.Single(sa => sa.Name == "CPU Type").SpecificationAttributeOptions.Single(sao => sao.Name == "Intel Core i7").Id
                    }
                }
            };
            allProducts.Add(productDellXPS);
            productDellXPS.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_dell_xps_1.png"), "image/png", pictureService.GetPictureSeName(productDellXPS.Name), reference: Domain.Common.Reference.Product, objectId: productDellXPS.Id)).Id,
                DisplayOrder = 1,
            });
            productDellXPS.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_dell_xps_2.png"), "image/png", pictureService.GetPictureSeName(productDellXPS.Name), reference: Domain.Common.Reference.Product, objectId: productDellXPS.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productDellXPS);

            #endregion

            #region Accessories


            var productLenovoYogaDuet = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lenovo Yoga Duet",
                ShortDescription = "The adjustable stand allows for more convenient and effective work, sketching or taking notes in laptop mode or at a lower angle. The detachable Bluetooth® keyboard allows you to write and look at the screen with even more freedom.",
                FullDescription = "<p>Weighing just 1.16 kg, the Yoga Duet 7i is light and versatile enough to be used anywhere. The adjustable stand allows for more convenient and effective work, sketching or taking notes in laptop mode or at a lower angle. The detachable Bluetooth® keyboard allows you to write and look at the screen with even more freedom.</p><p>The Yoga Duet 7i is an advanced mobile device that is not only intuitive to use and easily personalized, but also uncompromisingly efficient. How is this possible? Powered by the 10th Gen Intel® Core ™ processor and artificial intelligence features that dynamically adjust power to optimize battery life. So it works for up to 10.8 hours on a single charge. It also has USB-C ports for faster charging.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 75,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Tablets").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLenovoYogaDuet);
            productLenovoYogaDuet.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_yoga_duet_1.png"), "image/png", pictureService.GetPictureSeName(productLenovoYogaDuet.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoYogaDuet.Id)).Id,
                DisplayOrder = 1,
            });
            productLenovoYogaDuet.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_yoga_duet_2.png"), "image/png", pictureService.GetPictureSeName(productLenovoYogaDuet.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoYogaDuet.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productLenovoYogaDuet);


            var productLenovoSmartTab = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lenovo Smart Tab",
                ShortDescription = "The most versatile entertainment tablet now can do even more.",
                FullDescription = "<p>The most versatile entertainment tablet now can do even more. Lenovo Yoga Smart Tab with Google Assistant is a development of the groundbreaking Yoga Tab 3 with a stand for working in various modes. This tablet offers high-end entertainment features such as an IPS display with FHD resolution and two JBL® stereo speakers. Additionally, it also acts as a digital home control center. The built-in LTE modem ensures permanent access to the Internet.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 65,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Tablets").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLenovoSmartTab);
            productLenovoSmartTab.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_smart_tab_1.png"), "image/png", pictureService.GetPictureSeName(productLenovoSmartTab.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoSmartTab.Id)).Id,
                DisplayOrder = 1,
            });
            productLenovoSmartTab.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lenovo_smart_tab_2.png"), "image/png", pictureService.GetPictureSeName(productLenovoSmartTab.Name), reference: Domain.Common.Reference.Product, objectId: productLenovoSmartTab.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productLenovoSmartTab);


            var productAsusMixedReality = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Asus Mixed Reality",
                ShortDescription = "Explore exciting new virtual worlds with the ASUS Windows Mixed Reality Headset!",
                FullDescription = "<p>Explore exciting new virtual worlds with the ASUS Windows Mixed Reality Headset! It features a unique and beautiful 3D-pattern aesthetic and a comfy weight-balanced design with premium antibacterial cushioned materials, so it’s not only stylish but also supremely cool and comfortable for extended periods of exploring. Unlike other headsets, the ASUS Windows Mixed Reality Headset doesn’t need any external sensors, making initial set up super easy — you’ll be ready to play in 10 minutes or less*! It’s the revolutionary, easy-to-use and affordable way to explore your imagination.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 399,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Display").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productAsusMixedReality);
            productAsusMixedReality.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_asus_mixed_reality_1.png"), "image/png", pictureService.GetPictureSeName(productAsusMixedReality.Name), reference: Domain.Common.Reference.Product, objectId: productAsusMixedReality.Id)).Id,
                DisplayOrder = 1,
            });
            productAsusMixedReality.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_asus_mixed_reality_2.png"), "image/png", pictureService.GetPictureSeName(productAsusMixedReality.Name), reference: Domain.Common.Reference.Product, objectId: productAsusMixedReality.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productAsusMixedReality);


            #endregion

            #region Display

            var productAcerProjector = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Acer Projector C250",
                ShortDescription = "Auto-Portrait Technology Now the projector image can rotate automatically, just like on your phone!",
                FullDescription = "<p>Auto-Portrait Technology Now the projector image can rotate automatically, just like on your phone! Equipped with Auto-Portrait technology, the C250i is the first model to be able to rotate the projected image in real time. There is no need to customize settings or content. Simply place the image vertically and the Acer C250i projector will remove intrusive black stripes by itself when you activate this mode. No stand, projecting from any angle The projector can be easily taken anywhere. The exclusive design does not take up much space and allows a 360-degree projection from any angle without using the stand. FHD Resolutions A beautiful 100-inch 1080p Full HD picture looks almost as realistic as the view from the window.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 530,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Display").Id,
                        DisplayOrder = 3,
                    }
                }
            };
            allProducts.Add(productAcerProjector);
            productAcerProjector.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_projector_1.png"), "image/png", pictureService.GetPictureSeName(productAcerProjector.Name), reference: Domain.Common.Reference.Product, objectId: productAcerProjector.Id)).Id,
                DisplayOrder = 1,
            });
            productAcerProjector.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_projector_2.png"), "image/png", pictureService.GetPictureSeName(productAcerProjector.Name), reference: Domain.Common.Reference.Product, objectId: productAcerProjector.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productAcerProjector);


            var productAcerMonitor = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Acer Nitro XZ2",
                ShortDescription = "Eliminate choppy gameplay and distracting visual tear with AMD Radeon FreeSync™1. Savor the smooth, responsive visuals as the monitor’s refresh rate is synched to your computer’s framerate.",
                FullDescription = "<p>Eliminate choppy gameplay and distracting visual tear with AMD Radeon FreeSync™1. Savor the smooth, responsive visuals as the monitor’s refresh rate is synched to your computer’s framerate.</p><p>Enjoy seamless, lag-free gaming with a blazingly fast 165Hz2 refresh rate. To keep pace with the action, the rapid 4ms response time provides clearer, more immersive images.</p><p>Take your gameplay to the next level with improved color accuracy and contrast guaranteed by this VESA Certified DisplayHDR™ 400 monitor. This industry standard specifies HDR quality, including luminance, color gamut, bit depth, and rise time.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 1300,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Display").Id,
                        DisplayOrder = 2,
                    }
                }
            };
            allProducts.Add(productAcerMonitor);
            productAcerMonitor.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_monitor_1.png"), "image/png", pictureService.GetPictureSeName(productAcerMonitor.Name), reference: Domain.Common.Reference.Product, objectId: productAcerMonitor.Id)).Id,
                DisplayOrder = 1,
            });
            productAcerMonitor.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_acer_monitor_2.png"), "image/png", pictureService.GetPictureSeName(productAcerMonitor.Name), reference: Domain.Common.Reference.Product, objectId: productAcerMonitor.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productAcerMonitor);

            #endregion

            #region Smartphone

            var productRedmiK30 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Redmi K30 Ultra",
                ShortDescription = "Redmi K30 Ultra will be equipped with top-class components. The smartphone is to debut on the market with a screen made in IPS LCD technology with a maximum refresh rate of 144 Hz, which guarantees smooth scrolling and perfect sharpness in movies and video games. It is worth noting that until now, displays of this type have been reserved exclusively for high-end gaming smartphones. The heart of the latest Redmi will be the MediaTek Dimensity 1000+ processor.",
                FullDescription = "<p>Redmi K30 Ultra equipped with top-class components. The smartphone debut on the market with a screen made in IPS LCD technology with a maximum refresh rate of 144 Hz, which guarantees smooth scrolling and perfect sharpness in movies and video games. It is worth noting that until now, displays of this type have been reserved exclusively for high-end gaming smartphones. The heart of the latest Redmi will be the MediaTek Dimensity 1000+ processor. It is the collection's flagship chip offering eight cores, support for 5G connectivity and support for up to 16 GB RAM.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                Flag = "New",
                AllowCustomerReviews = true,
                Price = 199,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                ShowOnHomePage = true,
                MarkAsNew = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartphones").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productRedmiK30);
            productRedmiK30.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_Redmi_K30.png"), "image/png", pictureService.GetPictureSeName(productRedmiK30.Name), reference: Domain.Common.Reference.Product, objectId: productRedmiK30.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productRedmiK30);


            var productRedmiNote9 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Redmi Note 9",
                ShortDescription = "Redmi Note 9 is equipped with a high-performance octa-core processor with a maximum clock frequency of 2.0 GHz. The maximum GPU frequency of 1.0GHz ensures better performance and thus offers a smooth gaming experience.",
                FullDescription = "<p>Redmi Note 9 is equipped with a high-performance octa-core processor with a maximum clock frequency of 2.0 GHz. The maximum GPU frequency of 1.0GHz ensures better performance and thus offers a smooth gaming experience.</p><p>Thanks to the improved 5020mAh battery, you can enjoy long work on a single charge. In combination with the 18W fast charge, you will get excellent results and charge the battery in no time.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 249,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartphones").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productRedmiNote9);
            productRedmiNote9.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_Redmi_Note_9_1.png"), "image/png", pictureService.GetPictureSeName(productRedmiNote9.Name), reference: Domain.Common.Reference.Product, objectId: productRedmiNote9.Id)).Id,
                DisplayOrder = 1,
            });
            productRedmiNote9.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_Redmi_Note_9_2.png"), "image/png", pictureService.GetPictureSeName(productRedmiNote9.Name), reference: Domain.Common.Reference.Product, objectId: productRedmiNote9.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productRedmiNote9);


            var productPocoF2Pro = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "POCO F2 Pro",
                ShortDescription = "The speed demon is now even better. Powered by an octa-core trifecta processor with a liquid cooling system, it provides a perfect working experience. Quad Camera with Pro Mode support.",
                FullDescription = "<p>The speed demon is now even better. Powered by an octa-core trifecta processor with a liquid cooling system, it provides a perfect working experience. Quad Camera with Pro Mode support.</p><p>The technology of execution in 7nm provides a 25% increase in performance, improving the smoothness of graphics rendering, while significantly reducing energy consumption. Kryo 585 ™ processor | Adreno 650 ™ graphics processor</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 299,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartphones").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productPocoF2Pro);
            productPocoF2Pro.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_POCO_F2_Pro.png"), "image/png", pictureService.GetPictureSeName(productPocoF2Pro.Name), reference: Domain.Common.Reference.Product, objectId: productPocoF2Pro.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productPocoF2Pro);


            #endregion

            #region Others



            var productMiSmartBand = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Mi Smart Band 3i",
                ShortDescription = "<b>Mi Smart Band 3i:</b> Ignite your fitness journey with water resistant smart watch. Intuitive and Easy to View with large 0.78 inch OLED touch display. Get moving with the Fit App. Activity tracker and sleep tracker included.",
                FullDescription = "<p><b>Mi Smart Band 3i:</b> Ignite your fitness journey with water resistant smart watch. Intuitive and Easy to View with large 0.78 inch OLED touch display. Get moving with the Fit App. Activity tracker and sleep tracker included.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 79.99,
                IsShipEnabled = true,
                IsFreeShipping = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                MarkAsNew = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                TierPrices =
                {
                    new TierPrice
                    {
                        Quantity = 2,
                        Price = 19,
                        StartDateTimeUtc = DateTime.UtcNow,
                        EndDateTimeUtc = null
                    },
                    new TierPrice
                    {
                        Quantity = 5,
                        Price = 17,
                    },
                    new TierPrice
                    {
                        Quantity = 10,
                        Price = 15,
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartwatches").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productMiSmartBand);
            productMiSmartBand.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_Mi_Smart_Band_3i_1.png"), "image/png", pictureService.GetPictureSeName(productMiSmartBand.Name), reference: Domain.Common.Reference.Product, objectId: productMiSmartBand.Id)).Id,
                DisplayOrder = 1,
            });
            productMiSmartBand.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_Mi_Smart_Band_3i_2.png"), "image/png", pictureService.GetPictureSeName(productMiSmartBand.Name), reference: Domain.Common.Reference.Product, objectId: productMiSmartBand.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productMiSmartBand);


            var productPs4 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Playstation 4 Slim",
                ShortDescription = "Meet the sleeker, smaller PS4 ™ that offers gamers an amazing gaming experience. The volume of the new PS4 is more than 30% smaller compared to previous console models, and its weight has been reduced by 25% and 16% respectively compared to the first (CUH-1000 series) and current (CUH-1200) versions of the PS4™.",
                FullDescription = "<p>Meet the sleeker, smaller PS4 ™ that offers gamers an amazing gaming experience. The volume of the new PS4 is more than 30% smaller compared to previous console models, and its weight has been reduced by 25% and 16% respectively compared to the first (CUH-1000 series) and current (CUH-1200) versions of the PS4 ™.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 39,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productPs4);
            productPs4.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_ps4_1.png"), "image/png", pictureService.GetPictureSeName(productPs4.Name), reference: Domain.Common.Reference.Product, objectId: productPs4.Id)).Id,
                DisplayOrder = 1,
            });
            productPs4.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_ps4_2.png"), "image/png", pictureService.GetPictureSeName(productPs4.Name), reference: Domain.Common.Reference.Product, objectId: productPs4.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productPs4);


            var productMiBeard = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Xiaomi Mi Beard",
                ShortDescription = "Rounded blades for skin - friendly performance. Advanced self - sharpening. When in a rush, simply plug in the cord and get trimming. IPX7 fully-washable body The whole body is hydro - resistant and fully washable for your convenience.Comes with detachable head.",
                FullDescription = "<p>Rounded blades for skin - friendly performance. Advanced self - sharpening. With 2 combs that can go between 0.5mm and 20mm, this trimmer will perfectly sculpt your beard.Precision is at its finest with 6000 oscillations per min delivering accurate cuts and even shape.</p><p>IPX7 fully-washable body The whole body is hydro - resistant and fully washable for your convenience.Comes with detachable head.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 37,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                BrandId = _brandRepository.Table.Single(c => c.Name == "Xiaomi").Id,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productMiBeard);
            productMiBeard.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_mi_beard_1.png"), "image/png", pictureService.GetPictureSeName(productMiBeard.Name), reference: Domain.Common.Reference.Product, objectId: productMiBeard.Id)).Id,
                DisplayOrder = 1,
            });
            productMiBeard.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_mi_beard_2.png"), "image/png", pictureService.GetPictureSeName(productMiBeard.Name), reference: Domain.Common.Reference.Product, objectId: productMiBeard.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productMiBeard);


            #endregion

            #region Shoes


            var productAdidasPredator = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "adidas Predator Instinct",
                ShortDescription = "Adidas Predator is the highest quality model of football boots. The special construction of the sole guarantees high flexibility and great adhesion, and the ingredients used to make the upper (synthetic material) ensure optimal weight of the shoe and adequate protection throughout the year.",
                FullDescription = "<p>Adidas Predator is the highest quality model of football boots. The special construction of the sole guarantees high flexibility and great adhesion, and the ingredients used to make the upper (synthetic material) ensure optimal weight of the shoe and adequate protection throughout the year. A feature of this model is also excellent vapor permeability - the moisture generated during the game is effectively expelled to the outside. The unique comfort and excellent foot support are due to the modern construction elements used by Adidas, which improve the player's features on the pitch. In Adidas footwear, the footballer turns into a ruthless predator. Thanks to the combination of modern technologies and great design, it is an excellent choice and fun to play.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 149,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Size").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "8",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "9",
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "10",
                                DisplayOrder = 3,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "11",
                                DisplayOrder = 4,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Color").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "White/Blue",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "White/Black",
                                DisplayOrder = 2,
                            },
                        }
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Shoes").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Adidas").Id,
            };
            allProducts.Add(productAdidasPredator);
            productAdidasPredator.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_predator_1.png"), "image/png", pictureService.GetPictureSeName(productAdidasPredator.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasPredator.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productAdidasPredator);


            var productAdidasNitrocharge = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Adidas Nitrocharge",
                ShortDescription = "The shoes are made of synthetic materials, they fit well to the foot thanks to their anatomical design and the insole made of Eva material. They are designed for playing and running on natural surfaces.",
                FullDescription = "<p>One of three colorways of the adidas Consortium Campus 80s Primeknit set to drop alongside each other. This pair comes in light maroon and running white. Featuring a maroon-based primeknit upper with white accents. A limited release, look out for these at select adidas Consortium accounts worldwide.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 99,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Size").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "8",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "9",
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "10",
                                DisplayOrder = 3,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "11",
                                DisplayOrder = 4,
                            }
                        }
                    },
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Color").Id,
                        AttributeControlTypeId = AttributeControlType.ColorSquares,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Yellow",
                                IsPreSelected = true,
                                ColorSquaresRgb = "#FFFF00",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Blue",
                                ColorSquaresRgb = "#363656",
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Orange",
                                ColorSquaresRgb = "#FF8000",
                                DisplayOrder = 3,
                           }
                        }
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Shoes").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Adidas").Id,
            };
            allProducts.Add(productAdidasNitrocharge);
            productAdidasNitrocharge.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidas.png"), "image/png", pictureService.GetPictureSeName(productAdidasNitrocharge.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasNitrocharge.Id)).Id,
                DisplayOrder = 1,
            });
            productAdidasNitrocharge.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidas_2.png"), "image/png", pictureService.GetPictureSeName(productAdidasNitrocharge.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasNitrocharge.Id)).Id,
                DisplayOrder = 2,
            });
            productAdidasNitrocharge.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidas_3.png"), "image/png", pictureService.GetPictureSeName(productAdidasNitrocharge.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasNitrocharge.Id)).Id,
                DisplayOrder = 3,
            });
            productAdidasNitrocharge.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidas_4.png"), "image/png", pictureService.GetPictureSeName(productAdidasNitrocharge.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasNitrocharge.Id)).Id,
                DisplayOrder = 4,
            });


            await _productRepository.InsertAsync(productAdidasNitrocharge);

            var productAttribute = _productAttributeRepository.Table.Where(x => x.Name == "Color").FirstOrDefault();

            productAdidasNitrocharge.ProductAttributeMappings.Where(x => x.ProductAttributeId == productAttribute.Id).First().ProductAttributeValues.Where(x => x.Name == "Blue").First().PictureId = productAdidasNitrocharge.ProductPictures.ElementAt(1).PictureId;
            productAdidasNitrocharge.ProductAttributeMappings.Where(x => x.ProductAttributeId == productAttribute.Id).First().ProductAttributeValues.Where(x => x.Name == "Yellow").First().PictureId = productAdidasNitrocharge.ProductPictures.ElementAt(2).PictureId;
            productAdidasNitrocharge.ProductAttributeMappings.Where(x => x.ProductAttributeId == productAttribute.Id).First().ProductAttributeValues.Where(x => x.Name == "Orange").First().PictureId = productAdidasNitrocharge.ProductPictures.ElementAt(3).PictureId;
            await _productRepository.UpdateAsync(productAdidasNitrocharge);


            var productAdidasTurfs = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Adidas Turfs",
                ShortDescription = "Be unpredictable. Shoes designed for the youngest players starting their adventure with football. The sole is designed to provide perfect grip on artificial turf and hard or frozen surfaces.",
                FullDescription = "Be unpredictable. Shoes designed for the youngest players starting their adventure with football. The sole is designed to provide perfect grip on artificial turf and hard or frozen surfaces. The X-SKIN INSPIRATION upper made of synthetic material will give a feeling of lightness and support while guiding the ball thanks to the convex texture. The heel stiffening will provide stability, and the textile inner lining will provide comfort and adequate cushioning. A profiled insole reflecting the anatomical shape of the foot and symmetrical lacing will keep the foot in the right position. The sole will allow for dynamic feints and changes in the direction of the run on artificial turf as well as hard or frozen surfaces.",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 89,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Shoes").Id,
                        DisplayOrder = 1,
                    }
                },
                BrandId = _brandRepository.Table.Single(c => c.Name == "Adidas").Id,
            };
            allProducts.Add(productAdidasTurfs);
            productAdidasTurfs.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidasturfs.png"), "image/png", pictureService.GetPictureSeName(productAdidasTurfs.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasTurfs.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productAdidasTurfs);


            #endregion

            #region Apparel

            //this one is a grouped product with two associated ones
            var productDerbyKit = new Product
            {
                ProductTypeId = ProductType.GroupedProduct,
                VisibleIndividually = true,
                Name = "Derby County Kit",
                ShortDescription = "Show your pride and support and show off in The Rams Homemade Costumes.",
                FullDescription = "<p>Show your pride and support and show off in The Rams Homemade Costumes.</p><p>This is an official t-shirt made according to The Rams homewear specification. The whole is decorated with the club badge and the Umbro Double Diamond logo.</p>",
                ProductLayoutId = productLayoutGrouped.Id,
                AllowCustomerReviews = true,
                Published = true,
                Price = 129.99,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyKit);
            productDerbyKit.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_awayshirt_1.png"), "image/png", pictureService.GetPictureSeName(productDerbyKit.Name), reference: Domain.Common.Reference.Product, objectId: productDerbyKit.Id)).Id,
                DisplayOrder = 1,
            });
            productDerbyKit.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_shirt_1.png"), "image/png", pictureService.GetPictureSeName(productDerbyKit.Name), reference: Domain.Common.Reference.Product, objectId: productDerbyKit.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productDerbyKit);
            var productDerbyKit_associated_1 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = false, //hide this products
                ParentGroupedProductId = productDerbyKit.Id,
                Name = "Derby County Shirt - Away",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Published = true,
                Price = 129.99,
                IsShipEnabled = true,
                Flag = "Grouped",
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyKit_associated_1);
            productDerbyKit_associated_1.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_awayshirt_1.png"), "image/png", pictureService.GetPictureSeName("Derby County Away Shirt"), reference: Domain.Common.Reference.Product, objectId: productDerbyKit_associated_1.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDerbyKit_associated_1);
            var productDerbyKit_associated_2 = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = false,
                ParentGroupedProductId = productDerbyKit.Id,
                Name = "Derby County Shirt - Home",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Published = true,
                Price = 149.99,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyKit_associated_2);
            productDerbyKit_associated_2.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_shirt_1.png"), "image/png", pictureService.GetPictureSeName("Derby County Shirt - Home"), reference: Domain.Common.Reference.Product, objectId: productDerbyKit_associated_2.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDerbyKit_associated_2);

            var productNikeKids = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Nike Kids Kit",
                ShortDescription = "Nike Dry-FIT football kit for kids. The set includes a T-shirt, shorts and football socks. Clothes made of high-quality synthetic materials that perfectly transport moisture and dry quickly.",
                FullDescription = "<p>Nike Dry-FIT football kit for kids. The set includes a T-shirt, shorts and football socks. Clothes made of high-quality synthetic materials that perfectly transport moisture and dry quickly. The set is perfect for training, matches, PE lessons, and the T-shirt and shorts are also perfect for everyday use. The shirt has ventilation panels under the arms and the back is made of airy mesh that removes excess heat. Children's shorts with an elastic, rubber belt will adapt to any figure. The set also includes football socks made of a pleasant-to-touch material that ensures high comfort of use.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Published = true,
                Price = 39,
                IsShipEnabled = true,
                Weight = 1,
                Length = 2,
                Width = 3,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Size").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Small",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "1X",
                                DisplayOrder = 2,
                            },

                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "2X",
                                DisplayOrder = 3,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "3X",
                                DisplayOrder = 4,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "4X",
                                DisplayOrder = 5,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "5X",
                                DisplayOrder = 6,
                            }
                        }
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productNikeKids);
            productNikeKids.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_kidskit.png"), "image/png", pictureService.GetPictureSeName(productNikeKids.Name), reference: Domain.Common.Reference.Product, objectId: productNikeKids.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productNikeKids);

            var productPsgKit = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Paris Saint Germain Home Kit",
                ShortDescription = "",
                FullDescription = "<p>This oversized women t-Shirt needs minimum ironing. It is a great product at a great value!</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 99.99,
                IsShipEnabled = true,
                Weight = 4,
                Length = 3,
                Width = 3,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                TierPrices =
                {
                    new TierPrice
                    {
                        Quantity = 3,
                        Price = 21,
                    },
                    new TierPrice
                    {
                        Quantity = 7,
                        Price = 19,
                    },
                    new TierPrice
                    {
                        Quantity = 10,
                        Price = 16,
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productPsgKit);
            productPsgKit.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_psg_1.png"), "image/png", pictureService.GetPictureSeName(productPsgKit.Name), reference: Domain.Common.Reference.Product, objectId: productPsgKit.Id)).Id,
                DisplayOrder = 1,
            });
            productPsgKit.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_psg_2.png"), "image/png", pictureService.GetPictureSeName(productPsgKit.Name), reference: Domain.Common.Reference.Product, objectId: productPsgKit.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productPsgKit);


            var productDerbyShirt = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Derby County Home Shirt",
                ShortDescription = "Show your pride and support and show off in The Rams Homemade Kits.",
                FullDescription = "<p>Show your pride and support and show off in The Rams Homemade Costumes.</p><p>This is an official t-shirt made according to The Rams homewear specification. The whole is decorated with the club badge and the Umbro Double Diamond logo.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 59,
                IsShipEnabled = true,
                Weight = 4,
                Length = 3,
                Width = 3,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Custom Text").Id,
                        TextPrompt = "Enter your text:",
                        AttributeControlTypeId = AttributeControlType.TextBox,
                        IsRequired = true,
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyShirt);
            productDerbyShirt.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_shirt_1.png"), "image/png", pictureService.GetPictureSeName(productDerbyShirt.Name), reference: Domain.Common.Reference.Product, objectId: productDerbyShirt.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDerbyShirt);

            var productDerbyShorts = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Derby County Home Shorts",
                ShortDescription = "Show your pride and support and show off in The Rams Homemade Kits.",
                FullDescription = "<p>Show your pride and support and show off in The Rams Homemade Costumes.</p><p>This is an official t-shirt made according to The Rams homewear specification. The whole is decorated with the club badge and the Umbro Double Diamond logo.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 29,
                IsShipEnabled = true,
                Weight = 4,
                Length = 3,
                Width = 3,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyShorts);
            productDerbyShorts.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_shorts_1.png"), "image/png", pictureService.GetPictureSeName(productDerbyShorts.Name), reference: Domain.Common.Reference.Product, objectId: productDerbyShorts.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDerbyShorts);

            var productDerbyKitHome = new Product
            {
                ProductTypeId = ProductType.BundledProduct,
                VisibleIndividually = true,
                Name = "Derby County Home Shirt",
                ShortDescription = "Show your pride and support and show off in The Rams Homemade Kits.",
                FullDescription = "<p>Show your pride and support and show off in The Rams Homemade Costumes.</p><p>This is an official t-shirt made according to The Rams homewear specification. The whole is decorated with the club badge and the Umbro Double Diamond logo.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 15,
                IsShipEnabled = true,
                Weight = 4,
                Length = 3,
                Width = 3,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productDerbyKitHome);
            productDerbyKitHome.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_derby_shirt_1.png"), "image/png", pictureService.GetPictureSeName(productDerbyKitHome.Name), reference: Domain.Common.Reference.Product, objectId: productDerbyKitHome.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productDerbyKitHome);

            var productChicagoBulls = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Chicago Bulls Jersey",
                ShortDescription = "Capture your team's distinct identity when you grab this custom Chicago Bulls jersey, It features classic trims and Chicago Bulls graphics along with Nike Dry and Dri-FIT technologies for added comfort.",
                FullDescription = "<p>Capture your team's distinct identity when you grab this custom Chicago Bulls jersey, It features classic trims and Chicago Bulls graphics along with Nike Dry and Dri-FIT technologies for added comfort. Before you watch the next game, grab this incredible jersey so everyone knows your fandom is on the cutting edge.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 43.5,
                OldPrice = 55,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                TierPrices =
                {
                    new TierPrice
                    {
                        Quantity = 3,
                        Price = 40,
                    },
                    new TierPrice
                    {
                        Quantity = 6,
                        Price = 38,
                    },
                    new TierPrice
                    {
                        Quantity = 10,
                        Price = 35,
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Apparel").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productChicagoBulls);

            productChicagoBulls.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_chicago_jersey_1.png"), "image/png", pictureService.GetPictureSeName(productChicagoBulls.Name), reference: Domain.Common.Reference.Product, objectId: productChicagoBulls.Id)).Id,
                DisplayOrder = 1,
            });
            productChicagoBulls.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_chicago_jersey_2.png"), "image/png", pictureService.GetPictureSeName(productChicagoBulls.Name), reference: Domain.Common.Reference.Product, objectId: productChicagoBulls.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productChicagoBulls);


            #endregion

            #region Smartwatches


            var productVivoactive = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Garmin VivoActive",
                ShortDescription = "The Vívoactive watch offers easy-to-repeat animated cardio, strength, yoga and Pilates exercises that you can view on your watch screen. Choose from preloaded animated workouts or download more from the Garmin Connect ™ community site.",
                FullDescription = "<p>You no longer need to search for videos and advice on the web to know what to do while training. The Vívoactive watch offers easy-to-repeat animated cardio, strength, yoga and Pilates exercises that you can view on your watch screen. Choose from preloaded animated workouts or download more from the Garmin Connect ™ community site.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 30,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductAttributeMappings =
                {
                    new ProductAttributeMapping
                    {
                        ProductAttributeId = _productAttributeRepository.Table.Single(x => x.Name == "Size").Id,
                        AttributeControlTypeId = AttributeControlType.DropdownList,
                        IsRequired = true,
                        ProductAttributeValues =
                        {
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Small",
                                DisplayOrder = 1,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Medium",
                                DisplayOrder = 2,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "Large",
                                DisplayOrder = 3,
                            },
                            new ProductAttributeValue
                            {
                                AttributeValueTypeId = AttributeValueType.Simple,
                                Name = "X-Large",
                                DisplayOrder = 4,
                            }
                        }
                    }
                },
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartwatches").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productVivoactive);
            productVivoactive.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_vivoactive.png"), "image/png", pictureService.GetPictureSeName(productVivoactive.Name), reference: Domain.Common.Reference.Product, objectId: productVivoactive.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productVivoactive);



            var productGarminFenix = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Garmin Fenix 2",
                ShortDescription = "Fēnix 2 combines the best features of our fitness watches with outdoor training watches. It is both a great navigation system and an ideal training partner in many different sports.",
                FullDescription = "<p>Fēnix 2 combines the best features of our fitness watches with outdoor training watches. It is both a great navigation system and an ideal training partner in many different sports. Whether you're running, swimming, skiing, cycling or hiking in the mountains, fēnix 2 lets you easily switch between groups of settings optimized for each activity.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 45,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartwatches").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productGarminFenix);
            productGarminFenix.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_garmin_fenix_1.png"), "image/png", pictureService.GetPictureSeName(productGarminFenix.Name), reference: Domain.Common.Reference.Product, objectId: productGarminFenix.Id)).Id,
                DisplayOrder = 1,
            });
            productGarminFenix.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_garmin_fenix_2.png"), "image/png", pictureService.GetPictureSeName(productGarminFenix.Name), reference: Domain.Common.Reference.Product, objectId: productGarminFenix.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productGarminFenix);



            var productForerunner = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Garmin Forerunner",
                ShortDescription = "This easy-to-use running watch is great for everyday runs, workouts and even pre-race training in a 10k run. Built-in GPS tracks your running route and provides accurate distance, pace and interval statistics.",
                FullDescription = "<p>This easy-to-use running watch is great for everyday runs, workouts and even pre-race training in a 10k run. Built-in GPS tracks your running route and provides accurate distance, pace and interval statistics. Its intuitive interface makes it easy to mark laps or pause the timer, even with sweaty hands. Forerunner 45 also monitors heart rate on the wrist during the day and while you sleep.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 25,
                IsShipEnabled = true,
                Weight = 7,
                Length = 7,
                Width = 7,
                Height = 7,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Apparel").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Smartwatches").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productForerunner);
            productForerunner.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_forerunner.png"), "image/png", pictureService.GetPictureSeName(productForerunner.Name), reference: Domain.Common.Reference.Product, objectId: productForerunner.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productForerunner);

            #endregion

            #region Digital Downloads


            var downloadCyberpunk1 = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                ContentType = "application/x-zip-co",
                DownloadBinary = File.ReadAllBytes(sampleDownloadsPath + "product_cyberpunk_1.zip"),
                Extension = ".zip",
                Filename = "Cyberpunk",
                IsNew = true,
            };
            await downloadService.InsertDownload(downloadCyberpunk1);
            var downloadCyberpunk2 = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                ContentType = "text/plain",
                DownloadBinary = File.ReadAllBytes(sampleDownloadsPath + "product_cyberpunk_2.txt"),
                Extension = ".txt",
                Filename = "Cyberpunk",
                IsNew = true,
            };
            await downloadService.InsertDownload(downloadCyberpunk2);
            var productCyberpunk = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Cyberpunk 2077",
                ShortDescription = "Cyberpunk 2077 is an open-world adventure set in Night City, a megalopolis ruled by an obsessive quest for power, fame and body remake. Your name is V and you must acquire a one-of-a-kind implant - the key to immortality. Create your own playstyle and set out to conquer the mighty city of the future, whose history is shaped by your decisions.",
                FullDescription = "<p>Cyberpunk 2077 is an open-world adventure set in Night City, a megalopolis ruled by an obsessive quest for power, fame and body remake. Your name is V and you must acquire a one-of-a-kind implant - the key to immortality. Create your own playstyle and set out to conquer the mighty city of the future, whose history is shaped by your decisions.</p><p>Become a cyberpunk, a freelance armed to the teeth, and become the legend of the most dangerous city of the future. Create your character from scratch. Take on the role of the outlaw Punk, freedom-loving Nomad or ruthless Corp.</p><p>Get the most powerful implant in Night City and take on those who shake the whole city. Follow Rockerboy Johnny Silverhand (played by Keanu Reeves) and change a world ruled by large corporations forever. And all this is accompanied by music from bands and creators such as Run the Jewels, Refused, Grimes, A $ AP Rocky, Gazelle Twin, Ilan Rubin, Richard Devine, Nina Kraviz, Deadly Hunta, Rat Boy and Tina Guo.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 69,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Downloadable Products").Id,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                IsDownload = true,
                DownloadId = downloadCyberpunk1.Id,
                DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid,
                UnlimitedDownloads = true,
                HasUserAgreement = false,
                HasSampleDownload = true,
                SampleDownloadId = downloadCyberpunk2.Id,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Digital downloads").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productCyberpunk);
            productCyberpunk.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_cyberpunk_1.png"), "image/png", pictureService.GetPictureSeName(productCyberpunk.Name), reference: Domain.Common.Reference.Product, objectId: productCyberpunk.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productCyberpunk);

            var downloadGTA1 = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                ContentType = "application/x-zip-co",
                DownloadBinary = File.ReadAllBytes(sampleDownloadsPath + "product_GTA_1.zip"),
                Extension = ".zip",
                Filename = "GTA",
                IsNew = true,
            };
            await downloadService.InsertDownload(downloadGTA1);
            var downloadGTA2 = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                ContentType = "text/plain",
                DownloadBinary = File.ReadAllBytes(sampleDownloadsPath + "product_GTA_2.txt"),
                Extension = ".txt",
                Filename = "GTA",
                IsNew = true,
            };
            await downloadService.InsertDownload(downloadGTA2);
            var productGTA = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Grand Theft Auto 5",
                ShortDescription = "When a young street hustler, a retired bank robber and a terrifying psychopath land themselves in trouble, they must pull off a series of dangerous heists to survive in a city in which they can trust nobody, least of all each other.",
                FullDescription = "<p>When a young street hustler, a retired bank robber and a terrifying psychopath land themselves in trouble, they must pull off a series of dangerous heists to survive in a city in which they can trust nobody, least of all each other.</p><p>Launch business ventures from your Maze Bank West Executive Office, research powerful weapons technology from your underground Gunrunning Bunker and use your Counterfeit Cash Factory to start a lucrative counterfeiting operation.</p><p>Tear through the streets with a range of 10 high performance vehicles including a Supercar, Motorcycles, the weaponized Dune FAV, a Helicopter, a Rally Car and more. You’ll also get properties including a 10 car garage to store your growing fleet.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 49,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Downloadable Products").Id,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                IsDownload = true,
                DownloadId = downloadGTA1.Id,
                DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid,
                UnlimitedDownloads = true,
                HasUserAgreement = false,
                HasSampleDownload = true,
                SampleDownloadId = downloadGTA2.Id,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Digital downloads").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productGTA);

            productGTA.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_gta_1.png"), "image/png", pictureService.GetPictureSeName(productGTA.Name), reference: Domain.Common.Reference.Product, objectId: productGTA.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productGTA);


            var downloadCod = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                ContentType = "application/x-zip-co",
                DownloadBinary = File.ReadAllBytes(sampleDownloadsPath + "product_cod_1.zip"),
                Extension = ".zip",
                Filename = "Call of Duty",
                IsNew = true,
            };
            await downloadService.InsertDownload(downloadCod);
            var productCod = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Call of Duty: ColdWar",
                ShortDescription = "The iconic Black Ops series is back with Call of Duty®: Black Ops Cold War - the direct sequel to the original and fan-favorite Call of Duty®: Black Ops. Black Ops Cold War will drop fans into the depths of the Cold War’s volatile geopolitical battle of the early 1980s.",
                FullDescription = "<p>The iconic Black Ops series is back with Call of Duty®: Black Ops Cold War - the direct sequel to the original and fan-favorite Call of Duty®: Black Ops. Black Ops Cold War will drop fans into the depths of the Cold War’s volatile geopolitical battle of the early 1980s. Nothing is ever as it seems in a gripping single-player Campaign, where players will come face-to-face with historical figures and hard truths, as they battle around the globe through iconic locales like East Berlin, Vietnam, Turkey, Soviet KGB headquarters and more. As elite operatives, you will follow the trail of a shadowy figure named Perseus who is on a mission to destabilize the global balance of power and change the course of history. Descend into the dark center of this global conspiracy alongside iconic characters Woods, Mason and Hudson and a new cast of operatives attempting to stop a plot decades in the making. Beyond the Campaign, players will bring a Cold War arsenal of weapons and equipment into the next generation of Multiplayer and Zombies experiences.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 69,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Downloadable Products").Id,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                IsDownload = true,
                DownloadId = downloadCod.Id,
                DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid,
                UnlimitedDownloads = true,
                HasUserAgreement = false,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Digital downloads").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productCod);
            productCod.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_cod_1.png"), "image/png", pictureService.GetPictureSeName(productCod.Name), reference: Domain.Common.Reference.Product, objectId: productCod.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productCod);



            #endregion

            #region Lego

            var productLegoFalcon = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "LEGO Millennium Falcon",
                ShortDescription = "Inspire kids and adults with the LEGO® Star Wars ™ 75257 Millennium Falcon model. The brick-built version of the iconic Corellian freighter features a variety of details. This iconic set from the LEGO Star Wars series is a great addition to any fan's collection.",
                FullDescription = "<p>Inspire kids and adults with the LEGO® Star Wars ™ 75257 Millennium Falcon model. The brick-built version of the iconic Corellian freighter features a variety of details, including a rotating lower and upper gun turret, 2 spring-loaded shooters, a lowering ramp and an opening cockpit with space for 2 minifigures. The top panels fold out to reveal a detailed interior where children will love to reenact scenes from Star Wars: Skywalker. Rebirth ”featuring characters from the“ Star Wars ”universe - Finn, Chewbakka, Lando Calrissian, Boolio, C-3PO, R2-D2 and D-O. This iconic set from the LEGO Star Wars series is a great addition to any fan's collection.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 109,
                OldPrice = 199,
                IsShipEnabled = true,
                IsFreeShipping = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Lego").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Lego").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLegoFalcon);
            productLegoFalcon.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lego_falcon_1.png"), "image/png", pictureService.GetPictureSeName(productLegoFalcon.Name), reference: Domain.Common.Reference.Product, objectId: productLegoFalcon.Id)).Id,
                DisplayOrder = 1,
            });
            productLegoFalcon.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lego_falcon_2.png"), "image/png", pictureService.GetPictureSeName(productLegoFalcon.Name), reference: Domain.Common.Reference.Product, objectId: productLegoFalcon.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productLegoFalcon);



            var productLegoHogwarts = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lego Hogwarts",
                ShortDescription = "Taste real magic at the LEGO® Harry Potter™ Hogwarts™ Castle! Unforgettable building satisfaction with this highly detailed LEGO Harry Potter collectible set with over 6,000 pieces.",
                FullDescription = "<p>Taste real magic at the LEGO® Harry Potter™ Hogwarts™ Castle! Unforgettable building satisfaction with this highly detailed LEGO Harry Potter collectible set with over 6,000 pieces. It is packed with elements from the Harry Potter series - you will find towers, turrets, chambers, classrooms, creatures, Whomping Willow ™, Hagrid's hut and many other signature details. Plus, with 4 minifigures and 27 microfigures of students, teachers, statues and 5 Dementors, this advanced construction toy set is the perfect gift for any Harry Potter fan.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 99,
                OldPrice = 149,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Lego").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Lego").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLegoHogwarts);
            productLegoHogwarts.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lego_hogwarts_1.png"), "image/png", pictureService.GetPictureSeName(productLegoHogwarts.Name), reference: Domain.Common.Reference.Product, objectId: productLegoHogwarts.Id)).Id,
                DisplayOrder = 1,
            });
            productLegoHogwarts.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_lego_hogwarts_2.png"), "image/png", pictureService.GetPictureSeName(productLegoHogwarts.Name), reference: Domain.Common.Reference.Product, objectId: productLegoHogwarts.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productLegoHogwarts);

            var productLegoCity = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Lego City Police Base",
                ShortDescription = "Everything is awesome about the LEGO® City Police Station (60246) playset. Little law enforcers and fans of the LEGO City TV series will love creating stories with a host of fun characters, including Duke DeTain, Chief Wheeler and Daisy Kaboom. ",
                FullDescription = "<p>Everything is awesome about the LEGO® City Police Station (60246) playset. Little law enforcers and fans of the LEGO City TV series will love creating stories with a host of fun characters, including Duke DeTain, Chief Wheeler and Daisy Kaboom. </p><p>This fantastic set includes a police station with a light-brick searchlight and a police car with sound-brick siren, plus a cool truck, motorcycle and surveillance drone. A building toy with a little extra With this toy playset you get a simple building guide and Instructions.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 59,
                OldPrice = 99,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Lego").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Lego").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productLegoCity);
            productLegoCity.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_LegoCity_1.png"), "image/png", pictureService.GetPictureSeName(productLegoCity.Name), reference: Domain.Common.Reference.Product, objectId: productLegoCity.Id)).Id,
                DisplayOrder = 1,
            });
            productLegoCity.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_LegoCity_2.png"), "image/png", pictureService.GetPictureSeName(productLegoCity.Name), reference: Domain.Common.Reference.Product, objectId: productLegoCity.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productLegoCity);

            #endregion

            #region Balls

            var productAdidasBall = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Adidas Ball",
                ShortDescription = "The Adidas Finale Top Capitano is a durable training ball with strong references to the UEFA Champions League. The ball is a replica of the model used in this year's Champions League group stage and is perfect for training and spontaneous games.",
                FullDescription = "<p>The Adidas Finale Top Capitano is a durable training ball with strong references to the UEFA Champions League. The ball is a replica of the model used in this year's Champions League group stage and is perfect for training and spontaneous games. The strong TPU coating has been machine-stitched to increase the durability of the ball. The ball's electrifying multicolored design shows the emotions of fans around the world as Europe's top teams compete for the highest honor.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 69,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Balls").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                MarkAsNew = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Balls").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productAdidasBall);
            productAdidasBall.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_adidasball.png"), "image/png", pictureService.GetPictureSeName(productAdidasBall.Name), reference: Domain.Common.Reference.Product, objectId: productAdidasBall.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productAdidasBall);


            var productMikasa = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Volleyball Ball",
                ShortDescription = "Made of high-quality synthetic leather (PU) A high-class ball based on the V200W match model. Solid and strong machine sewing.The 18 - panel, colorful design increases the visibility of the ball during the game.",
                FullDescription = "<p>Made of high-quality synthetic leather (PU) A high-class ball based on the V200W match model. Solid and strong machine sewing.The 18 - panel, colorful design increases the visibility of the ball during the game.</p><p> Weight: 260 - 280g </p><p> Circumference: 65 - 67cm </p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 29.99,
                IsShipEnabled = true,
                IsFreeShipping = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Balls").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Balls").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productMikasa);
            productMikasa.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_mikasa.png"), "image/png", pictureService.GetPictureSeName(productMikasa.Name), reference: Domain.Common.Reference.Product, objectId: productMikasa.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productMikasa);


            var productSpalding = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "Spalding Basketball Ball",
                ShortDescription = "The panels of leather, composite leather are attached to the rubber body by hand. This technique is used for indoor and indoor / outdoor balls. Balloon - the highest quality inner tube that maintains the pressure of the ball.",
                FullDescription = "<p>The panels of leather, composite leather are attached to the rubber body by hand. This technique is used for indoor and indoor / outdoor balls. Balloon - the highest quality inner tube that maintains the pressure of the ball. A specialized nylon braid - nylon lines give the ball integrity and durability. Smooth body and channels for a softer feel and strength - optimized deep channel design for better grip and control. Composite leather cover - provides a good grip, feel and aesthetic appearance of the ball, as well as the necessary strength and resistance to abrasion. Composite leather has an advanced moisture management system to improve dry and wet grip.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 49,
                IsShipEnabled = true,
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Balls").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Balls").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productSpalding);
            productSpalding.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_spalding.png"), "image/png", pictureService.GetPictureSeName(productSpalding.Name), reference: Domain.Common.Reference.Product, objectId: productSpalding.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(productSpalding);



            #endregion

            #region Gift vouchers


            var product25GiftVoucher = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "$25 Virtual Gift voucher",
                ShortDescription = "$25 Gift voucher. Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products.",
                FullDescription = "<p>Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products. Purchases are deducted from the GiftVoucher balance. Any unused balance will be placed in the recipient's GiftVoucher account when redeemed. If an order exceeds the amount of the GiftVoucher, the balance must be paid with a credit card or other available payment method.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 25,
                IsGiftVoucher = true,
                GiftVoucherTypeId = GiftVoucherType.Virtual,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                Published = true,
                ShowOnHomePage = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Gift vouchers").Id,
                        DisplayOrder = 2,
                    }
                }
            };
            allProducts.Add(product25GiftVoucher);
            product25GiftVoucher.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_25giftcart.png"), "image/png", pictureService.GetPictureSeName(product25GiftVoucher.Name), reference: Domain.Common.Reference.Product, objectId: product25GiftVoucher.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(product25GiftVoucher);


            var product50GiftVoucher = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "$50 Physical Gift voucher",
                ShortDescription = "$50 Gift voucher. Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products.",
                FullDescription = "<p>Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products. Purchases are deducted from the GiftVoucher balance. Any unused balance will be placed in the recipient's GiftVoucher account when redeemed. If an order exceeds the amount of the GiftVoucher, the balance must be paid with a credit card or other available payment method.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 50,
                IsGiftVoucher = true,
                GiftVoucherTypeId = GiftVoucherType.Physical,
                IsShipEnabled = true,
                IsFreeShipping = true,
                DeliveryDateId = deliveryDate.Id,
                Weight = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                Published = true,
                MarkAsNew = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Gift vouchers").Id,
                        DisplayOrder = 3,
                    }
                }
            };
            allProducts.Add(product50GiftVoucher);
            product50GiftVoucher.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_50giftcart.png"), "image/png", pictureService.GetPictureSeName(product50GiftVoucher.Name), reference: Domain.Common.Reference.Product, objectId: product50GiftVoucher.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(product50GiftVoucher);


            var product100GiftVoucher = new Product
            {
                ProductTypeId = ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = "$100 Physical Gift voucher",
                ShortDescription = "$100 Gift voucher. Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products.",
                FullDescription = "<p>Gift vouchers must be redeemed through our site Web site toward the purchase of eligible products. Purchases are deducted from the GiftVoucher balance. Any unused balance will be placed in the recipient's GiftVoucher account when redeemed. If an order exceeds the amount of the GiftVoucher, the balance must be paid with a credit card or other available payment method.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 100,
                IsGiftVoucher = true,
                GiftVoucherTypeId = GiftVoucherType.Physical,
                IsShipEnabled = true,
                DeliveryDateId = deliveryDate.Id,
                Weight = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
                {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Gift vouchers").Id,
                        DisplayOrder = 4,
                    }
                }
            };
            allProducts.Add(product100GiftVoucher);
            product100GiftVoucher.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_100giftcart.png"), "image/png", pictureService.GetPictureSeName(product100GiftVoucher.Name), reference: Domain.Common.Reference.Product, objectId: product100GiftVoucher.Id)).Id,
                DisplayOrder = 1,
            });
            await _productRepository.InsertAsync(product100GiftVoucher);

            var productPlaystationBundlePack = new Product
            {
                ProductTypeId = ProductType.BundledProduct,
                VisibleIndividually = true,
                Name = "Playstation 5 Kit",
                ShortDescription = "Meet the sleeker, smaller PS4 ™ that offers gamers an amazing gaming experience. The volume of the new PS4 is more than 30% smaller compared to previous console models, and its weight has been reduced by 25% and 16% respectively compared to the first (CUH-1000 series) and current (CUH-1200) versions of the PS4™.",
                FullDescription = "<p>Meet the sleeker, smaller PS4 ™ that offers gamers an amazing gaming experience. The volume of the new PS4 is more than 30% smaller compared to previous console models, and its weight has been reduced by 25% and 16% respectively compared to the first (CUH-1000 series) and current (CUH-1200) versions of the PS4 ™.</p>",
                ProductLayoutId = productLayoutSimple.Id,
                AllowCustomerReviews = true,
                Price = 259,
                IsShipEnabled = true,
                Flag = "Bundle Product",
                Weight = 2,
                Length = 2,
                Width = 2,
                Height = 3,
                TaxCategoryId = _taxCategoryRepository.Table.Single(tc => tc.Name == "Electronics & Software").Id,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                StockQuantity = 10000,
                NotifyAdminForQuantityBelow = 1,
                AllowOutOfStockSubscriptions = false,
                StockAvailability = true,
                LowStockActivityId = LowStockActivity.DisableBuyButton,
                BackorderModeId = BackorderMode.NoBackorders,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                ProductCategories =
               {
                    new ProductCategory
                    {
                        CategoryId = _categoryRepository.Table.Single(c => c.Name == "Others").Id,
                        DisplayOrder = 1,
                    }
                }
            };
            allProducts.Add(productPlaystationBundlePack);
            productPlaystationBundlePack.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_sony_ps5_console_1.png"), "image/png", pictureService.GetPictureSeName(productPlaystationBundlePack.Name), reference: Domain.Common.Reference.Product, objectId: productPlaystationBundlePack.Id)).Id,
                DisplayOrder = 1,
            });
            productPlaystationBundlePack.ProductPictures.Add(new ProductPicture
            {
                PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "product_sony_ps5_console_2.png"), "image/png", pictureService.GetPictureSeName(productPlaystationBundlePack.Name), reference: Domain.Common.Reference.Product, objectId: productPlaystationBundlePack.Id)).Id,
                DisplayOrder = 2,
            });
            await _productRepository.InsertAsync(productPlaystationBundlePack);

            var productbundle1 = new BundleProduct
            {
                ProductId = productPs4.Id,
                DisplayOrder = 1,
                Quantity = 1
            };
            var productbundle2 = new BundleProduct
            {
                ProductId = productSonyPS5Pad.Id,
                DisplayOrder = 2,
                Quantity = 2
            };
            var productbundle3 = new BundleProduct
            {
                ProductId = productPs5Camera.Id,
                DisplayOrder = 3,
                Quantity = 1
            };
            var productbundle4 = new BundleProduct
            {
                ProductId = productCod.Id,
                DisplayOrder = 4,
                Quantity = 1
            };
            productPlaystationBundlePack.BundleProducts.Add(productbundle1);
            productPlaystationBundlePack.BundleProducts.Add(productbundle2);
            productPlaystationBundlePack.BundleProducts.Add(productbundle3);
            productPlaystationBundlePack.BundleProducts.Add(productbundle4);
            await _productRepository.UpdateAsync(productPlaystationBundlePack);

            #endregion

            //search engine names
            foreach (var product in allProducts)
            {
                product.SeName = SeoExtensions.GenerateSlug(product.Name, false, false, false);
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
                    EntityId = product.Id,
                    EntityName = "Product",
                    LanguageId = "",
                    IsActive = true,
                    Slug = product.SeName,
                });

                await _productRepository.UpdateAsync(product);
            }


            #region Related Products

            //related products

            productMikasa.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productSpalding.Id,
                });

            productMikasa.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasBall.Id,
                });

            productSpalding.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMikasa.Id,
                });

            productSpalding.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasBall.Id,
                });

            productAdidasBall.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMikasa.Id,
                });

            productAdidasBall.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productSpalding.Id,
                });

            productGTA.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productCyberpunk.Id,
                });

            productGTA.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productCod.Id,
                });

            productCyberpunk.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productGTA.Id,
                });

            productCyberpunk.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productCod.Id,
                });

            productLegoCity.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoHogwarts.Id,
                });

            productLegoCity.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoFalcon.Id,
                });

            productLegoHogwarts.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoCity.Id,
                });

            productLegoHogwarts.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoFalcon.Id,
                });

            productLegoFalcon.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoHogwarts.Id,
                });

            productLegoFalcon.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLegoCity.Id,
                });

            productLenovoLegionY740.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellXPS.Id,
                });

            productLenovoLegionY740.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiNotebook.Id,
                });

            productLenovoLegionY740.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAsusMixedReality.Id,
                });

            productLenovoLegionY740.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerNitro.Id,
                });

            productDellXPS.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLenovoLegionY740.Id,
                });

            productDellXPS.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiNotebook.Id,
                });

            productDellXPS.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerMonitor.Id,
                });

            productDellXPS.RelatedProducts.Add(
                 new RelatedProduct
                 {
                     ProductId2 = productDellG5.Id,
                 });

            productMiNotebook.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellXPS.Id,
                });

            productMiNotebook.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerMonitor.Id,
                });

            productMiNotebook.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLenovoLegionY740.Id,
                });

            productMiNotebook.RelatedProducts.Add(
                 new RelatedProduct
                 {
                     ProductId2 = productAcerNitro.Id,
                 });

            productAcerNitro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellXPS.Id,
                });

            productAcerNitro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerProjector.Id,
                });

            productAcerNitro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLenovoLegionY740.Id,
                });

            productAcerNitro.RelatedProducts.Add(
                 new RelatedProduct
                 {
                     ProductId2 = productDellG5.Id,
                 });

            productDellG5.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLenovoLegionY740.Id,
                });

            productDellG5.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiNotebook.Id,
                });

            productDellG5.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerNitro.Id,
                });

            productDellG5.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerMonitor.Id,
                });
            productPs5Camera.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPlaystationBundlePack.Id,
                });
            productPs5Camera.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPs4.Id,
                });

            productPs5Camera.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellG5.Id,
                });
            productPs5Camera.RelatedProducts.Add(
                 new RelatedProduct
                 {
                     ProductId2 = productAcerNitro.Id,
                 });
            productAcerProjector.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productRedmiNote9.Id,
                });

            productAcerProjector.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDerbyKit.Id,
                });

            productAcerProjector.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAcerMonitor.Id,
                });

            productAcerProjector.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPocoF2Pro.Id,
                });
            productRedmiK30.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productRedmiNote9.Id,
                });

            productRedmiK30.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPocoF2Pro.Id,
                });
            productRedmiK30.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiSmartBand.Id,
                });

            productRedmiK30.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiBeard.Id,
                });

            productRedmiNote9.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productRedmiK30.Id,
                });
            productRedmiNote9.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPocoF2Pro.Id,
                });

            productRedmiNote9.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiSmartBand.Id,
                });
            productRedmiNote9.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiBeard.Id,
                });
            productPocoF2Pro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productRedmiK30.Id,
                });
            productPocoF2Pro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productRedmiNote9.Id,
                });

            productPocoF2Pro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiSmartBand.Id,
                });
            productPocoF2Pro.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiBeard.Id,
                });

            productAdidasNitrocharge.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productChicagoBulls.Id,
                });

            productAdidasNitrocharge.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasPredator.Id,
                });

            productAdidasNitrocharge.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasTurfs.Id,
                });
            productAdidasNitrocharge.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productNikeKids.Id,
                });
            productChicagoBulls.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasNitrocharge.Id,
                });
            productChicagoBulls.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasPredator.Id,
                });

            productChicagoBulls.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productAdidasTurfs.Id,
                });
            productChicagoBulls.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productNikeKids.Id,
                });

            productDerbyShirt.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productChicagoBulls.Id,
                });

            productDerbyShirt.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productNikeKids.Id,
                });
            productDerbyShirt.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productPsgKit.Id,
                });
            productDerbyShirt.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productVivoactive.Id,
                });
            productSonyPS5Pad.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productBuildComputer.Id,
                });
            productSonyPS5Pad.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productLenovoIdeaPadDual.Id,
                });
            productSonyPS5Pad.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellXPS.Id,
                });
            productSonyPS5Pad.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiNotebook.Id,
                });

            productLenovoIdeaPadDual.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productBuildComputer.Id,
                });

            productLenovoIdeaPadDual.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productSonyPS5Pad.Id,
                });

            productLenovoIdeaPadDual.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productDellXPS.Id,
                });

            productLenovoIdeaPadDual.RelatedProducts.Add(
                new RelatedProduct
                {
                    ProductId2 = productMiNotebook.Id,
                });


            #endregion

            #region Product Tags

            //product tags
            await AddProductTag(product25GiftVoucher, "nice");
            await AddProductTag(product25GiftVoucher, "gift");
            await AddProductTag(productNikeKids, "cool");
            await AddProductTag(productNikeKids, "apparel");
            await AddProductTag(productNikeKids, "shirt");
            await AddProductTag(productMiSmartBand, "computer");
            await AddProductTag(productMiSmartBand, "cool");
            await AddProductTag(productAdidasPredator, "cool");
            await AddProductTag(productAdidasPredator, "shoes");
            await AddProductTag(productAdidasPredator, "apparel");
            await AddProductTag(productLenovoYogaDuet, "tablet");
            await AddProductTag(productLenovoYogaDuet, "awesome");
            await AddProductTag(productPs4, "computer");
            await AddProductTag(productPs4, "cool");
            await AddProductTag(productPsgKit, "cool");
            await AddProductTag(productPsgKit, "apparel");
            await AddProductTag(productPsgKit, "shirt");
            await AddProductTag(productMiNotebook, "compact");
            await AddProductTag(productMiNotebook, "awesome");
            await AddProductTag(productMiNotebook, "computer");
            await AddProductTag(productLenovoLegionY740, "compact");
            await AddProductTag(productLenovoLegionY740, "awesome");
            await AddProductTag(productLenovoLegionY740, "computer");
            await AddProductTag(productLegoFalcon, "awesome");
            await AddProductTag(productLegoFalcon, "lego");
            await AddProductTag(productLegoFalcon, "nice");
            await AddProductTag(productRedmiK30, "cell");
            await AddProductTag(productRedmiK30, "compact");
            await AddProductTag(productRedmiK30, "awesome");
            await AddProductTag(productBuildComputer, "awesome");
            await AddProductTag(productBuildComputer, "computer");
            await AddProductTag(productDerbyKit, "cool");
            await AddProductTag(productDerbyKit, "football kit");
            await AddProductTag(productAcerProjector, "projector");
            await AddProductTag(productAcerProjector, "cool");
            await AddProductTag(productSonyPS5Pad, "cool");
            await AddProductTag(productSonyPS5Pad, "computer");
            await AddProductTag(productLenovoSmartTab, "awesome");
            await AddProductTag(productLenovoSmartTab, "tablet");
            await AddProductTag(productDerbyShirt, "cool");
            await AddProductTag(productDerbyShirt, "shirt");
            await AddProductTag(productDerbyShirt, "apparel");
            await AddProductTag(productAdidasBall, "Balls");
            await AddProductTag(productAdidasBall, "awesome");
            await AddProductTag(productMikasa, "awesome");
            await AddProductTag(productMikasa, "Balls");
            await AddProductTag(productLegoHogwarts, "lego");
            await AddProductTag(productAdidasNitrocharge, "cool");
            await AddProductTag(productAdidasNitrocharge, "shoes");
            await AddProductTag(productAdidasNitrocharge, "apparel");
            await AddProductTag(productLenovoIdeaPadDual, "awesome");
            await AddProductTag(productLenovoIdeaPadDual, "tablet");
            await AddProductTag(productPs5Camera, "nice");
            await AddProductTag(productPs5Camera, "computer");
            await AddProductTag(productPs5Camera, "compact");
            await AddProductTag(productAcerNitro, "nice");
            await AddProductTag(productAcerNitro, "computer");
            await AddProductTag(productDellG5, "computer");
            await AddProductTag(productDellG5, "cool");
            await AddProductTag(productDellG5, "compact");
            await AddProductTag(productVivoactive, "apparel");
            await AddProductTag(productVivoactive, "cool");
            await AddProductTag(productChicagoBulls, "cool");
            await AddProductTag(productChicagoBulls, "sport");
            await AddProductTag(productChicagoBulls, "apparel");
            await AddProductTag(productAsusMixedReality, "game");
            await AddProductTag(productAsusMixedReality, "computer");
            await AddProductTag(productAsusMixedReality, "cool");
            await AddProductTag(productCyberpunk, "awesome");
            await AddProductTag(productCyberpunk, "digital");
            await AddProductTag(productForerunner, "apparel");
            await AddProductTag(productForerunner, "cool");
            await AddProductTag(productRedmiNote9, "awesome");
            await AddProductTag(productRedmiNote9, "compact");
            await AddProductTag(productRedmiNote9, "cell");
            await AddProductTag(productGTA, "digital");
            await AddProductTag(productGTA, "game");
            await AddProductTag(productPocoF2Pro, "awesome");
            await AddProductTag(productPocoF2Pro, "cool");
            await AddProductTag(productPocoF2Pro, "camera");
            await AddProductTag(productCod, "digital");
            await AddProductTag(productCod, "awesome");
            await AddProductTag(productLegoCity, "lego");
            await AddProductTag(productDellXPS, "awesome");
            await AddProductTag(productDellXPS, "computer");
            await AddProductTag(productDellXPS, "compact");
            await AddProductTag(productAdidasTurfs, "jeans");
            await AddProductTag(productAdidasTurfs, "cool");
            await AddProductTag(productAdidasTurfs, "apparel");
            await AddProductTag(productSpalding, "Balls");
            await AddProductTag(productSpalding, "awesome");


            #endregion

            //reviews
            var random = new Random();
            foreach (var product in allProducts)
            {
                if (product.ProductTypeId != ProductType.SimpleProduct)
                    continue;

                //only 3 of 4 products will have reviews
                if (random.Next(4) == 3)
                    continue;

                //rating from 4 to 5
                var rating = random.Next(4, 6);
                var productReview = new ProductReview
                {
                    CustomerId = defaultCustomer.Id,
                    ProductId = product.Id,
                    IsApproved = true,
                    StoreId = defaultStore.Id,
                    Title = "Some sample review",
                    ReviewText = string.Format("This sample review is for the {0}. I've been waiting for this product to be available. It is priced just right.", product.Name),
                    Rating = rating,
                    HelpfulYesTotal = 0,
                    HelpfulNoTotal = 0,
                    CreatedOnUtc = DateTime.UtcNow,

                };
                await _productReviewRepository.InsertAsync(productReview);

                product.ApprovedRatingSum = rating;
                product.ApprovedTotalReviews = product.ApprovedTotalReviews + 1;

            }
            await _productRepository.UpdateAsync(allProducts);
        }
        private async Task AddProductTag(Product product, string tag)
        {
            var productTag = _productTagRepository.Table.FirstOrDefault(pt => pt.Name == tag);
            if (productTag == null)
            {
                productTag = new ProductTag
                {
                    Name = tag,
                    SeName = SeoExtensions.GenerateSlug(tag, false, false, false),
                };

                await _productTagRepository.InsertAsync(productTag);
            }
            productTag.Count = productTag.Count + 1;
            await _productTagRepository.UpdateAsync(productTag);
            product.ProductTags.Add(productTag.Name);
            await _productRepository.UpdateAsync(product);
        }
    }
}
