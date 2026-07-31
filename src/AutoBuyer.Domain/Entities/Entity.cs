using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBuyer.Domain.Entities
{
    public class Entity
    {
        public Guid Id { get; protected set; }

        protected Entity()
        {
            Id = Guid.NewGuid();
        }
    }
}
