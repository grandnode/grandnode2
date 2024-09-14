using Moq;
using NUnit.Framework;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Infrastructure.Models;
using NUnit.Framework.Legacy;
using System.Linq.Expressions;

namespace Grand.Business.Common.Tests.Services.Seo
{
    public class SeNameServiceTests
    {
        private Mock<ISlugService> _mockSlugService;
        private Mock<ILanguageService> _mockLanguageService;
        private SeoSettings _seoSettings;
        private SeNameService _seNameService;

        [SetUp]
        public void Setup()
        {
            _mockSlugService = new Mock<ISlugService>();
            _mockLanguageService = new Mock<ILanguageService>();
            _mockLanguageService.Setup(l => l.GetAllLanguages(true, "")).ReturnsAsync(new List<Language>());
            _seoSettings = new SeoSettings {
                ReservedEntityUrlSlugs = ["reserved-slug"],
                ConvertNonWesternChars = false,
                AllowUnicodeCharsInUrls = false,
                AllowSlashChar = false,
                SeoCharConversion = null
            };
            _seNameService = new SeNameService(_mockSlugService.Object, _mockLanguageService.Object, _seoSettings);
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
            var result = await _seNameService.ValidateSeName(entity, seName, name, ensureNotEmpty);

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
            var result = await _seNameService.ValidateSeName(entity, seName, name, ensureNotEmpty);

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
            var result = await _seNameService.ValidateSeName(entity, seName, name, ensureNotEmpty);

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
                .ReturnsAsync([new Language { UniqueSeoCode = "en" }]);

            // Act
            var result = await _seNameService.ValidateSeName(entity, seName, name, true);

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
                .ReturnsAsync([new Language { UniqueSeoCode = "en" }]);

            // Act
            var result = await _seNameService.ValidateSeName(entity, seName, name, true);

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
                .ReturnsAsync([new Language { UniqueSeoCode = "en" }]);

            // Act
            var result = await _seNameService.ValidateSeName(entity, seName, name, true);

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
            var result = await _seNameService.ValidateSeName(entity, seName, name, true);

            // Assert
            ClassicAssert.IsTrue(result.StartsWith("reserved-slug-") && result.Length > "reserved-slug-4".Length);
        }
        
        [Test]
        public async Task TranslationSeNameProperties_ShouldReturnEmptyList_WhenInputListIsEmpty()
        {
            // Arrange
            var list = new List<LocalizedModelLocal>();
            var entity = new TestEntity { Id = "1" };
            Expression<Func<LocalizedModelLocal, string>> keySelector = x => x.SomeProperty;

            // Act
            var result = await _seNameService.TranslationSeNameProperties(list, entity, keySelector);

            // Assert
            ClassicAssert.IsEmpty(result);
        }
        [Test]
        public async Task TranslationSeNameProperties_ShouldAddTranslationEntity_WhenPropertyIsValid()
        {
            // Arrange
            var list = new List<LocalizedModelLocal>
            {
                new LocalizedModelLocal { SomeProperty = "SomeValue", LanguageId = "1" }
            };
            var entity = new TestEntity { Id = "1" };
            var seName = "slug";
            Expression<Func<LocalizedModelLocal, string>> keySelector = x => x.SomeProperty;

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "123", EntityName = "TestEntity" });


            // Act
            var result = await _seNameService.TranslationSeNameProperties(list, entity, keySelector);

            // Assert
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.AreEqual("1", result[0].LanguageId);
            ClassicAssert.AreEqual("SomeProperty", result[0].LocaleKey);
            ClassicAssert.AreEqual("SomeValue", result[0].LocaleValue);
        }
        [Test]
        public async Task TranslationSeNameProperties_ShouldAddTranslationEntity_AddSlug_WhenPropertyIsValid()
        {
            // Arrange
            var list = new List<LocalizedSlugModelLocal>
            {
                new LocalizedSlugModelLocal { SomeProperty = "SomeValue", LanguageId = "1" }
            };
            
            var entity = new TestTransEntity { Id = "1" };
            var seName = "slug";
            Expression<Func<LocalizedSlugModelLocal, string>> keySelector = x => x.SomeProperty;

            _mockSlugService.Setup(s => s.GetBySlug(It.IsAny<string>())).ReturnsAsync(new EntityUrl() { Slug = seName, EntityId = "1", EntityName = "TestTransEntity" });


            // Act
            var result = await _seNameService.TranslationSeNameProperties(list, entity, keySelector);

            // Assert
            ClassicAssert.AreEqual(2, result.Count);
            ClassicAssert.AreEqual("1", result[0].LanguageId);
            ClassicAssert.AreEqual("SomeProperty", result[0].LocaleKey);
            ClassicAssert.AreEqual("SomeValue", result[0].LocaleValue);
            
            ClassicAssert.AreEqual("1", result[1].LanguageId);
            ClassicAssert.AreEqual("SeName", result[1].LocaleKey);
            ClassicAssert.AreEqual("somevalue", result[1].LocaleValue);
        }
        
        [Test]
        public async Task SaveSeName_ShouldCallSaveSlugOnce_WhenEntityHasNoLocales()
        {
            // Arrange
            var entity = new TestTransEntity { Id = "1", SeName = "test-se-name"};

            // Act
            await _seNameService.SaveSeName(entity);

            // Assert
            _mockSlugService.Verify(x => x.SaveSlug(entity, "test-se-name", ""), Times.Once);
        }
        [Test]
        public async Task SaveSeName_ShouldCallSaveSlugMultipleTimes_WhenEntityHasLocalesWithDifferentSeName()
        {
            // Arrange
            var entity = new TestTransEntity { Id = "1", SeName = "main-se-name"};
            var locale1 = new TranslationEntity
            {
                LocaleKey = nameof(ISlugEntity.SeName),
                LocaleValue = "locale1-se-name",
                LanguageId = "en"
            };
            entity.Locales.Add(locale1);
            var locale2 = new TranslationEntity
            {
                LocaleKey = nameof(ISlugEntity.SeName),
                LocaleValue = "locale2-se-name",
                LanguageId = "fr"
            };
            entity.Locales.Add(locale2);

            // Act
            await _seNameService.SaveSeName(entity);

            // Assert
            _mockSlugService.Verify(x => x.SaveSlug(entity, "main-se-name", ""), Times.Once);
            _mockSlugService.Verify(x => x.SaveSlug(entity, "locale1-se-name", "en"), Times.Once);
            _mockSlugService.Verify(x => x.SaveSlug(entity, "locale2-se-name", "fr"), Times.Once);
        }
        class LocalizedModelLocal : ILocalizedModelLocal
        {
            public string LanguageId { get; set; }
            public string SomeProperty { get; set; }
        }
        
        class LocalizedSlugModelLocal : ILocalizedModelLocal, ISlugModelLocal
        {
            public string LanguageId { get; set; }
            public string SomeProperty { get; set; }
            public string SeName { get; set; }
        }
        class TestEntity : BaseEntity, ISlugEntity
        {
            // Implement necessary members of ISlugEntity if needed
            public string SeName { get; set; }
        }
        class TestTransEntity : BaseEntity, ISlugEntity, ITranslationEntity
        {
            public string SeName { get; set; }
            public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
        }
    }
}