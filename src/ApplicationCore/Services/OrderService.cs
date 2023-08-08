using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
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
        var orderMessage = new OrderMessage(basket.BuyerId, shippingAddress, items, order.Total());

        await _orderRepository.AddAsync(order);
        await SendHttpRequestOnTrigger(orderMessage);
    }

    private async Task SendHttpRequestOnTrigger(OrderMessage order)
    {
        try
        {
            //var httpRequestMessage = new HttpRequestMessage(
            //    HttpMethod.Post,
            //    "https://orderitemsreserver-func.azurewebsites.net/api/OrderItemsReserver?code=X8zAmUCdtlQ5f9CuEBJYCMwSNcZoHmzFk1VZVABfTvLlAzFuYqWl1Q==")
            //{
            //    Headers =
            //    {
            //        { HeaderNames.Accept, "*/*" },
            //        { HeaderNames.UserAgent, "HttpRequestsSample" }
            //    },
            //    Content = new StringContent(JsonConvert.SerializeObject(order), System.Text.Encoding.UTF8, "application/json")
            //};

            var content = new StringContent(JsonConvert.SerializeObject(order), System.Text.Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.PostAsync(_functionUrl, content);
            //var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            //var client = new HttpClient();
            //var response = await client.PostAsync(_functionUrl, content);
            //var res = response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Can not send a message to trigger.");
        }
    }
}
