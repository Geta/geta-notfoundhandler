// Copyright (c) Geta Digital. All rights reserved.
// Functionality added by Jacob Spencer 06/2024
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using CsvHelper.Configuration.Attributes;

namespace Geta.NotFoundHandler.Models;

public class CsvImportModel
{
    [Name("OldUrl")]
    public string OldUrl { get; set; }
    [Name("NewUrl")]
    public string NewUrl { get; set; }
    [Name("WildcardSkippedAppend")]
    public string WildcardSkippedAppend { get; set; }
    [Name("RedirectType")]
    public string RedirectType { get; set; }
}
