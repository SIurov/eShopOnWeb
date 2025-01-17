﻿using System.Net;
using System.Text;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Services;
using Newtonsoft.Json;
using static Azure.Core.HttpHeader;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private string? _username = null;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly ICatalogViewModelService _catalogViewModelService;
    private readonly IAppLogger<CheckoutModel> _logger;

    public CheckoutModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
         ICatalogViewModelService catalogViewModelService,
        IAppLogger<CheckoutModel> logger)
    {
        _basketService = basketService;
        _signInManager = signInManager;
        _orderService = orderService;
        _basketViewModelService = basketViewModelService;
        _logger = logger;
        _catalogViewModelService = catalogViewModelService;
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        await SetBasketModelAsync();
    }

    public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
    {
        try
        {
            await SetBasketModelAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
            await _basketService.SetQuantities(BasketModel.Id, updateModel);
            await _orderService.CreateOrderAsync(BasketModel.Id, new Address("123 Main St.", "Kent", "OH", "United States", "44240"));

            // Module 7
            var names = await _catalogViewModelService.GetCatalogItemNames(items.Select(x => x.Id).ToArray());
            var body = JsonConvert.SerializeObject(items.Select(x => x.Quantity).Zip(names).ToDictionary(b => b.Item2, b => b.Item1));
            var queue = new QueueClient("Endpoint=sb://finalmsiurovsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=JgxsXFAKvRGszgF/qxPDdD86MkG13Xo/5+ASbA/9jAM=",
                "orders");
            await queue.SendAsync(new Message(Encoding.UTF8.GetBytes(body)));

            // Module 5
            var jsonContent = JsonContent.Create(new
            {
                ShippingAddress = "123 Main St. Kent OH, United States, 44240",
                Items = BasketModel.Items.ToDictionary(x => x.ProductName, x => items.First(y => y.CatalogItemId == x.CatalogItemId).Quantity),
                Total = BasketModel.Items.Sum(x => x.UnitPrice * items.First(y => y.CatalogItemId == x.CatalogItemId).Quantity)

            });
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-functions-key", "YqWra-jURBJ8Yxgw4da5CTjkviIBYBNbhLLsjbF60lCEAzFuJLes2Q==");
            var result = await httpClient.PostAsync("https://finalmorderitemsreserver.azurewebsites.net/api/CosmosReserverFunction", jsonContent);


            /*Module 4*/
            /* var names = await _catalogViewModelService.GetCatalogItemNames(items.Select(x => x.Id).ToArray());
             var jsonContent = JsonContent.Create(items.Select(x => x.Id).Zip(names).ToDictionary(b => b.Item2, b => b.Item1));
             var httpClient = new HttpClient();
             httpClient.DefaultRequestHeaders.Add("x-functions-key", "gCOPcuN20NEb_kHs0Q-raCAMWflfleQIHqjefHGOhiaYAzFuwkDrZg==");
             var result = await httpClient.PostAsync("https://module4funcapp.azurewebsites.net/api/reserverFunction", jsonContent);*/


            await _basketService.DeleteBasketAsync(BasketModel.Id);
        }
        catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
        {
            //Redirect to Empty Basket page
            _logger.LogWarning(emptyBasketOnCheckoutException.Message);
            return RedirectToPage("/Basket/Index");
        }

        return RedirectToPage("Success");
    }

    private async Task SetBasketModelAsync()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        if (_signInManager.IsSignedIn(HttpContext.User))
        {
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
        }
        else
        {
            GetOrSetBasketCookieAndUserName();
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username!);
        }
    }

    private void GetOrSetBasketCookieAndUserName()
    {
        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            _username = Request.Cookies[Constants.BASKET_COOKIENAME];
        }
        if (_username != null) return;

        _username = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
    }
}
