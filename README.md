
# NotFound Handler for ASP.NET Core and EPiServer

## Description

![](http://tc.geta.no/app/rest/builds/buildType:(id:GetaPackages_Geta404Handler_00ci),branch:master/statusIcon)
[![Platform](https://img.shields.io/badge/Platform-.NET%204.6.1-blue.svg?style=flat)](https://msdn.microsoft.com/en-us/library/w0x726c2%28v=vs.110%29.aspx)
[![Platform](https://img.shields.io/badge/Episerver%20-%2011-orange.svg?style=flat)](https://world.episerver.com/cms/)

The popular NotFound handler for ASP.NET Core and EPiServer, enabling better control over your 404 page in addition to allowing redirects for old URLs that no longer works.

The perfect companion if you're transitioning to your site from another system and cannot keep the URL structure, or plan to do major re-structuring of your content.

![](https://raw.githubusercontent.com/Geta/geta-notfoundhandler/master/doc/img/Redirects.png)

## Features

* The NotFound handler stores the redirects in the database. Editors can add redirects without any deployments.
* All redirects are edited in the Admin UI as shown above. After the add-on is installed and a proper 404 page has been created, no changes to the application is needed to add new redirects.
* You can import and export redirects as XML from the Admin UI.
* Handles partial and "full" URLs and can redirect out of the site by using fully qualified URLs for the "New url" field.
* Supports wildcard redirects.
* By using fully qualified URLs in the "Old url" field, they will only apply for that specific site. Editing redirects is done for all sites in the same UI.
* You need to be authorized to work with the gadget.

# Installation

The command below will install Admin UI and NotFound handler into your ASP.NET Core project.

`Install-Package Geta.NotFoundHandler.Admin`

If you need only the handler, then you can install it by the command below.

`Install-Package Geta.NotFoundHandler`

For the Episerver project, you would want to install Admin UI integration package.

`Install-Package Geta.NotFoundHandler.Episerver`

The package can be found in the [EPiServer Nuget Feed](https://nuget.episerver.com/package/?id=Geta.NotFoundHandler).

# Configuration

Add the NotFound handler in the Startup.cs in the `ConfigureServices` method. Below is an example with all available configuration you can set.
For Episerver project, also call `AddEpiserverNotFoundHandler`.

```
public void ConfigureServices(IServiceCollection services)
{
	services.AddNotFoundHandler(o =>
	{
		o.BufferSize = 30;
		o.ThreshHold = 5;
		o.HandlerMode = FileNotFoundMode.On;
		o.IgnoredResourceExtensions = new[] { "jpg", "gif", "png", "css", "js", "ico", "swf", "woff" };
		o.Logging = LoggerMode.On;
		o.LogWithHostname = false;
		o.AddProvider<NullNotFoundHandlerProvider>();
	});

services.AddEpiserverNotFoundHandler();

...
}
```

You can call the `AddNotFoundHandler` method without configuration and it will use default settings.

As an alternative or in addition, the configuration can be read from the `appsettings.json`:

```
"Geta": {
	"NotFoundHandler": {
		"BufferSize":  40
	} 
}
```

The configuration from the `appsettings.json` will override any configuration set in the Startup. Note that you cannot add providers in the `appsetings.json`. All other settings are supported.

Next, initialize NotFound handler in the `Configure` method as the first registration. It will make sure that NotFound handler will catch all 404 errors.

```
public void Configure(IApplicationBuilder app)
{
	app.UseNotFoundHandler();

...
}
```

## Settings

**HandlerMode** You can turn off the redirects by setting `HandlerMode` to `Off`. Default is `On`.

### Logging
Suggestions for NotFound rules require 404 requests to be logged to the database.

Logging of 404 requests is buffered to shield your application from Denial of Service attacks. By default, logging will happen for every 30'th error. You can change this setting in the configuration and set `bufferSize` to `0` to log the errors immediately. This is not recommended as you will be vulnerable to massive logging to your database. You can control how much you would like to log by specifying a threshold value. This value determines how frequently 404 errors are allowed to be logged.

**Important!** Even if the threshold is set low, you can still receive a lot of requests in the 404 log. In the Admin view (follow "Administer" link in gadget) you can delete suggestions (logged 404 requests). You can find all the logged items in the `NotFoundHandler.Suggestions` table in your database if you want to manually clear the logged requests (this will not remove any redirects).

![](https://raw.githubusercontent.com/Geta/geta-notfoundhandler/master/doc/img/Administer.png)

**Logging**: Turn logging `On` or `Off`. Default is `On`

**BufferSize**: Size of memory buffer to hold 404 requests. Default is 30

**Threshold**: Average maximum allowed requests per second. Default is 5

 * Example 1:
   * bufferSize is set to 100, threshold is set to 10
   * Case: 100 errors in 5 seconds - (diff = seconds between first logged request and the last logged request in the buffer).
   * 100 / 5 = 20. Error frequency is higher than threshold value. Buffered requests will not get logged, the entire buffer will be discarded.
 * Example 2:
   * bufferSize is 100, threshold is 10
   * Case: 100 errors in 15 seconds
   * 100 / 15 = 6. Error frequency is within threshold value. Buffered requests will get logged.
   
If the `bufferSize` is set to `0`, the `threshold` value will be ignored, and every request will be logged immediately.

**LogWithHostname**: Set to `true` to include hostname in the log. Useful in a multisite environment with several hostnames/domains. Default is `false` 

### Specifying ignored resources

**IgnoredResourceExtensions** 

By default, requests to files with the following extensions will be ignored by the redirect module: `jpg,gif,png,css,js,ico,swf,woff`

If you want to specify this yourself, add `IgnoredResourceExtensions` to the configuration.

## Restricting access to the Admin UI

By default, only users of `Administrators` role can access Admin UI. But you can configure you authorization policy when registrating the NotFound handler.

```
 services.AddNotFoundHandler(o => { },
            policy =>
            {
                policy.RequireRole("MyRole");
            });
```

You can setup any policy rules you want.

## Import

For details see [Import redirects for 404 handler](https://getadigital.com/blog/import-redirects-for-404-handler/) article.

# Custom 404 Page


# Custom Handlers
If you need more advanced or custom logic to create redirects, you can implement an INotFoundHandler.

1. Create a class that implements `Geta.NotFoundHandler.Core`
2. In the `public string RewriteUrl(string url)` method, add your custom logic
3. Register the handler in the configuration.

```
services.AddNotFoundHandler(o =>
{
        o.AddProvider<CustomProductRedirectHandler>();
});
```

This is especially useful for rewrites that follow some kind of logic, like checking the querystring for and id or some other value you can use to look up the page.

Here is an example using EPiServer Find to look up a product by code:

```csharp
public class CustomProductRedirectHandler : INotFoundHandler
{
    public string RewriteUrl(string url)
    {
        if(url.Contains("productid"))
        {
            // Give it a thorough look - see if we can redirect it
            Url uri = new Url(url);
            var productId = uri.QueryCollection.GetValues("productid").FirstOrDefault();
            if (productId != null && string.IsNullOrEmpty(productId) == false)
            {
                SearchResults<FindProduct> results = SearchClient.Instance.Search<FindProduct>()
                    .Filter(p => p.Code.MatchCaseInsensitive(productId))
                    .GetResult();
                if (results.Hits.Any())
                {
                    // Pick the first one
                    SearchHit<FindProduct> product = results.Hits.FirstOrDefault();
                    return product.Document.ProductUrl;
                }
            }
        }
        return null;
    }
}
```

**Note!** Make sure the code you add has good performance, it could be called a lot. If you're querying a database or a search index, you might want to add caching and perhaps Denial Of Service prevention measures.

# Troubleshooting
The module has extensive logging. Turn on debug logging for the `Geta.NotFoundHandler` namespace in your logging configuration.

# Usage

## Wildcards

If you want to redirect many addresses below a specific one to one new URL, set this to true. If we get a wild card match on this URL, the new URL will be used in its raw format and the old URL will not be appended to the new one.

For example, if we have a redirect: `/a` to `/b`, then:
- with wildcard setting it will redirect `/a/1` to `/b`
- without wildcard setting it will redirect `/a/1` to `/b/1`

# Sandbox App
Sandbox application is testing poligon for pacakge new features and bug fixes.

CMS username: epiadmin

Password: 3p!Pass

# Contributing
If you can help please do so by contributing to the package!
Reach out package maintainer for additional details if needed.

## Package Maintainer

https://github.com/marisks


# Mentions and Resources


