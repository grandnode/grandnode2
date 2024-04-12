using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Customers.Services;

public class UserApiService : IUserApiService
{
    public UserApiService(IRepository<UserApi> userRepository, IMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    /// <summary>
    ///     Get user api by id
    /// </summary>
    /// <param name="id">id</param>
    public virtual Task<UserApi> GetUserById(string id)
    {
        return _userRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Get user api by email
    /// </summary>
    /// <param name="email">email</param>
    public virtual async Task<UserApi> GetUserByEmail(string email)
    {
        return await Task.FromResult(_userRepository.Table.FirstOrDefault(x => x.Email == email.ToLowerInvariant()));
    }

    /// <summary>
    ///     Insert user api
    /// </summary>
    /// <param name="userApi">User api</param>
    public virtual async Task InsertUserApi(UserApi userApi)
    {
        await _userRepository.InsertAsync(userApi);

        //event notification
        await _mediator.EntityInserted(userApi);
    }

    /// <summary>
    ///     Update user api
    /// </summary>
    /// <param name="userApi">User api</param>
    public virtual async Task UpdateUserApi(UserApi userApi)
    {
        await _userRepository.UpdateAsync(userApi);

        //event notification
        await _mediator.EntityUpdated(userApi);
    }

    /// <summary>
    ///     Delete user api
    /// </summary>
    /// <param name="userApi">User api</param>
    public virtual async Task DeleteUserApi(UserApi userApi)
    {
        await _userRepository.DeleteAsync(userApi);

        //event notification
        await _mediator.EntityDeleted(userApi);
    }

    /// <summary>
    ///     Get users api
    /// </summary>
    /// <param name="email"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    public virtual async Task<IPagedList<UserApi>> GetUsers(string email = "", int pageIndex = 0,
        int pageSize = 2147483647)
    {
        var query = from p in _userRepository.Table
            select p;

        if (!string.IsNullOrEmpty(email))
            query = query.Where(x => x.Email.Contains(email.ToLowerInvariant()));

        return await PagedList<UserApi>.Create(query, pageIndex, pageSize);
    }

    #region Fields

    private readonly IRepository<UserApi> _userRepository;
    private readonly IMediator _mediator;

    #endregion
}