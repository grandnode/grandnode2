// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Globalization;

namespace Grand.Web.Common.TagHelpers.Admin.Extend;

internal static class ExpressionMetadataProvider
{
    /// <summary>
    ///     Gets <see cref="ModelExplorer" /> for named <paramref name="expression" /> in given
    ///     <paramref name="viewData" />.
    /// </summary>
    /// <param name="expression">Expression name, relative to <c>viewData.Model</c>.</param>
    /// <param name="viewData">
    ///     The <see cref="ViewDataDictionary" /> that may contain the <paramref name="expression" /> value.
    /// </param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider" />.</param>
    /// <returns>
    ///     <see cref="ModelExplorer" /> for named <paramref name="expression" /> in given <paramref name="viewData" />.
    /// </returns>
    public static ModelExplorer FromStringExpression(
        string expression,
        ViewDataDictionary viewData,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        var viewDataInfo = ViewDataEvaluator.Eval(viewData, expression);
        if (viewDataInfo == null)
        {
            // Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
            var propertyExplorer = viewData.ModelExplorer.GetExplorerForProperty(expression);
            if (propertyExplorer != null) return propertyExplorer;
        }

        if (viewDataInfo != null)
        {
            if (viewDataInfo.Container == viewData &&
                viewDataInfo.Value == viewData.Model &&
                string.IsNullOrEmpty(expression))
                // Nothing for empty expression in ViewData and ViewDataEvaluator just returned the model. Handle
                // using FromModel() for its object special case.
                return FromModel(viewData, metadataProvider);

            var containerExplorer = viewData.ModelExplorer;
            if (viewDataInfo.Container != null)
                containerExplorer = metadataProvider.GetModelExplorerForType(
                    viewDataInfo.Container.GetType(),
                    viewDataInfo.Container);

            if (viewDataInfo.PropertyInfo != null)
            {
                // We've identified a property access, which provides us with accurate metadata.
                var containerMetadata = metadataProvider.GetMetadataForType(viewDataInfo.Container.GetType());
                var propertyMetadata = containerMetadata.Properties[viewDataInfo.PropertyInfo.Name];

                Func<object, object> modelAccessor = ignore => viewDataInfo.Value;
                return containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor);
            }

            if (viewDataInfo.Value != null)
            {
                // We have a value, even though we may not know where it came from.
                var valueMetadata = metadataProvider.GetMetadataForType(viewDataInfo.Value.GetType());
                return containerExplorer.GetExplorerForExpression(valueMetadata, viewDataInfo.Value);
            }
        }

        // Treat the expression as string if we don't find anything better.
        var stringMetadata = metadataProvider.GetMetadataForType(typeof(string));
        return viewData.ModelExplorer.GetExplorerForExpression(stringMetadata, null);
    }

    private static ModelExplorer FromModel(
        ViewDataDictionary viewData,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        if (viewData.ModelMetadata.ModelType == typeof(object))
        {
            // Use common simple type rather than object so e.g. Editor() at least generates a TextBox.
            var model = viewData.Model == null ? null : Convert.ToString(viewData.Model, CultureInfo.CurrentCulture);
            return metadataProvider.GetModelExplorerForType(typeof(string), model);
        }

        return viewData.ModelExplorer;
    }
}