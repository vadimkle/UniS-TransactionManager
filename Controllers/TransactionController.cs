using AutoMapper;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Debit([FromBody] DebitTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(new InsertTransactionResult
        { InsertDateTime = result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("credit")]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> Credit([FromBody] CreditTransaction transaction)
    {
        var dto = _mapper.Map<TransactionDto>(transaction);
        var result = await _transactionService.AddTransactionAsync(dto);
        return Ok(new InsertTransactionResult
        { InsertDateTime = result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revert([FromQuery] Guid id, [FromQuery] Guid clientId)
    {
        var result = await _transactionService.RevertTransactionAsync(id, clientId);
        return Ok(new RevertTransactionResult
        { RevertDateTime = result.revertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpGet("balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance([FromQuery] Guid clientId)
    {
        var balance = await _transactionService.GetClientBalanceAsync(clientId);
        return Ok(new { balanceDateTime = DateTime.UtcNow, clientBalance = balance });
    }
}