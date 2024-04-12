using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Marketing.Extensions;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;

namespace Grand.Business.Marketing.Services.Newsletters;

/// <summary>
///     Newsletter subscription service
/// </summary>
public class NewsLetterSubscriptionService : INewsLetterSubscriptionService
{
    #region Ctor

    public NewsLetterSubscriptionService(
        IRepository<NewsLetterSubscription> subscriptionRepository,
        IMediator mediator,
        IHistoryService historyService)
    {
        _subscriptionRepository = subscriptionRepository;
        _mediator = mediator;
        _historyService = historyService;
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Publishes the subscription event.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <param name="isSubscribe">if set to <c>true</c> [is subscribe].</param>
    /// <param name="publishSubscriptionEvents">if set to <c>true</c> [publish subscription events].</param>
    private async Task PublishSubscriptionEvent(string email, bool isSubscribe, bool publishSubscriptionEvents)
    {
        if (publishSubscriptionEvents)
        {
            if (isSubscribe)
                await _mediator.PublishNewsletterSubscribe(email);
            else
                await _mediator.PublishNewsletterUnsubscribe(email);
        }
    }

    #endregion

    #region Fields

    private readonly IMediator _mediator;
    private readonly IRepository<NewsLetterSubscription> _subscriptionRepository;
    private readonly IHistoryService _historyService;

    #endregion

    #region Methods

    /// <summary>
    ///     Inserts a newsletter subscription
    /// </summary>
    /// <param name="newsLetterSubscription">NewsLetter subscription</param>
    /// <param name="publishSubscriptionEvents">if set to <c>true</c> [publish subscription events].</param>
    public virtual async Task InsertNewsLetterSubscription(NewsLetterSubscription newsLetterSubscription,
        bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);

        //Handle e-mail
        newsLetterSubscription.Email = CommonHelper.EnsureSubscriberEmailOrThrow(newsLetterSubscription.Email);

        //Persist
        await _subscriptionRepository.InsertAsync(newsLetterSubscription);

        //Publish the subscription event 
        if (newsLetterSubscription.Active)
            await PublishSubscriptionEvent(newsLetterSubscription.Email, true, publishSubscriptionEvents);

        //save history
        await newsLetterSubscription.SaveHistory<NewsLetterSubscription>(_historyService);

        //Publish event
        await _mediator.EntityInserted(newsLetterSubscription);
    }

    /// <summary>
    ///     Updates a newsletter subscription
    /// </summary>
    /// <param name="newsLetterSubscription">NewsLetter subscription</param>
    /// <param name="publishSubscriptionEvents">if set to <c>true</c> [publish subscription events].</param>
    public virtual async Task UpdateNewsLetterSubscription(NewsLetterSubscription newsLetterSubscription,
        bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);

        //Handle e-mail
        newsLetterSubscription.Email = CommonHelper.EnsureSubscriberEmailOrThrow(newsLetterSubscription.Email);

        //get previous newsLetterSubscription record
        var prevNewsLetterSubscription = await _subscriptionRepository.GetByIdAsync(newsLetterSubscription.Id);

        //Persist
        await _subscriptionRepository.UpdateAsync(newsLetterSubscription);

        //save history
        await newsLetterSubscription.SaveHistory<NewsLetterSubscription>(_historyService);

        //Publish the un/subscribe event 
        if (prevNewsLetterSubscription != null)
            switch (newsLetterSubscription.Active)
            {
                case true when !prevNewsLetterSubscription.Active:
                    await PublishSubscriptionEvent(newsLetterSubscription.Email, true, publishSubscriptionEvents);
                    break;
                case false when prevNewsLetterSubscription.Active:
                    await PublishSubscriptionEvent(newsLetterSubscription.Email, false, publishSubscriptionEvents);
                    break;
            }

        //Publish event
        await _mediator.EntityUpdated(newsLetterSubscription);
    }

    /// <summary>
    ///     Deletes a newsletter subscription
    /// </summary>
    /// <param name="newsLetterSubscription">NewsLetter subscription</param>
    /// <param name="publishSubscriptionEvents">if set to <c>true</c> [publish subscription events].</param>
    public virtual async Task DeleteNewsLetterSubscription(NewsLetterSubscription newsLetterSubscription,
        bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);

        await _subscriptionRepository.DeleteAsync(newsLetterSubscription);

        //Publish the unsubscribe event 
        await PublishSubscriptionEvent(newsLetterSubscription.Email, false, publishSubscriptionEvents);

        //event notification
        await _mediator.EntityDeleted(newsLetterSubscription);
    }

    /// <summary>
    ///     Gets a newsletter subscription by newsletter subscription identifier
    /// </summary>
    /// <param name="newsLetterSubscriptionId">The newsletter subscription identifier</param>
    /// <returns>NewsLetter subscription</returns>
    public virtual Task<NewsLetterSubscription> GetNewsLetterSubscriptionById(string newsLetterSubscriptionId)
    {
        return _subscriptionRepository.GetByIdAsync(newsLetterSubscriptionId);
    }

    /// <summary>
    ///     Gets a newsletter subscription by newsletter subscription GUID
    /// </summary>
    /// <param name="newsLetterSubscriptionGuid">The newsletter subscription GUID</param>
    /// <returns>NewsLetter subscription</returns>
    public virtual async Task<NewsLetterSubscription> GetNewsLetterSubscriptionByGuid(Guid newsLetterSubscriptionGuid)
    {
        if (newsLetterSubscriptionGuid == Guid.Empty) return null;

        var newsLetterSubscriptions = from nls in _subscriptionRepository.Table
            where nls.NewsLetterSubscriptionGuid == newsLetterSubscriptionGuid
            select nls;

        return await Task.FromResult(newsLetterSubscriptions.FirstOrDefault());
    }

    /// <summary>
    ///     Gets a newsletter subscription by email and store ID
    /// </summary>
    /// <param name="email">The newsletter subscription email</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>NewsLetter subscription</returns>
    public virtual async Task<NewsLetterSubscription> GetNewsLetterSubscriptionByEmailAndStoreId(string email,
        string storeId)
    {
        if (!CommonHelper.IsValidEmail(email))
            return null;

        email = email.Trim();

        var newsLetterSubscriptions = from nls in _subscriptionRepository.Table
            where nls.Email.ToLower() == email.ToLower() && nls.StoreId == storeId
            select nls;

        return await Task.FromResult(newsLetterSubscriptions.FirstOrDefault());
    }

    /// <summary>
    ///     Gets a newsletter subscription by customer ID
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <returns>NewsLetter subscription</returns>
    public virtual async Task<NewsLetterSubscription> GetNewsLetterSubscriptionByCustomerId(string customerId)
    {
        if (string.IsNullOrEmpty(customerId))
            return null;

        var newsLetterSubscriptions = from nls in _subscriptionRepository.Table
            where nls.CustomerId == customerId
            select nls;

        return await Task.FromResult(newsLetterSubscriptions.FirstOrDefault());
    }

    /// <summary>
    ///     Gets the newsletter subscription list
    /// </summary>
    /// <param name="email">Email to search or string. Empty to load all records.</param>
    /// <param name="storeId">Store identifier. "" to load all records.</param>
    /// <param name="isActive">Value indicating whether subscriber record should be active or not; null to load all records</param>
    /// <param name="categoryIds"></param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>NewsLetterSubscription entities</returns>
    public virtual async Task<IPagedList<NewsLetterSubscription>> GetAllNewsLetterSubscriptions(string email = null,
        string storeId = "", bool? isActive = null, string[] categoryIds = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        //do not filter by customer group
        var query = from p in _subscriptionRepository.Table
            select p;

        if (!string.IsNullOrEmpty(email))
            query = query.Where(nls => nls.Email.ToLower().Contains(email.ToLower()));
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(nls => nls.StoreId == storeId);
        if (isActive.HasValue)
            query = query.Where(nls => nls.Active == isActive.Value);
        if (categoryIds is { Length: > 0 })
            query = query.Where(c => c.Categories.Any(x => categoryIds.Contains(x)));

        query = query.OrderBy(nls => nls.Email);
        return await PagedList<NewsLetterSubscription>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Export newsletter subscribers to TXT
    /// </summary>
    /// <param name="subscriptions">Subscriptions</param>
    /// <returns>Result in TXT (string) format</returns>
    public virtual string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        const char separator = ',';
        var sb = new StringBuilder();
        foreach (var subscription in subscriptions)
        {
            sb.Append(subscription.Email);
            sb.Append(separator);
            sb.Append(subscription.Active);
            sb.Append(separator);
            sb.Append(subscription.CreatedOnUtc.ToString("dd.MM.yyyy HH:mm:ss"));
            sb.Append(separator);
            sb.Append(subscription.StoreId);
            sb.Append(separator);
            sb.Append(string.Join(';', subscription.Categories));
            sb.Append(Environment.NewLine); //new line
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Import newsletter subscribers from TXT file
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="currentStoreId">Current store ident</param>
    /// <returns>Number of imported subscribers</returns>
    public virtual async Task<int> ImportNewsletterSubscribersFromTxt(Stream stream, string currentStoreId)
    {
        var count = 0;
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var tmp = line.Split(',');

            string email;
            var isActive = true;
            var categories = new List<string>();
            var isCategories = false;
            var storeId = currentStoreId;
            switch (tmp.Length)
            {
                //parse
                case 1:
                    //"email" only
                    email = tmp[0].Trim();
                    break;
                case 2:
                    //"email" and "active" fields specified
                    email = tmp[0].Trim();
                    isActive = bool.Parse(tmp[1].Trim());
                    break;
                case 3:
                    //"email" and "active" and "storeId" fields specified
                    email = tmp[0].Trim();
                    isActive = bool.Parse(tmp[1].Trim());
                    storeId = tmp[2].Trim();
                    break;
                case 4:
                    //"email" and "active" and "storeId" and categories fields specified
                    email = tmp[0].Trim();
                    isActive = bool.Parse(tmp[1].Trim());
                    storeId = tmp[2].Trim();
                    try
                    {
                        var items = tmp[3].Trim().Split(';').ToList();
                        categories.AddRange(items.Where(item => !string.IsNullOrEmpty(item)));
                        isCategories = true;
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                default:
                    throw new GrandException("Wrong file format");
            }

            //import
            await ImportSubscription(email, storeId, isActive, isCategories, categories);

            count++;
        }

        return count;
    }

    protected virtual async Task ImportSubscription(string email, string storeId, bool isActive, bool isCategories,
        List<string> categories)
    {
        var subscription = await GetNewsLetterSubscriptionByEmailAndStoreId(email, storeId);
        if (subscription != null)
        {
            subscription.Email = email;
            subscription.Active = isActive;
            if (isCategories)
            {
                subscription.Categories.Clear();
                foreach (var item in categories) subscription.Categories.Add(item);
            }

            await UpdateNewsLetterSubscription(subscription);
        }
        else
        {
            subscription = new NewsLetterSubscription {
                Active = isActive,
                Email = email,
                StoreId = storeId,
                NewsLetterSubscriptionGuid = Guid.NewGuid()
            };
            foreach (var item in categories) subscription.Categories.Add(item);
            await InsertNewsLetterSubscription(subscription);
        }
    }

    #endregion
}