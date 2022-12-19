using SampleProject.Engine.TransmissionSystem;
using SampleProject.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProject.Engine
{
    class EngineDef
    {
        Part m_parts = new Carburator();
        public Transmission GetTransmisson() => new Transmission();
    }
}
