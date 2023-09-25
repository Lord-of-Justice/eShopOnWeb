using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _functionUrl;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IHttpClientFactory httpClientFactory,
        string functionUrl)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _httpClientFactory = httpClientFactory;
        _functionUrl = functionUrl;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);
        var orderMessage = new OrderMessage(items, order.Total());
        var deliverymessage = new OrderDeliveryMessage(basket.BuyerId, shippingAddress, items, order.Total());

        await _orderRepository.AddAsync(order);
        await SendMessageToServiceBus(orderMessage);
        await SendHttpRequestOnTrigger(deliverymessage);
    }

    private async Task SendMessageToServiceBus(OrderMessage order)
    {
        var serviceBusConnectionString = "Endpoint=sb://eshop-azureservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=aQgEx+kPHbBDVCi/P5pv0fKeH2DaVjXE6+ASbPa3AjY=";
        var queueName = "orders";

        await using var client = new ServiceBusClient(serviceBusConnectionString);
        await using ServiceBusSender sender = client.CreateSender(queueName);
        try
        {
            string messageBody = JsonConvert.SerializeObject(order);
            var message = new ServiceBusMessage(messageBody);
            await sender.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            throw new Exception($"Can not send a message to service bus. Exception: {ex.Message}");
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }

    private async Task SendHttpRequestOnTrigger(OrderDeliveryMessage order)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(order), System.Text.Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.PostAsync(_functionUrl, content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Can not send a message to trigger. Exception: {ex.Message}");
        }
    }
}
