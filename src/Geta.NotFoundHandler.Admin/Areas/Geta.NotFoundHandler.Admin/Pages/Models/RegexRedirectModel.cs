// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.ComponentModel.DataAnnotations;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;

public class RegexRedirectModel
{
    public Guid? Id { get; set; }
    [Required]
    [Display(Name = "Old URL Regex")]
    public string OldUrlRegex { get; set; }
    [Required]
    [Display(Name = "New URL Format")]
    public string NewUrlFormat { get; set; }
    [Required]
    [Display(Name = "Order Number")]
    public int OrderNumber { get; set; }
}
