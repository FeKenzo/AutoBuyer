using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBuyer.Domain.Entities
{
    public class Store : Entity
    {
        public string? Name { get; private set; }

        public string? BaseUrl { get; private set; }

        public bool IsEnabled { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public Store(string name, string baseUrl)
        {
            SetName(name);
            SetBaseUrl(baseUrl);

            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Store name is required.");

            Name = name.Trim();
        }
        public void SetBaseUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid URL.");

            BaseUrl = url.TrimEnd('/');
        }
        public void Enable()
        {
            IsEnabled = true;
        }
        public void Disable()
        {
            IsEnabled = false;
        }
    }
}
