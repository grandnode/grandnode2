﻿using Grand.Business.Core.Commands.System.Common;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Common
{
    public class ClearMostViewedCommandHandler : IRequestHandler<ClearMostViewedCommand, bool>
    {
        private readonly IRepository<Product> _repositoryProduct;

        public ClearMostViewedCommandHandler(IRepository<Product> repositoryProduct)
        {
            _repositoryProduct = repositoryProduct;
        }

        public async Task<bool> Handle(ClearMostViewedCommand request, CancellationToken cancellationToken)
        {
            await _repositoryProduct.UpdateManyAsync(x => x.Viewed != 0,
                        UpdateBuilder<Product>.Create().Set(x => x.Viewed, 0));

            return true;
        }
    }
}
