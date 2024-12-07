﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Common;

public class LanguageDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string LanguageCulture { get; set; }
    public string UniqueSeoCode { get; set; }
    public string FlagImageFileName { get; set; }
    public bool Rtl { get; set; }
    public string DefaultCurrencyId { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}