using System.IO;

namespace Geta.NotFoundHandler.Core.Redirects;

public interface IRedirectsParser
{
    CustomRedirectCollection LoadFromStream(Stream xmlContent);
}
