﻿using Grand.Domain.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Infrastructure.Models;

/// <summary>
///     Represents base model
/// </summary>
public class BaseModel
{
    #region Ctor

    public BaseModel()
    {
        UserFields = [];
    }

    #endregion

    #region Properties

    public IList<UserField> UserFields { get; set; }

    #endregion

    #region Methods

    public virtual void BindModel(ModelBindingContext bindingContext)
    {
    }
    #endregion
}