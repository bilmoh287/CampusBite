namespace CampusBite.Services.Network;

public static class ApiConstants
{
#if ANDROID
    // Virtual emulator uses 10.0.2.2; Physical phone connected via USB uses localhost (via adb reverse)
    public static string BaseUrl => 
        Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual
            ? "http://10.0.2.2:5278"
            : "http://localhost:5278";
#else
    // Windows, macOS, and iOS Simulator directly share localhost loopback
    public static string BaseUrl => "http://localhost:5278";
#endif

    public const string AuthLogin = "/api/auth/login";
    public const string AuthRegister = "/api/auth/register";
    public const string AuthRefreshToken = "/api/auth/refresh-token";
    public const string AuthRevokeToken = "/api/auth/revoke-token";
    public const string AuthMe = "/api/auth/me";

    public const string Cafes = "/api/cafes";
    public const string Products = "/api/products";
    public const string Orders = "/api/orders";
}
