// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Core.Redirects
{
    /// <summary>
    /// Class for reading and writing to the custom redirects file
    /// </summary>
    public class RedirectsXmlParser : IRedirectsParser
    {
        private readonly ILogger<RedirectsXmlParser> _logger;
        private XmlDocument _customRedirectsXmlFile;

        private const string NewUrl = "new";
        private const string OldUrl = "old";
        private const string SkipWildcard = "onWildCardMatchSkipAppend";
        private const string RedirectType = "redirectType";

        public RedirectsXmlParser(ILogger<RedirectsXmlParser> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Reads the custom redirects information from the specified xml file
        /// </summary>
        public CustomRedirectCollection LoadFromStream(Stream xmlContent)
        {
            _customRedirectsXmlFile = new XmlDocument();
            if (xmlContent != null)
            {
                _customRedirectsXmlFile.Load(xmlContent);
            }
            else
            {
                // Not on disk, not in a vpp, construct an empty one
                _customRedirectsXmlFile = new XmlDocument
                {
                    InnerXml = "<redirects><urls></urls></redirects>"
                };
                _logger.LogError("NotFoundHandler: The Custom Redirects file does not exist");
            }

            return Load();
        }

        public virtual XmlDocument Export(List<CustomRedirect> redirects)
        {
            var document = new XmlDocument();
            var xmlDeclaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            var root = document.DocumentElement;
            document.InsertBefore(xmlDeclaration, root);

            var redirectsElement = document.CreateElement(string.Empty, "redirects", string.Empty);
            document.AppendChild(redirectsElement);

            var urlsElement = document.CreateElement(string.Empty, "urls", string.Empty);
            redirectsElement.AppendChild(urlsElement);

            foreach (var redirect in redirects)
            {
                if (string.IsNullOrWhiteSpace(redirect.OldUrl) || string.IsNullOrWhiteSpace(redirect.NewUrl))
                {
                    continue;
                }

                var urlElement = document.CreateElement(string.Empty, "url", string.Empty);

                var oldElement = document.CreateElement(string.Empty, OldUrl, string.Empty);
                oldElement.AppendChild(document.CreateTextNode(redirect.OldUrl.Trim()));
                if (redirect.WildCardSkipAppend)
                {
                    var wildCardAttribute = document.CreateAttribute(string.Empty, SkipWildcard, string.Empty);
                    wildCardAttribute.Value = "true";
                    oldElement.Attributes.Append(wildCardAttribute);
                }

                var redirectTypeAttribute = document.CreateAttribute(string.Empty, RedirectType, string.Empty);
                redirectTypeAttribute.Value = redirect.RedirectType.ToString();
                oldElement.Attributes.Append(redirectTypeAttribute);

                var newElement = document.CreateElement(string.Empty, NewUrl, string.Empty);
                newElement.AppendChild(document.CreateTextNode(redirect.NewUrl.Trim()));

                urlElement.AppendChild(oldElement);
                urlElement.AppendChild(newElement);

                urlsElement.AppendChild(urlElement);
            }

            return document;
        }

        /// <summary>
        /// Parses the xml file and reads all redirects.
        /// </summary>
        /// <returns>A collection of CustomRedirect objects</returns>
        private CustomRedirectCollection Load()
        {
            const string UrlPath = "/redirects/urls/url";

            var redirects = new CustomRedirectCollection();

            // Parse all url nodes
            var nodes = _customRedirectsXmlFile.SelectNodes(UrlPath);

            if (nodes == null) throw new InvalidOperationException($"Can't find nodes under '{UrlPath}'.");

            foreach (XmlNode node in nodes)
            {
                // Each url new url can have several old values
                // we need to create a redirect object for each pair
                var newNode = node.SelectSingleNode(NewUrl);
                var oldNodes = node.SelectNodes(OldUrl);

                if (newNode == null) throw new InvalidOperationException($"Can't find node under '{UrlPath}/{NewUrl}'.");
                if (oldNodes == null) throw new InvalidOperationException($"Can't find nodes under '{UrlPath}/{OldUrl}'.");

                foreach (XmlNode oldNode in oldNodes)
                {
                    var skipWildCardAppend = GetSkipWildCardAppend(oldNode);
                    var redirectType = GetRedirectType(oldNode);

                    // Create new custom redirect nodes
                    var redirect = new CustomRedirect(oldNode.InnerText, newNode.InnerText, skipWildCardAppend, redirectType);
                    redirects.Add(redirect);
                }
            }

            return redirects;
        }

        private static bool GetSkipWildCardAppend(XmlNode oldNode)
        {
            var skipWildCardAttr = oldNode.Attributes?[SkipWildcard];
            if (skipWildCardAttr != null && bool.TryParse(skipWildCardAttr.Value, out var skipWildCardAppend))
            {
                return skipWildCardAppend;
            }

            return false;
        }

        private static RedirectType GetRedirectType(XmlNode oldNode)
        {
            var redirectTypeAttr = oldNode.Attributes?[RedirectType];
            if (redirectTypeAttr != null && Enum.TryParse(redirectTypeAttr.Value, out RedirectType redirectType))
            {
                return redirectType;
            }

            return Redirects.RedirectType.Temporary;
        }
    }
}
