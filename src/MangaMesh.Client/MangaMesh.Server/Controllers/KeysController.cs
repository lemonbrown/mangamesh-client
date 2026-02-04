using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MangaMesh.Server.Services;

namespace MangaMesh.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KeysController : ControllerBase
    {
        private readonly IKeyPairService _keyPairService;

        private readonly IKeyStore _keyStore;

        private readonly IChallengeService _challengeService;

        public KeysController(
            IKeyPairService keyPairService,
            IKeyStore keyStore,
            IChallengeService challengeService)
        {
            _keyPairService = keyPairService;
            _keyStore = keyStore;
            _challengeService = challengeService;
        }

        [HttpPost("generate")]
        [ProducesResponseType<KeyPairResult>(200)]
        public async Task<IResult> GenerateKeyPair()
        {
            var keyPair = await _keyPairService.GenerateKeyPairBase64Async();

            return Results.Ok(keyPair);
        }

        [HttpGet]
        [ProducesResponseType<KeyPairResult>(200)]
        public async Task<IResult> GetKeyPair()
        {
            var key = await _keyStore.GetAsync();

            return Results.Ok(key);
        }

        [HttpPost("challenge/solve")]
        public async Task<IResult> SolveChallenge(KeyChallengeRequest request)
        {
            var signature = _keyPairService.SolveChallenge(request.NonceBase64, request.PrivateKeyBase64);

            return Results.Ok(signature);
        }

        [HttpPost("challenges")]
        public IResult RequestChallenge([FromBody] CreateChallengeRequest request)
        {
            var (id, nonce) = _challengeService.CreateChallenge(request.PublicKey);

            return Results.Ok(new
            {
                challengeId = id,
                nonce = nonce,
                expiresAt = DateTime.UtcNow.AddMinutes(5)
            });
        }

        [HttpPost("challenges/verify")]
        public IResult VerifySignature([FromBody] VerifySignatureRequest request)
        {
            Console.WriteLine($"[KeysController] Verifying: Key={request.PublicKey}, Challenge={request.ChallengeId}");

            var nonce = _challengeService.GetNonce(request.ChallengeId);
            if (nonce == null)
            {
                Console.WriteLine("[KeysController] Challenge not found");
                return Results.NotFound("Challenge not found or expired");
            }

            var isValid = _keyPairService.Verify(request.PublicKey, request.SignatureBase64, nonce);

            if (isValid)
            {
                _challengeService.Remove(request.ChallengeId);
                return Results.Ok(new { valid = true });
            }

            Console.WriteLine("[KeysController] Verification Failed");
            return Results.BadRequest(new { valid = false, error = "Invalid signature" });
        }

        public class CreateChallengeRequest
        {
            public string PublicKey { get; set; }
        }

        public class VerifySignatureRequest
        {
            public string ChallengeId { get; set; }
            public string SignatureBase64 { get; set; }
            public string PublicKey { get; set; }
        }

    }
}
