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
    private readonly TransactionService _transactionService;
    private readonly IMapper _mapper;

    public TransactionController(TransactionService transactionService, IMapper mapper)
    {
        _transactionService = transactionService;
        _mapper = mapper;
    }

    [HttpPost("debit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InsertTransactionResult>> Debit([FromBody] DebitTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(new InsertTransactionResult
        { InsertDateTime = result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("credit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<ActionResult<InsertTransactionResult>> Credit([FromBody] CreditTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(new InsertTransactionResult
        { InsertDateTime = result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevertTransactionResult>> Revert([FromQuery] Guid id, [FromQuery] Guid clientId)
    {
        var result = await _transactionService.RevertTransactionAsync(id, clientId);
        return Ok(new RevertTransactionResult
        { RevertDateTime = result.revertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(ClientBalanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientBalanceResult>> GetBalance([FromQuery] Guid clientId)
    {
        var result = await _transactionService.GetClientBalanceAsync(clientId);
        return Ok(_mapper.Map<object?>(result));
    }
}