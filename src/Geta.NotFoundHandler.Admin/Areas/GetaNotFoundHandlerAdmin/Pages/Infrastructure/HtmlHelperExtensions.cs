using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Infrastructure
{
    public static class HtmlHelperExtensions
    {
        private static readonly ConcurrentDictionary<(Type, string), string> QueryNameDictionary = new();

        public static IHtmlContent HiddenForQueryParams<T>(this IHtmlHelper<T> htmlHelper)
            where T : BaseRedirectPageModel
        {
            var htmlContentBuilder = new HtmlContentBuilder();
            htmlContentBuilder.AppendHiddenInputFor(htmlHelper, x => x.Params.QueryText);
            htmlContentBuilder.AppendHiddenInputFor(htmlHelper, x => x.Params.Page);
            htmlContentBuilder.AppendHiddenInputFor(htmlHelper, x => x.Params.SortBy);
            htmlContentBuilder.AppendHiddenInputFor(htmlHelper, x => x.Params.SortDirection);
            return htmlContentBuilder;
        }

        public static string NameQueryFor<TModel, TResult>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression)
        {
            ArgumentNullException.ThrowIfNull(html);
            ArgumentNullException.ThrowIfNull(expression);

            return QueryNameDictionary.GetOrAdd((typeof(TModel), expression.ToString()), _ =>
            {
                var modelExpression = html.ViewContext.HttpContext.RequestServices.GetRequiredService<ModelExpressionProvider>().CreateModelExpression(html.ViewData, expression);

                if (modelExpression.Metadata is DefaultModelMetadata defaultMetadata && defaultMetadata.Attributes.Attributes.OfType<IModelNameProvider>().FirstOrDefault() is IModelNameProvider attribute
                    && attribute.Name is string queryName && !string.IsNullOrEmpty(queryName))
                {
                    return queryName;
                }

                return html.NameFor(expression);
            });
        }

        private static void AppendHiddenInputFor<TModel, TResult>(this IHtmlContentBuilder htmlContentBuilder, IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression)
        {
            htmlContentBuilder.AppendHtml(htmlHelper.Hidden(htmlHelper.NameQueryFor(expression), htmlHelper.DisplayTextFor(expression)));
        }
    }
}
