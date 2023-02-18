using System;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.NotFoundHandler.Infrastructure.Web
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromFormOrQueryAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
    {
        public BindingSource BindingSource => CompositeBindingSource.Create(
            new[] { BindingSource.Form, BindingSource.Query }, nameof(FromFormOrQueryAttribute));

        public string Name { get; set; }
    }
}
