using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeader
{
    public class SortableHeaderViewModel
    {
        public SortableHeaderViewModel(string displayName, string internalName, QueryParams @params, string additionalClass = null)
        {
            DisplayName = displayName;
            InternalName = internalName;
            Params = @params;
            AdditionalClass = additionalClass;
        }

        public string DisplayName { get; init; }

        public string InternalName { get; init; }

        public QueryParams Params { get; init; }

        public string AdditionalClass { get; }
    }
}
