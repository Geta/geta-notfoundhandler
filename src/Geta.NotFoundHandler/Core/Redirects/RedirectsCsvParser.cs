// Copyright (c) Geta Digital. All rights reserved.
// Functionality added by Jacob Spencer 06/2024
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Geta.NotFoundHandler.Models;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Core.Redirects;

public class RedirectsCsvParser : IRedirectsParser
{
    private static readonly Regex FormulaAfterSeparator = new(@";(?=[\s""']*[=@+\-\t\r\n])");

    private readonly ILogger<RedirectsCsvParser> _logger;

    public RedirectsCsvParser(ILogger<RedirectsCsvParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads the custom redirects information from the specified csv file
    /// </summary>
    public CustomRedirectCollection LoadFromStream(Stream csvContent)
    {
        using var reader = new StreamReader(csvContent);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<CsvImportModel>().ToList();

        if(!records.Any())
        {
            _logger.LogError("NotFoundHandler: The Custom Redirects file does not exist");
            return new CustomRedirectCollection();
        }

        return Load(records);
    }

    private CustomRedirectCollection Load(IEnumerable<CsvImportModel> csvImportModels)
    {
        var redirects = new CustomRedirectCollection();

        foreach (var item in csvImportModels)
        {
            // Create new custom redirect nodes
            var redirectType = GetRedirectType(item.RedirectType);
            var skipWildCardAppend = GetSkipWildCardAppend(item.WildcardSkippedAppend);
            var redirect = new CustomRedirect(item.OldUrl, item.NewUrl, skipWildCardAppend, redirectType);
            redirects.Add(redirect);
        }

        return redirects;
    }

    private static bool GetSkipWildCardAppend(string skipWildCardAttr)
    {
        if (!string.IsNullOrWhiteSpace(skipWildCardAttr) && bool.TryParse(skipWildCardAttr, out var skipWildCardAppend))
        {
            return skipWildCardAppend;
        }

        return false;
    }

    private RedirectType GetRedirectType(string redirectTypeAttr)
    {
        if (!string.IsNullOrWhiteSpace(redirectTypeAttr) && Enum.TryParse(redirectTypeAttr, out RedirectType redirectType))
        {
            return redirectType;
        }

        return Redirects.RedirectType.Temporary;
    }

    public virtual List<CsvImportModel> Export(List<CustomRedirect> redirects)
    {
        return redirects.Select(item => new CsvImportModel()
            {
                NewUrl = item.NewUrl,
                OldUrl = item.OldUrl,
                RedirectType = item.RedirectType.ToString(),
                WildcardSkippedAppend = item.WildCardSkipAppend.ToString()
            }).ToList();
    }

    /// <summary>
    /// Writes the redirects as CSV. Values a spreadsheet would run as a formula are escaped,
    /// whichever way they got into the database.
    /// </summary>
    public virtual byte[] ExportToCsv(List<CustomRedirect> redirects)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            InjectionOptions = InjectionOptions.Escape,
            // Note: The CsvHelper defaults without the line feed, which OWASP lists too.
            InjectionCharacters = ['=', '@', '+', '-', '\t', '\r', '\n']
        };

        var records = Export(redirects);
        foreach (var record in records)
        {
            record.OldUrl = EncodeSeparatorBeforeFormula(record.OldUrl);
            record.NewUrl = EncodeSeparatorBeforeFormula(record.NewUrl);
        }

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, configuration))
        {
            csv.WriteRecords(records);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// A spreadsheet using ";" as the list separator would put what follows it in a cell of its own,
    /// which the check on the start of a value can't see. "%3B" means the same in a url.
    /// </summary>
    private static string EncodeSeparatorBeforeFormula(string url)
    {
        return url == null ? null : FormulaAfterSeparator.Replace(url, "%3B");
    }
}
