using System;
using System.Collections.Generic;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
public class OrderDeliveryMessage : OrderMessage
{
    public string BuyerId { get; set; }
    public Address ShipToAddress { get; set; }

    public OrderDeliveryMessage(string buyerId, Address shipToAddress, List<OrderItem> items, decimal total)
        : base(items, total)
    {
        Id = Guid.NewGuid().ToString();
        BuyerId = buyerId;
        ShipToAddress = shipToAddress;
        OrderItems = items;
        Total = total;
    }
}
