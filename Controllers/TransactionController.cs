using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TransactionManager.Dtos;
using TransactionManager.Services;
using TransactionManager.Views;

namespace TransactionManager.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IMapper _mapper;

    public TransactionController(ITransactionService transactionService, IMapper mapper)
    {
        _transactionService = transactionService;
        _mapper = mapper;
    }

    [HttpPost("debit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InsertTransactionResult>> Debit([FromBody] DebitTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(_mapper.Map<InsertTransactionResult>(result));
    }

    [HttpPost("credit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InsertTransactionResult>> Credit([FromBody] CreditTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(_mapper.Map<InsertTransactionResult>(result));
    }

    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevertTransactionResult>> Revert([FromQuery] Guid id, [FromQuery] Guid clientId)
    {
        var result = await _transactionService.RevertTransactionAsync(id, clientId);
        return Ok(_mapper.Map<RevertTransactionResult>(result));
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(ClientBalanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientBalanceResult>> GetBalance([FromQuery] Guid clientId)
    {
        var result = await _transactionService.GetClientBalanceAsync(clientId);
        return Ok(_mapper.Map<ClientBalanceResult>(result));
    }
}