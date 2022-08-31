﻿//Contribution: Orchard project (http://www.orchardproject.net/)
namespace Grand.Business.Core.Interfaces.System.MachineNameProvider
{
    /// <summary>
    /// Describes a service which the name of the machine (instance) running the application.
    /// </summary>
    public interface IMachineNameProvider
    {
        /// <summary>
        /// Returns the name of the machine (instance) running the application.
        /// </summary>
        string GetMachineName();
    }
}
