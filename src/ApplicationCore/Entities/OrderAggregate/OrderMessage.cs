using System;
using System.Collections.Generic;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
public class OrderMessage
{
    public string Id { get; set; }
    //public string BuyerId { get; private set; }
    //public Address ShipToAddress { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems { get; private set; }
    public decimal Total { get; private set; }

    public OrderMessage(List<OrderItem> items, decimal total)
    {
        Id = Guid.NewGuid().ToString();
        OrderItems = items;
        Total = total;
    }

    //public OrderMessage(string buyerId, Address shipToAddress, List<OrderItem> items, decimal total)
    //{
    //    Id = Guid.NewGuid().ToString();
    //    BuyerId = buyerId;
    //    ShipToAddress = shipToAddress;
    //    OrderItems = items;
    //    Total = total;
    //}
}
