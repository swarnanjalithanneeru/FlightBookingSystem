using FlightBookingSystem.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Repositories;
using PaymentService.Services;

namespace FlightBookingSystem.Tests;

[TestFixture]
public class PaymentServiceTests
{
    private PaymentDbContext _dbContext = null!;
    private IPaymentRepository _paymentRepository = null!;
    private IPaymentService _paymentService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PaymentDbContext(options);
        _dbContext.Database.EnsureCreated();

        _paymentRepository = new PaymentRepository(_dbContext);
        _paymentService = new PaymentServiceImpl(_paymentRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task SimulatePayment_Success_ReturnsTransactionIdAndSuccessStatus()
    {
        var request = new SimulatePaymentRequestDto
        {
            BookingId = 501,
            Amount = 4500.00m,
            PaymentMethod = "CreditCard",
            SimulateStatus = "Success"
        };

        var result = await _paymentService.SimulatePaymentAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.PaymentStatus, Is.EqualTo(PaymentStatusConstants.Success));
        Assert.That(result.Data.TransactionId, Does.StartWith("TXN-"));
        Assert.That(result.Data.Amount, Is.EqualTo(4500.00m));
    }

    [Test]
    public async Task SimulatePayment_Failed_RecordsFailedStatus()
    {
        var request = new SimulatePaymentRequestDto
        {
            BookingId = 502,
            Amount = 3000.00m,
            PaymentMethod = "UPI",
            SimulateStatus = "Failed"
        };

        var result = await _paymentService.SimulatePaymentAsync(request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("failed"));

        // Verify record was persisted with Failed status in PaymentDB
        var paymentInDb = await _paymentRepository.GetByBookingIdAsync(502);
        Assert.That(paymentInDb, Is.Not.Null);
        Assert.That(paymentInDb!.PaymentStatus, Is.EqualTo(PaymentStatusConstants.Failed));
    }

    [Test]
    public async Task SimulatePayment_Idempotent_ReturnsExistingTransaction()
    {
        var request = new SimulatePaymentRequestDto
        {
            BookingId = 503,
            Amount = 2500.00m,
            PaymentMethod = "DebitCard",
            SimulateStatus = "Success"
        };

        // First call
        var firstResult = await _paymentService.SimulatePaymentAsync(request);
        Assert.That(firstResult.Success, Is.True);

        // Second duplicate call
        var secondResult = await _paymentService.SimulatePaymentAsync(request);
        Assert.That(secondResult.Success, Is.True);
        Assert.That(secondResult.Data!.TransactionId, Is.EqualTo(firstResult.Data!.TransactionId));
        Assert.That(secondResult.Data.PaymentId, Is.EqualTo(firstResult.Data.PaymentId));
    }

    [Test]
    public async Task SimulatePayment_InvalidStatus_Rejects()
    {
        var request = new SimulatePaymentRequestDto
        {
            BookingId = 504,
            Amount = 1000.00m,
            SimulateStatus = "UnknownStatus"
        };

        var result = await _paymentService.SimulatePaymentAsync(request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Only 'Success' or 'Failed'"));
    }

    [Test]
    public async Task SimulateRefund_UpdatesPaymentStatusToRefunded()
    {
        // Setup initial successful payment
        await _paymentService.SimulatePaymentAsync(new SimulatePaymentRequestDto
        {
            BookingId = 505,
            Amount = 6000.00m,
            PaymentMethod = "CreditCard",
            SimulateStatus = "Success"
        });

        // Process refund
        var refundResult = await _paymentService.SimulateRefundAsync(505, new SimulateRefundRequestDto
        {
            Amount = 6000.00m
        });

        Assert.That(refundResult.Success, Is.True);
        Assert.That(refundResult.Data!.PaymentStatus, Is.EqualTo(PaymentStatusConstants.Refunded));

        // Check DB
        var paymentInDb = await _paymentRepository.GetByBookingIdAsync(505);
        Assert.That(paymentInDb!.PaymentStatus, Is.EqualTo(PaymentStatusConstants.Refunded));
    }
}
