//Contribution: Orchard project (http://www.orchardproject.net/)
using Grand.Business.Core.Interfaces.System.MachineNameProvider;

namespace Grand.Business.System.Services.MachineNameProvider
{
    /// <summary>
    /// Default machine name provider
    /// </summary>
    public class DefaultMachineNameProvider : IMachineNameProvider
    {
        /// <summary>
        /// Returns the name of the machine (instance) running the application.
        /// </summary>
        public string GetMachineName()
        {
            return Environment.MachineName;
        }
    }
}
