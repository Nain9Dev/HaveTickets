using Microsoft.JSInterop;

namespace HaveTickets.WebUI.Services;

public class SessionService
{
    private readonly IJSRuntime _jsRuntime;
    private Guid? _userId;

    public SessionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<Guid> GetUserIdAsync()
    {
        if (_userId.HasValue)
        {
            return _userId.Value;
        }

        var storedIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "haveTickets_userId");
        if (!string.IsNullOrEmpty(storedIdStr) && Guid.TryParse(storedIdStr, out var storedId))
        {
            _userId = storedId;
            return storedId;
        }

        var newId = Guid.NewGuid();
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "haveTickets_userId", newId.ToString());
        _userId = newId;

        return newId;
    }
}
