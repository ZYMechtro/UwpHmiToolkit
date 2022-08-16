using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UwpHmiToolkit.ViewModel;

namespace UwpHmiToolkit.Semi
{
    public class EquipmentInfo : AutoBindableBase
    {
        /// <summary>
        /// Equipment Model
        /// </summary>
        public string MDLN { get; set; } = "Model";

        /// <summary>
        /// Software Revision Code
        /// </summary>
        public string SOFTREV { get; set; } = "0.0";


    }
}
