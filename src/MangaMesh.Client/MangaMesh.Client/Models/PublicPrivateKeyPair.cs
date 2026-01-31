using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public record PublicPrivateKeyPair(string PublicKeyBase64, string PrivateKeyBase64);
}
