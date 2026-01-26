using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public class ReleaseIdentity
    {
        public string DisplayName { get; init; } = string.Empty;
        public string? ClaimedGroupId { get; init; } // optional
        public bool IsVerified { get; init; }
    }

}
