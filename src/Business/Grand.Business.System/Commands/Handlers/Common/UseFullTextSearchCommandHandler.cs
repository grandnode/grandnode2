using Grand.Business.System.Commands.Models.Common;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Common
{
    public class UseFullTextSearchCommandHandler : IRequestHandler<UseFullTextSearchCommand, bool>
    {
        private readonly IRepository<Product> _repositoryProduct;

        public UseFullTextSearchCommandHandler(IRepository<Product> repositoryProduct)
        {
            _repositoryProduct = repositoryProduct;
        }

        public async Task<bool> Handle(UseFullTextSearchCommand request, CancellationToken cancellationToken)
        {
            if (request.UseFullTextSearch)
            {
                var indexOption = new CreateIndexOptions() { Name = "ProductText" };
                indexOption.Collation = new Collation("simple");
                await _repositoryProduct.Collection.Indexes.CreateOneAsync(new CreateIndexModel<Product>((Builders<Product>.IndexKeys.Text("$**")), indexOption));
            }
            else
            {
                await _repositoryProduct.Collection.Indexes.DropOneAsync("ProductText");
            }

            return true;
        }
    }
}
