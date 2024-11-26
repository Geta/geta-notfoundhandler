using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Extensions;

public static class ModelStateExtensions
{
    public static ModelStateDictionary RemoveNestedKeys(this ModelStateDictionary source, string prefix)
    {
        foreach (var key in source.Keys)
        {
            if (key.StartsWith($"{prefix}."))
            {
                source.Remove(key);
            }
        }

        return source;
    }
}
