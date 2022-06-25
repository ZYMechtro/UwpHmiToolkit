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
            Disable = 0,
            Enable = 1,
            NotCommunicatig = 2,
            WaitDelay = 3,
            WaitCra = 4,
            WaitCcrFromHost = 5,
            Communicating = 6
        }

        public enum ControlState
        {
            Offline = 0,
            EqpOffline = 1,
            AttemptOnline = 2,
            HostOffline = 3,
            Online = 4,
            Local = 5,
            Remote = 6
        }

        public enum ProcessingState
        {
            Initialize,
            Idle,
            Setup,
            Ready,
            Running,
            PauseOnReady
        }


        public enum SpoolingState
        {
            SpoolInactive,
            SpoolActive,
            PurgeSpool,
            TransmitSpool,
            NoSpoolOutput,
            SpoolNotFull,
            SpoolFull
        }

    }
}
