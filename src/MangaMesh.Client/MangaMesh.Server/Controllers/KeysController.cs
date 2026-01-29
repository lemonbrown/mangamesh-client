using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KeysController : ControllerBase
    {
        private readonly IKeyPairService _keyPairService;

        private readonly IKeyStore _keyStore;

        public KeysController(
            IKeyPairService keyPairService,
            IKeyStore keyStore)
        {
            _keyPairService = keyPairService;
            _keyStore = keyStore;
        }

        [HttpPost("generate")]
        [ProducesResponseType<KeyPairResult>(200)]
        public async Task<IResult> GenerateKeyPair()
        {
            var keyPair = await _keyPairService.GenerateKeyPairBase64Async();

            return Results.Ok(keyPair);
        }

        [HttpGet]
        [ProducesResponseType<string>(200)]
        public async Task<IResult> GetPublicKey()
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
       
    }
}
