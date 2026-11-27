using System.Diagnostics.CodeAnalysis;
using Grand.SharedKernel.Attributes;

namespace Grand.Domain;

public abstract class ParentEntity
{
    private string _id;

    protected ParentEntity()
    {
        _id = UniqueIdentifier.New;
    }

    [DBFieldName("_id")]
    //the setter turns null into a new identifier, so the getter never returns null
    [AllowNull]
    public string Id {
        get => _id;
        set => _id = string.IsNullOrEmpty(value) ? UniqueIdentifier.New : value;
    }   
}