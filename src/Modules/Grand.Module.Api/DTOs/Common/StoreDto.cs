﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Common;

public class StoreDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool SslEnabled { get; set; }
    public string SecureUrl { get; set; }
    public string Hosts { get; set; }
    public string DefaultLanguageId { get; set; }
    public string DefaultWarehouseId { get; set; }
    public int DisplayOrder { get; set; }
    public string CompanyName { get; set; }
    public string CompanyAddress { get; set; }
    public string CompanyPhoneNumber { get; set; }
    public string CompanyRegNo { get; set; }
    public string CompanyVat { get; set; }
}