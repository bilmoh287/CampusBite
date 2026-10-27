namespace CampusBite.Services.Network;

public static class ApiConstants
{
#if ANDROID
    // Android emulator routes the host development machine's loopback to 10.0.2.2
    public const string BaseUrl = "http://10.0.2.2:5278";
#else
    // Windows, macOS, and iOS Simulator directly share localhost loopback
    public const string BaseUrl = "http://localhost:5278";
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
