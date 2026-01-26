using MangaMesh.Server.Models;
using MangaMesh.Server.Stores;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionStore _subscriptions;

        public SubscriptionsController(ISubscriptionStore subscriptions)
        {
            _subscriptions = subscriptions;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAll()
            => Ok(await _subscriptions.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] SubscriptionDto dto)
        {
            var added = await _subscriptions.AddAsync(dto);
            return added ? NoContent() : Conflict();
        }

        [HttpDelete]
        public async Task<IActionResult> Remove([FromBody] SubscriptionDto dto)
        {
            var removed = await _subscriptions.RemoveAsync(dto);
            return removed ? NoContent() : NotFound();
        }
    }

}
