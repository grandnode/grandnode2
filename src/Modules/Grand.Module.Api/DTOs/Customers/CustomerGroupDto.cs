﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Customers;

public class CustomerGroupDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public bool FreeShipping { get; set; }
    public bool TaxExempt { get; set; }
    public bool Active { get; set; }
    public bool IsSystem { get; set; }
    public string SystemName { get; set; }
    public bool EnablePasswordLifetime { get; set; }
}