using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using MangaMesh.Shared.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public class KeyStore : IKeyStore
    {       
        public async Task SaveAsync(string publicKeyBase64)
        {
            var key = new PublicKey(publicKeyBase64);

            await JsonFileStore.SaveAsync(AppContext.BaseDirectory + "\\data\\keys\\public_key.json", key);            
        }

        public async Task<PublicKey> GetAsync()
        {
            var key = await JsonFileStore.LoadSingleAsync<PublicKey>(AppContext.BaseDirectory + "\\data\\keys\\public_key.json");

            return key;
        }
    }
}
