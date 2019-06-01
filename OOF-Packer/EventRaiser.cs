using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOF_Packer
{
    public class EventRaiser
    {
        public static Action<string> OnFileNameChange;

        public static Action<double> OnProgressChange;
    }
}
