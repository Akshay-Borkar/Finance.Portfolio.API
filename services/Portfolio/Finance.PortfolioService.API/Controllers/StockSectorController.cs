using Finance.PortfolioService.Application.Features.StockSector.Commands.CreateStockSector;
using Finance.PortfolioService.Application.Features.StockSector.Commands.DeleteStockSector;
using Finance.PortfolioService.Application.Features.StockSector.Commands.UpdateStockSector;
using Finance.PortfolioService.Application.Features.StockSector.Queries.GetAllStockSectors;
using Finance.PortfolioService.Application.Features.StockSector.Queries.GetStockSectorDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.PortfolioService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockSectorController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockSectorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<List<StockSectorDto>> Get()
        => await _mediator.Send(new GetStockSectorQuery());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockSectorDetailDto>> Get(Guid id)
    {
        var stockSector = await _mediator.Send(new GetStockSectorDetailQuery(id));
        return Ok(stockSector);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Post(CreateStockSectorCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Put(UpdateStockSectorCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteStockSectorCommand { Id = id });
        return NoContent();
    }
}
