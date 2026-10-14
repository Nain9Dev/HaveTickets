using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HaveTickets.WebUI;
using HaveTickets.WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
    ? "http://localhost:8190/" 
    : "https://havetickets.onrender.com/"; // To be configured for production hosting

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
