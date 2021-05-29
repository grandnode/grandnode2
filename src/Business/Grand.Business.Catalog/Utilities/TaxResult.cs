using System.Collections.Generic;

namespace Grand.Business.Catalog.Utilities
{
    /// <summary>
    /// Represents a result of tax calculation
    /// </summary>
    public partial class TaxResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public TaxResult()
        {
            Errors = new List<string>();
        }

        /// <summary>
        /// Gets or sets a tax rate
        /// </summary>
        public double TaxRate { get; set; }

        /// <summary>
        /// Gets or sets an address
        /// </summary>
        public IList<string> Errors { get; set; }

        /// <summary>
        /// Gets a value that indicates if request has been completed successfully
        /// </summary>
        public bool Success
        {
            get 
            { 
                return Errors.Count == 0; 
            }
        }

        /// <summary>
        /// Add error
        /// </summary>
        /// <param name="error">Error</param>
        public void AddError(string error)
        {
            Errors.Add(error);
        }
    }
}
