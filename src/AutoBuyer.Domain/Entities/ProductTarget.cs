using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBuyer.Domain.Entities
{
    public class ProductTarget : Entity
    {
        public Guid StoreId { get; private set; }

        public Store Store { get; private set; }

        public string Name { get; private set; }

        public string ProductUrl { get; private set; }

        public decimal TargetPrice { get; private set; }

        public bool AutoBuyEnabled { get; private set; }

        public bool MonitoringEnabled { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public void EnableMonitoring()
        {
            MonitoringEnabled = true;
        }
        public void DisableMonitoring()
        {
            MonitoringEnabled = false;
        }
        public void EnableAutoBuy()
        {
            AutoBuyEnabled = true;
        }
        public void DisableAutoBuy()
        {
            AutoBuyEnabled = false;
        }


    }
}
