using Grand.Domain;
using System.Linq.Expressions;

namespace Grand.Data;

/// <summary>
///     Repository
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    ///     Gets a table
    /// </summary>
    IQueryable<T> Table { get; }
   

    /// <summary>
    ///     Get entity by identifier
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns>Entity</returns>
    T GetById(string id);

    /// <summary>
    ///     Get async entity by identifier
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns>Entity</returns>
    Task<T> GetByIdAsync(string id);

    /// <summary>
    ///     Get entity by identifier
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns>Entity</returns>
    Task<T> GetOneAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    ///     Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    T Insert(T entity);

    /// <summary>
    ///     Async Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    Task<T> InsertAsync(T entity);

    /// <summary>
    ///     Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    T Update(T entity);

    /// <summary>
    ///     Async Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    ///     Update field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    Task UpdateField<U>(string id, Expression<Func<T, U>> expression, U value);

    /// <summary>
    ///     Inc field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    Task IncField<U>(string id, Expression<Func<T, U>> expression, U value);

    /// <summary>
    ///     Updates a single entity
    /// </summary>
    /// <param name="filterexpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    Task UpdateOneAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder);

    /// <summary>
    ///     Updates a many entities
    /// </summary>
    /// <param name="filterexpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder);

    /// <summary>
    ///     Add to set - add subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task AddToCollectionField<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value);

    /// <summary>
    ///     Update subdocument
    /// </summary>
    /// <typeparam name="U">Document</typeparam>
    /// <param name="id">Ident of entitie</param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch">Subdocument predicate to match</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    /// <returns></returns>
    Task UpdateCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, bool>> elemFieldMatch,
        U value);

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch"></param>
    /// <returns></returns>
    Task RemoveCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, bool>> elemFieldMatch);

    /// <summary>
    ///     Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    void Delete(T entity);

    /// <summary>
    ///     Async Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    Task<T> DeleteAsync(T entity);

    /// <summary>
    ///     Async Delete entities
    /// </summary>
    /// <param name="entities">Entities</param>
    Task DeleteAsync(IEnumerable<T> entities);

    /// <summary>
    ///     Delete a many entities
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression);

    /// <summary>
    ///     Clear entities
    /// </summary>
    Task ClearAsync();

    /// <summary>
    ///     Gets a table collection
    /// </summary>
    IQueryable<C> TableCollection<C>() where C : class;    
}