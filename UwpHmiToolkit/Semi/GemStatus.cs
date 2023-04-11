using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UwpHmiToolkit.Semi
{
    public partial class Gem
    {
        public enum CommunicationState
        {
            Disable,
            Enable_WaitDelay,
            Enable_WaitCra,
            Enable_WaitCrFromHost,
            Enable_Communicating
        }

        public enum ControlState
        {
            Offline_EqpOffline,
            Offline_AttemptOnline,
            Offline_HostOffline,
            Online_Local,
            Online_Remote
        }

        public enum ProcessingState
        {
            Initialize,
            Idle,
            Ready,
            Running,
            PauseOnReady
        }

        public enum SpoolingState
        {
            SpoolInactive,
            Active_Output_PurgeSpool,
            Active_Output_TransmitSpool,
            Active_NoSpoolOutput,
            Active_SpoolNotFull,
            Active_SpoolFull
        }

    }
}
