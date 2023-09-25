using System;
using System.Collections.Generic;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
public class OrderMessage
{
    public string Id { get; set; }
    public IReadOnlyCollection<OrderItem> OrderItems { get; set; }
    public decimal Total { get; set; }

    public OrderMessage(List<OrderItem> items, decimal total)
    {
        Id = Guid.NewGuid().ToString();
        OrderItems = items;
        Total = total;
    }
}
