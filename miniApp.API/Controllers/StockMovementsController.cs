using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using miniApp.API.Data;
using miniApp.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace miniApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockMovementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StockMovementsController> _logger;

        public StockMovementsController(AppDbContext context, ILogger<StockMovementsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Fetching all stock movements...");
            var movements = await _context.StockMovements
                .Include(m => m.Product)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
            return Ok(movements);
        }

        [HttpPost("in")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> StockIn([FromBody] StockMovement movement)
        {
            _logger.LogInformation("Stock IN for ProductId: {ProductId}, Qty: {Qty}", movement.ProductId, movement.Quantity);
            movement.Type = "IN";
            movement.Timestamp = DateTime.UtcNow;
            _context.StockMovements.Add(movement);

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == movement.ProductId);
            if (inventory == null)
            {
                _logger.LogInformation("Creating new inventory for ProductId: {ProductId}", movement.ProductId);
                inventory = new Inventory
                {
                    ProductId = movement.ProductId,
                    Quantity = movement.Quantity
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.Quantity += movement.Quantity;
                _logger.LogInformation("Updated inventory for ProductId: {ProductId} to Qty: {Qty}", inventory.ProductId, inventory.Quantity);
            }

            await _context.SaveChangesAsync();
            return Ok(movement);
        }

        [HttpPost("adjust")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Adjust([FromBody] StockMovement movement)
        {
            _logger.LogInformation("Adjusting stock for ProductId: {ProductId} to Qty: {Qty}", movement.ProductId, movement.Quantity);
            movement.Type = "ADJUST";
            movement.Timestamp = DateTime.UtcNow;
            _context.StockMovements.Add(movement);

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == movement.ProductId);
            if (inventory == null)
            {
                _logger.LogWarning("Inventory not found for ProductId: {ProductId}", movement.ProductId);
                return BadRequest("Inventory not found");
            }

            inventory.Quantity = movement.Quantity;

            await _context.SaveChangesAsync();
            return Ok(movement);
        }

        [HttpPost("count")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> Count([FromBody] StockMovement movement)
        {
            _logger.LogInformation("Counting stock for ProductId: {ProductId} Qty: {Qty}", movement.ProductId, movement.Quantity);
            movement.Type = "COUNT";
            movement.Timestamp = DateTime.UtcNow;
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return Ok(movement);
        }
    }
}
