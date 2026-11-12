
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using VerifyXunit;
using Xunit;

namespace Simulation.Checkout.Tests;


public class CheckoutServiceSnapshotTests
{
    [Fact]
    public async Task CheckoutAsync_ShouldMatchReceiptSnapshot()
    {
        var orderRepository = new Mock<IOrderRepository>();
        var inventoryService = new Mock<IInventoryService>();
        var notificationService = new Mock<INotificationService>();

        inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(
            orderRepository.Object,
            inventoryService.Object,
            notificationService.Object);

        var order = new Order
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Amr",
            CustomerEmail = "amr@yahoo.com",
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductName = "Laptop",
                    UnitPrice = 1200,
                    Quantity = 1
                },
                new()
                {
                    ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    ProductName = "Mouse",
                    UnitPrice = 200,
                    Quantity = 2
                }
            }
        };

        var receipt = await service.CheckoutAsync(order);

        var settings = new VerifySettings();
        settings.ScrubDateTimes();

        await Verify(receipt, settings);
    }
}
