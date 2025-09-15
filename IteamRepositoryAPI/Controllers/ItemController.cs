using IteamRepositoryAPI.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IteamRepositoryAPI.Services;

namespace IteamRepository.Controllers
{
    // Fix: Change 'ControllerBas' to 'ControllerBase'
    [ApiController]

    [Route("api/[controller]")]

    [Authorize]

    public class ItemController : ControllerBase
    {
        private static readonly List<Item> _items = new();
        IItemService _itemService;

        public ItemController(IItemService service) 
        { 
            _itemService = service;
        }

        
        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _itemService.AddItem(userId, request.Name);


            return NoContent();
        }

        [HttpDelete("softdelete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete([FromBody] DeleteItemRequest item)
        {
            await _itemService.SoftDeleteItem(item.itemId);

            return NoContent();
        }

        [HttpGet("all")]
        public async Task <IActionResult> GetAll()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            
            return Ok(await _itemService.GetAllUserItems(userId));
        }
    }
}
