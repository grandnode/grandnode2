using Grand.Business.System.Commands.Models.Common;
using Grand.Domain.Data;
using Grand.Domain.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Common
{
    public class DeleteActivitylogCommandHandler : IRequestHandler<DeleteActivitylogCommand, bool>
    {
        private readonly IRepository<ActivityLog> _repositoryActivityLog;

        public DeleteActivitylogCommandHandler(IRepository<ActivityLog> repositoryActivityLog)
        {
            _repositoryActivityLog = repositoryActivityLog;
        }

        public async Task<bool> Handle(DeleteActivitylogCommand request, CancellationToken cancellationToken)
        {
            await _repositoryActivityLog.ClearAsync();
            return true;
        }
    }
}
