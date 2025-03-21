﻿using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteProductCommand : IRequest<bool>
{
    public ProductDto Model { get; set; }
}