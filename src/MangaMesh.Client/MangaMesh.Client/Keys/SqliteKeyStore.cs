using MangaMesh.Client.Data;
using Microsoft.EntityFrameworkCore;

namespace MangaMesh.Client.Keys
{
    public class SqliteKeyStore : IKeyStore
    {
        private readonly ClientDbContext _context;

        public SqliteKeyStore(ClientDbContext context)
        {
            _context = context;
        }

        public async Task<PublicPrivateKeyPair?> GetAsync()
        {
            var entity = await _context.Keys.OrderByDescending(k => k.CreatedAt).FirstOrDefaultAsync();

            if (entity == null)
            {
                return null;
            }

            return new PublicPrivateKeyPair(entity.PublicKey, entity.PrivateKey);
        }

        public async Task SaveAsync(string publicKeyBase64, string privateKeyBase64)
        {
            var entity = new KeyEntity
            {
                PublicKey = publicKeyBase64,
                PrivateKey = privateKeyBase64,
                CreatedAt = DateTime.UtcNow
            };

            _context.Keys.Add(entity);
            await _context.SaveChangesAsync();
        }
    }
}
