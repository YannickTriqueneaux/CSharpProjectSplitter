using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProject.Parts
{
    class Wheels : Part
    {
        private int nb;

        public Wheels(int nb)
        {
            this.nb = nb;
        }

        internal static Wheels Get(int nb)
        {
            return new Wheels(nb);
        }
    }
}
