using Microsoft.JSInterop;

namespace SystemMonitor.Dashboard.Services;

public sealed class ThemeService(IJSRuntime js)
{
    private const string StorageKey = "theme";
    private string _theme = "dark";

    public string Theme => _theme;
    public bool IsDark => _theme == "dark";
    public event Action? OnChange;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        _theme = saved is "light" or "dark" ? saved : "dark";
        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        _theme = _theme == "dark" ? "light" : "dark";
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, _theme);
        await ApplyAsync();
        OnChange?.Invoke();
    }

    private Task ApplyAsync() =>
        js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", _theme).AsTask();
}
