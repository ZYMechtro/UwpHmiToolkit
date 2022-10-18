using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UwpHmiToolkit.Semi
{
    public partial class SecsII
    {
        public enum ACKC7 : byte
        {
            Accepted = 0,
            Permission_not_granted = 1,
            Length_error = 2,
            Matrix_overflow = 3,
            PPID_not_found = 4,
            Unsupported_mode = 5,
            Invalid_recipe = 6,
            Name_formatte_was_illegal = 7,
            MDLN_not_match = 8,
            SOFTREV_not_match = 9,
            PPID_need_use_numeric_value = 10,
            PPID_values_over_limit = 11,
            CCode_can_not_find = 12,
            Invalid_PPID = 13,
            CCode_format_error = 14,
        }
        public static B GetACKC7(ACKC7 ack) => new B((byte)ack);

    }
}
