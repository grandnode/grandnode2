using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     An entity the panel may work on, or the response that refuses it: a missing entity and one the panel may
///     not see are answered alike
/// </summary>
public sealed class Access<T> where T : class
{
    private Access(T? entity, IActionResult? denied)
    {
        Entity = entity;
        Denied = denied;
        IsDenied = denied != null;
    }

    public T? Entity { get; }

    public IActionResult? Denied { get; }

    //made only by Allow or Deny, so exactly one of the two is set
    [MemberNotNullWhen(false, nameof(Entity))]
    [MemberNotNullWhen(true, nameof(Denied))]
    public bool IsDenied { get; }

    public static Access<T> Allow(T entity) => new(entity, null);

    public static Access<T> Deny(IActionResult denied) => new(null, denied);

    public bool TryGet([NotNullWhen(true)] out T? entity, [NotNullWhen(false)] out IActionResult? denied)
    {
        entity = Entity;
        denied = Denied;
        return !IsDenied;
    }
}
