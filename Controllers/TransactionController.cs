using Microsoft.AspNetCore.Mvc;
using TransactionManager.Services;
using TransactionManager.Views;

namespace TransactionManager.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("debit")]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Debit([FromBody] DebitTransaction transaction)
    {
        var result = _transactionService.AddTransaction(transaction);
        return Ok(new InsertTransactionResult { InsertDateTime= result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("credit")]
    [ProducesResponseType(typeof(InsertTransactionResult), StatusCodes.Status200OK)]
    public IActionResult Credit([FromBody] CreditTransaction transaction)
    {
        var result = _transactionService.AddTransaction(transaction);
        return Ok(new InsertTransactionResult { InsertDateTime= result.insertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertTransactionResult), StatusCodes.Status200OK)]
    public IActionResult Revert([FromQuery] Guid id, [FromQuery] Guid clientId)
    {
        var result = _transactionService.RevertTransaction(id, clientId);
        return Ok(new RevertTransactionResult { RevertDateTime= result.revertDateTime, ClientBalance = result.clientBalance });
    }

    [HttpGet("balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetBalance([FromQuery] Guid clientId)
    {
        var balance = _transactionService.GetBalance(clientId);
        return Ok(new { balanceDateTime = DateTime.UtcNow, clientBalance = balance });
    }
}