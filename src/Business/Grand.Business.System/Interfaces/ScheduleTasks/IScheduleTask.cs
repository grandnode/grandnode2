using System.Threading.Tasks;

namespace Grand.Business.System.Interfaces.ScheduleTasks
{
    public interface IScheduleTask
    {
        Task Execute();
    }
}
