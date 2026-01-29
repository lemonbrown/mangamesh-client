using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using NSec.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public class KeyPairService : IKeyPairService
    {
        private readonly IKeyStore _keyStore;

        public KeyPairService(IKeyStore keyStore)
        {
            _keyStore = keyStore;
        }

        public KeyPairResult GenerateKeyPairBase64()
        {
            var creationParameters = new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            };

            using var key = new Key(SignatureAlgorithm.Ed25519, creationParameters);

            var privateKeyBase64 = Convert.ToBase64String(
                key.Export(KeyBlobFormat.RawPrivateKey));

            var publicKeyBase64 = Convert.ToBase64String(
                key.Export(KeyBlobFormat.RawPublicKey));

            return new KeyPairResult(privateKeyBase64, publicKeyBase64);
        }

        public async Task<KeyPairResult> GenerateKeyPairBase64Async()
        {
            var result = GenerateKeyPairBase64();

            Directory.CreateDirectory(AppContext.BaseDirectory + "\\data\\keys");

            // ✅ Safe to await now
            await _keyStore.SaveAsync(result.PublicKeyBase64);

            return result;
        }

        public string SolveChallenge(string nonceBase64, string privateKeyBase64)
        {

            // --------------------------
            // 2️⃣ Decode inputs
            // --------------------------
            byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            byte[] nonceBytes = Convert.FromBase64String(nonceBase64);

            // --------------------------
            // 3️⃣ Import private key (raw)
            // --------------------------
            var creationParams = new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            };

            using var key = Key.Import(SignatureAlgorithm.Ed25519, privateKeyBytes, KeyBlobFormat.RawPrivateKey);

            // --------------------------
            // 4️⃣ Sign the nonce
            // --------------------------
            byte[] signatureBytes = SignatureAlgorithm.Ed25519.Sign(key, nonceBytes);

            if (signatureBytes.Length != 64)
            {
                Console.WriteLine($"ERROR: Signature length is {signatureBytes.Length}, expected 64 bytes!");
                return "";
            }

            string signatureBase64 = Convert.ToBase64String(signatureBytes);

            return signatureBase64;
        }

    }
}
