using System;

namespace Geta.NotFoundHandler.Data
{
    public interface ISuggestionRepository
    {
        void DeleteAll();
        void Delete(int maxErrors, int minimumDaysOld);
        void DeleteForRequest(string oldUrl);
        void Save(string oldUrl, string referer, DateTime requestedOn);
    }
}
