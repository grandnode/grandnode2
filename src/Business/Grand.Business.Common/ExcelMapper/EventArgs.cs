using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ganss.Excel
{
    /// <summary>
    /// Provides data for the <see cref="ExcelMapper.Saving"/> event.
    /// </summary>
    public class SavingEventArgs: EventArgs
    {
        /// <summary>
        /// Gets or sets the sheet.
        /// </summary>
        /// <value>
        /// The sheet.
        /// </value>
        public ISheet Sheet { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavingEventArgs"/> class.
        /// </summary>
        /// <param name="sheet">The sheet that is being saved.</param>
        public SavingEventArgs(ISheet sheet)
        {
            Sheet = sheet;
        }
    }
}
