using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Commands.System.Common;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using MediatR;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    /// <summary>
    /// Represents a task end auctions
    /// </summary>
    public partial class GenerateSitemapXmlTask : IScheduleTask
    {
        private readonly IMediator _mediator;
        private readonly ILanguageService _languageService;
        private readonly IStoreService _storeService;
        private readonly IMediaFileStore _mediaFileStore;

        public GenerateSitemapXmlTask(IMediator mediator,
            ILanguageService languageService,
            IStoreService storeService,
            IMediaFileStore mediaFileStore)
        {
            _mediator = mediator;
            _languageService = languageService;
            _storeService = storeService;
            _mediaFileStore = mediaFileStore;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public async Task Execute()
        {
            var stores = await _storeService.GetAllStores();
            var languages = await _languageService.GetAllLanguages();

            var file = "sitemap.xml";

            foreach (var store in stores)
            {
                var storelanguages = await _languageService.GetAllLanguages(storeId: store.Id);
                foreach (var lang in storelanguages)
                {
                    var siteMap = await _mediator.Send(new GetSitemapXmlCommand()
                    {
                        Language = lang,
                        Store = store,
                    });

                    if (!(stores.Count == 1 && languages.Count == 1))
                        file = $"sitemap-{store.Shortcut}-{lang.UniqueSeoCode}.xml";

                    if (stores.Count == 1 && languages.Count > 1)
                        file = $"sitemap-{lang.UniqueSeoCode}.xml";

                    await _mediaFileStore.WriteAllText(file, siteMap);
                }
            }
        }
    }
}