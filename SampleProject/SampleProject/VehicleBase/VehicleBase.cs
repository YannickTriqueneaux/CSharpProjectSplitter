using SampleProject.Engine;
using SampleProject.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProject.VehicleBase
{
    class VehicleBase
    {
        List<Part> m_parts;
        public EngineDef m_engine = new EngineDef();

        public VehicleBase()
        {
            m_parts.Add(new Klaxon());
            m_parts.AddRange(GetWheels());

            Console.WriteLine($"With Transmission type: {m_engine.GetTransmisson().GetType().Name}");
        }

        private IEnumerable<Wheels> GetWheels()
        {
            yield return Wheels.Get(4);
        }
    }
}
