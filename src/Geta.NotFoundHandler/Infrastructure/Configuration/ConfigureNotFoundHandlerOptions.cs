using System;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Infrastructure.Configuration;

public class ConfigureNotFoundHandlerOptions : IConfigureOptions<NotFoundHandlerOptions>
{
    private readonly Action<NotFoundHandlerOptions> _setupAction;

    public ConfigureNotFoundHandlerOptions(Action<NotFoundHandlerOptions> setupAction)
    {
        _setupAction = setupAction;
    }
    
    public void Configure(NotFoundHandlerOptions options)
    {
        _setupAction?.Invoke(options);
    }
}
