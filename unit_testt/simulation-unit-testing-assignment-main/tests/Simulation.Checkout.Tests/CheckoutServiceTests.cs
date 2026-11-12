
using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Xunit;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;
    private readonly Mock<IInventoryService> _inventoryService;
    private readonly Mock<INotificationService> _notificationService;

    private readonly CheckoutService _checkoutService;

    public CheckoutServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _inventoryService = new Mock<IInventoryService>();
        _notificationService = new Mock<INotificationService>();

        _checkoutService = new CheckoutService(
            _orderRepository.Object,
            _inventoryService.Object,
            _notificationService.Object);
    }

    private static Order CreateOrder(
        decimal price = 100,
        int quantity = 2)
    {
        return new Order
        {
            CustomerName = "Amr",
            CustomerEmail = "amr@yahoo.com",
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    UnitPrice = price,
                    Quantity = quantity
                }
            }
        };
    }

    [Fact]
    public async Task CheckoutAsync_NullOrder_ShouldThrowArgumentNullException()
    {
        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckoutAsync_EmptyOrder_ShouldThrowEmptyOrderException()
    {
        var order = new Order
        {
            CustomerName = "Amr",
            CustomerEmail = "amr@yahoo.com",
            Items = new List<OrderItem>()
        };

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    [Fact]
    public async Task CheckoutAsync_InvalidQuantity_ShouldThrowInvalidQuantityException()
    {
        var order = CreateOrder(quantity: 0);

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>();
    }

    [Fact]
    public async Task CheckoutAsync_NegativePrice_ShouldThrowInvalidPriceException()
    {
        var order = CreateOrder(price: -10);

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidPriceException>();
    }

    [Fact]
    public async Task CheckoutAsync_InsufficientInventory_ShouldThrowException()
    {
        var order = CreateOrder();

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    [Fact]
    public async Task CheckoutAsync_ValidOrder_ShouldCalculateCorrectTotal()
    {
        var order = CreateOrder(price: 100, quantity: 2);

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var receipt = await _checkoutService.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(200);
        receipt.Discount.Should().Be(0);
        receipt.Total.Should().Be(200);
    }

    [Fact]
    public async Task CheckoutAsync_OrderAboveDiscountThreshold_ShouldApplyDiscount()
    {
        var order = CreateOrder(price: 1200, quantity: 1);

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var receipt = await _checkoutService.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(1200);
        receipt.Discount.Should().Be(100);
        receipt.Total.Should().Be(1100);
    }

    [Fact]
    public async Task CheckoutAsync_ValidOrder_ShouldSaveOrder()
    {
        var order = CreateOrder();

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _checkoutService.CheckoutAsync(order);

        _orderRepository.Verify(
            x => x.SaveAsync(
                order,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_ValidOrder_ShouldSendNotification()
    {
        var order = CreateOrder();

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _checkoutService.CheckoutAsync(order);

        _notificationService.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_SubtotalEqualsThreshold_ShouldApplyDiscount()
    {
        var order = CreateOrder(price: 1000, quantity: 1);

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var receipt = await _checkoutService.CheckoutAsync(order);

        receipt.Discount.Should().Be(100);
        receipt.Total.Should().Be(900);
    }

    [Fact]
    public async Task CheckoutAsync_HighSubtotal_ShouldCapDiscountAt100()
    {
        var order = CreateOrder(price: 5000, quantity: 1);

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var receipt = await _checkoutService.CheckoutAsync(order);

        receipt.Discount.Should().Be(100);
        receipt.Total.Should().Be(4900);
    }

    [Fact]
    public async Task CheckoutAsync_SubtotalBelowThreshold_ShouldNotApplyDiscount()
    {
        var order = CreateOrder(price: 999, quantity: 1);

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var receipt = await _checkoutService.CheckoutAsync(order);

        receipt.Discount.Should().Be(0);
        receipt.Total.Should().Be(999);
    }

    [Fact]
    public async Task CheckoutAsync_InsufficientInventory_ShouldNotSaveOrNotify()
    {
        var order = CreateOrder();

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<InsufficientInventoryException>();

        _orderRepository.Verify(
            x => x.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificationService.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_InvalidQuantity_ShouldNotSaveOrNotify()
    {
        var order = CreateOrder(quantity: 0);

        Func<Task> act = async () =>
            await _checkoutService.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>();

        _orderRepository.Verify(
            x => x.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificationService.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_NullRepository_ShouldThrowArgumentNullException()
    {
        Action act = () => new CheckoutService(
            null!,
            _inventoryService.Object,
            _notificationService.Object);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("orderRepository");
    }

    [Fact]
    public void Constructor_NullInventoryService_ShouldThrowArgumentNullException()
    {
        Action act = () => new CheckoutService(
            _orderRepository.Object,
            null!,
            _notificationService.Object);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("inventoryService");
    }

    [Fact]
    public void Constructor_NullNotificationService_ShouldThrowArgumentNullException()
    {
        Action act = () => new CheckoutService(
            _orderRepository.Object,
            _inventoryService.Object,
            null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("notificationService");
    }


    [Fact]
    public async Task CheckoutAsync_ShouldPassCancellationTokenToDependencies()
    {
        var order = CreateOrder();
        var cancellationToken = new CancellationTokenSource().Token;

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                cancellationToken))
            .ReturnsAsync(true);

        await _checkoutService.CheckoutAsync(order, cancellationToken);

        _inventoryService.Verify(
            x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                cancellationToken),
            Times.Once);

        _orderRepository.Verify(
            x => x.SaveAsync(order, cancellationToken),
            Times.Once);

        _notificationService.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                cancellationToken),
            Times.Once);
    }


}