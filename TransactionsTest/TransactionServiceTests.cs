using AutoMapper;
using Moq;
using TransactionManager.Data;
using TransactionManager.Data.Models;
using TransactionManager.Dtos;
using TransactionManager.Exceptions;
using TransactionManager.Services;

namespace TransactionManager.Tests.Services
{
    [TestFixture]
    public class TransactionServiceTests
    {
        private Mock<ITransactionRepository> _mockRepository = null!;
        private Mock<IMapper> _mockMapper = null!;
        private TransactionService _transactionService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new Mock<ITransactionRepository>();
            _mockMapper = new Mock<IMapper>();
            _transactionService = new TransactionService(_mockRepository.Object, _mockMapper.Object);
        }

        #region GetClientBalanceAsync Tests

        [Test]
        public async Task GetClientBalanceAsync_ClientExists_ReturnsBalance()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var client = new ClientModel { ClientId = clientId, Balance = 100m, LastUpdated = DateTime.UtcNow };

            _mockRepository.Setup(r => r.GetClientByIdAsync(clientId))
                .ReturnsAsync(client);

            // Act
            var result = await _transactionService.GetClientBalanceAsync(clientId);

            // Assert
            Assert.AreEqual(client.Balance, result.Balance);
            Assert.AreEqual(client.LastUpdated, result.BalanceDateTime);
        }

        [Test]
        public void GetClientBalanceAsync_ClientNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetClientByIdAsync(clientId)).ReturnsAsync((ClientModel)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _transactionService.GetClientBalanceAsync(clientId));
            Assert.AreEqual($"Client not found. Client ID: {clientId}.", ex.Message);
        }

        #endregion

        #region AddTransactionAsync Tests

        [Test]
        public async Task AddTransactionAsync_TransactionExists_ReturnsExistingTransactionBalance()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionDto = new TransactionDto { TransactionId = transactionId };
            var existingTransaction = new TransactionModel
            {
                TransactionId = transactionId,
                ClientBalance = 200m,
                CreatedDateUtc = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionId, false))
                .ReturnsAsync(existingTransaction);


            // Act
            var result = await _transactionService.AddTransactionAsync(transactionDto);

            // Assert
            Assert.AreEqual(existingTransaction.ClientBalance, result.Balance);
            Assert.AreEqual(existingTransaction.CreatedDateUtc, result.BalanceDateTime);
        }

        [Test]
        public void AddTransactionAsync_TransactionAmountNotSpecified_ThrowsArgumentException()
        {
            // Arrange
            var transactionDto = new TransactionDto
            { TransactionId = Guid.NewGuid(), ClientId = Guid.NewGuid(), DateTime = DateTime.UtcNow.AddDays(-1) };

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionDto.TransactionId, false))
                .ReturnsAsync((TransactionModel?)null);
            _mockRepository.Setup(r => r.GetClientByIdAsync(transactionDto.ClientId))
                .ReturnsAsync(new ClientModel
                {
                    ClientId = transactionDto.ClientId,
                    LastUpdated = DateTime.UtcNow.AddDays(-2),
                    Balance = 200m
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _transactionService.AddTransactionAsync(transactionDto));
            Assert.AreEqual($"Transaction amount is not specified. Transaction ID: {transactionDto.TransactionId}", ex.Message);
        }

        [Test]
        public void AddTransactionAsync_TransactionBackdated_ThrowsNotLastTransactionException()
        {
            // Arrange
            var transactionDto = new TransactionDto
            { TransactionId = Guid.NewGuid(), ClientId = Guid.NewGuid(), DateTime = DateTime.UtcNow.AddDays(-1) };

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionDto.TransactionId, false))
                .ReturnsAsync((TransactionModel?)null);
            var clientLastUpdated = DateTime.UtcNow.AddDays(0.5);
            _mockRepository.Setup(r => r.GetClientByIdAsync(transactionDto.ClientId))
                .ReturnsAsync(new ClientModel
                {
                    ClientId = transactionDto.ClientId,
                    LastUpdated = clientLastUpdated,
                    Balance = 200m
                });

            // Act & Assert
            var exceptionMessageExpected = "There are more recent transactions for this client. " +
                $"DateTime specified {transactionDto.DateTime}. " +
                $"Last transaction is on {clientLastUpdated}. " +
                $"Client: {transactionDto.ClientId}.";

            var ex = Assert.ThrowsAsync<NotLastTransactionException>(async () => await _transactionService.AddTransactionAsync(transactionDto));
            Assert.AreEqual(exceptionMessageExpected, ex.Message);
        }

        [Test]
        public void AddTransactionAsync_NotEnoughMoney_ThrowsInsufficientAmountException()
        {
            // Arrange
            var transactionDto = new TransactionDto
            { TransactionId = Guid.NewGuid(), ClientId = Guid.NewGuid(), DateTime = DateTime.UtcNow.AddDays(-1), Credit = 300m };

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionDto.TransactionId, false))
                .ReturnsAsync((TransactionModel?)null);
            var clientLastUpdated = DateTime.UtcNow.AddDays(-2);
            _mockRepository.Setup(r => r.GetClientByIdAsync(transactionDto.ClientId))
                .ReturnsAsync(new ClientModel
                {
                    ClientId = transactionDto.ClientId,
                    LastUpdated = clientLastUpdated,
                    Balance = 200m
                });

            // Act & Assert
            var exceptionMessageExpected = $"Client {transactionDto.ClientId} has insufficient funds.";

            var ex = Assert.ThrowsAsync<InsufficientAmountException>(async () => await _transactionService.AddTransactionAsync(transactionDto));
            Assert.AreEqual(exceptionMessageExpected, ex.Message);
        }

        #endregion

        #region RevertTransactionAsync Tests

        [Test]
        public async Task RevertTransactionAsync_TransactionExists_ReturnsCompensatingTransactionBalance()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var transaction = new TransactionModel { TransactionId = transactionId, Debit = 50m, ClientBalance = 150m };
            var client = new ClientModel { ClientId = clientId, Balance = 150m };

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionId, true))
                .ReturnsAsync(transaction);
            _mockRepository.Setup(r => r.GetClientByIdAsync(clientId))
                .ReturnsAsync(client);

            // Act
            var result = await _transactionService.RevertTransactionAsync(transactionId, clientId);

            // Assert
            Assert.AreEqual(transaction.ClientBalance - 50m, result.Balance); // Expecting compensating transaction balance
        }

        [Test]
        public void RevertTransactionAsync_TransactionNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var clientId = Guid.NewGuid();

            _mockRepository.Setup(r => r.GetTransactionByIdAsync(transactionId, true))
                .ReturnsAsync((TransactionModel)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _transactionService.RevertTransactionAsync(transactionId, clientId));
            Assert.AreEqual($"Transaction not found. Id: {transactionId}", ex.Message);
        }

        #endregion
    }
}
