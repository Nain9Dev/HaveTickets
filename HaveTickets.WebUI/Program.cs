using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HaveTickets.WebUI;
using HaveTickets.WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
    ? "http://localhost:8190/" 
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddTransient<OfflineFallbackHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<OfflineFallbackHandler>();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseAddress) };
});
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
