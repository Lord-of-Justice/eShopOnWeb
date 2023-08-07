using System.Collections.Generic;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
public class OrderMessage
{
    public Address ShipToAddress { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems { get; private set; }
    public decimal Total { get; private set; }

    public OrderMessage(Address shipToAddress, List<OrderItem> items, decimal total)
    {
        ShipToAddress = shipToAddress;
        OrderItems = items;
        Total = total;
    }
}
