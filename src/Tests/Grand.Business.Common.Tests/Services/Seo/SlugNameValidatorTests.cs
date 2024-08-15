using Moq;
using NUnit.Framework;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using NUnit.Framework.Legacy;

namespace Grand.Business.Common.Tests.Services.Seo
{
    public class SlugNameValidatorTests
    {
        private Mock<ISlugService> _mockSlugService;
        private Mock<ILanguageService> _mockLanguageService;
        private SeoSettings _seoSettings;
        private SlugNameValidator _slugNameValidator;

        [SetUp]
        public void Setup()
        {
            _mockSlugService = new Mock<ISlugService>();
            _mockLanguageService = new Mock<ILanguageService>();
            _mockLanguageService.Setup(l => l.GetAllLanguages(true, "")).ReturnsAsync(new List<Language>());
            _seoSettings = new SeoSettings {
                ReservedEntityUrlSlugs = new List<string> { "reserved-slug" },
                ConvertNonWesternChars = false,
                AllowUnicodeCharsInUrls = false,
                AllowSlashChar = false,
                SeoCharConversion = null
            };
            _slugNameValidator = new SlugNameValidator(_mockSlugService.Object, _mockLanguageService.Object, _seoSettings);
        }

        [Test]
        public async Task ValidateSeName_ShouldReturnName_WhenSeNameIsEmpty()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            string seName = "";
            string name = "test-name";
            bool ensureNotEmpty = false;

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, ensureNotEmpty);

            // Assert
            ClassicAssert.AreEqual("test-name", result);
        }

        [Test]
        public async Task ValidateSeName_ShouldReturnEntityId_WhenSeNameIsEmptyAndEnsureNotEmptyTrue()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            string seName = "";
            string name = "";
            bool ensureNotEmpty = true;

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, ensureNotEmpty);

            // Assert
            ClassicAssert.AreEqual("123", result);
        }

        [Test]
        public async Task ValidateSeName_ShouldAppendNumber_WhenSlugIsReserved()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            string seName = "reserved-slug";
            string name = "test-name";
            bool ensureNotEmpty = false;

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "123", EntityName = "TestEntity" });
            _mockLanguageService.Setup(l => l.GetAllLanguages(true, "")).ReturnsAsync(new List<Language>());

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, ensureNotEmpty);

            // Assert
            ClassicAssert.AreEqual("reserved-slug-1", result);
        }
        [Test]
        public async Task ValidateSeName_SeNameInLanguageSeoCode_ReturnsUniqueSlug()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            var name = "Entity Name";
            var seName = "en"; // Assuming this is a reserved SEO code in languages

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "123", EntityName = "TestEntity" });

            _mockLanguageService.Setup(l => l.GetAllLanguages(true, ""))
                .ReturnsAsync(new[] { new Language { UniqueSeoCode = "en" } });

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, true);

            // Assert
            ClassicAssert.IsTrue(result.StartsWith("en-"));
        }
        
        [Test]
        public async Task ValidateSeName_SeNameTooLong_TruncatesToMaxLength()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            var name = "Entity Name";
            var seName = new string('a', 250); // Longer than the max length of 200

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "123", EntityName = "TestEntity" });

            _mockLanguageService.Setup(l => l.GetAllLanguages(true, ""))
                .ReturnsAsync(new[] { new Language { UniqueSeoCode = "en" } });

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, true);

            // Assert
            ClassicAssert.AreEqual(seName.Substring(0, 200), result);
        }
        [Test]
        public async Task ValidateSeName_NoReservedSlug_ReturnsOriginalSlug()
        {
            // Arrange
            var entity = new TestEntity { Id = "123" };
            var name = "Entity Name";
            var seName = "unique-slug";

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "123", EntityName = "TestEntity" });

            _mockLanguageService.Setup(l => l.GetAllLanguages(true, ""))
                .ReturnsAsync(new[] { new Language { UniqueSeoCode = "en" } });

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, true);

            // Assert
            ClassicAssert.AreEqual("unique-slug", result);
        }
        [Test]
        public async Task ValidateSeName_ReservedSlug_OverLimit_ReturnsSlugWithGuid()
        {
            // Arrange
            var entity = new TestEntity { Id = "1" };
            var name = "Entity Name";
            var seName = "reserved-slug";

            _mockSlugService.SetupSequence(s => s.GetBySlug(It.IsAny<string>()))
                .ReturnsAsync(new EntityUrl { EntityId = "2", EntityName = "OtherEntity" })
                .ReturnsAsync(new EntityUrl { EntityId = "2", EntityName = "OtherEntity" })
                .ReturnsAsync(new EntityUrl { EntityId = "2", EntityName = "OtherEntity" })
                .ReturnsAsync(new EntityUrl { EntityId = "2", EntityName = "OtherEntity" })
                .ReturnsAsync(new EntityUrl { EntityId = "2", EntityName = "OtherEntity" });

            // Act
            var result = await _slugNameValidator.ValidateSeName(entity, seName, name, true);

            // Assert
            ClassicAssert.IsTrue(result.StartsWith("reserved-slug-") && result.Length > "reserved-slug-4".Length);
        }
        
        class TestEntity : BaseEntity, ISlugEntity
        {
            // Implement necessary members of ISlugEntity if needed
            public string SeName { get; set; }
        }
    }
}