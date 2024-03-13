using System;
using System.Text;
using System.Security.Cryptography;
using SalesBotApi.Models;
using Newtonsoft.Json;

public class JwtService
{
    private static string secret = "foo-bar-001";
    private static TimeSpan tokenLifetime = TimeSpan.FromHours(24);

    public static string CreateToken(UserWithPassword fullUser)
    {
        UserWithJwt authorizedUser = new UserWithJwt()
       {
           id = fullUser.id,
           user_name = fullUser.user_name,
           company_id = fullUser.company_id
       };
       return CreateToken(authorizedUser);
    }

    public static string CreateToken(UserWithJwt authorizedUser)
    {
        // Header
        var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var headerBytes = Encoding.UTF8.GetBytes(header);
        var headerBase64 = Convert.ToBase64String(headerBytes);

        JwtPayload payload = new JwtPayload
        {
            id = authorizedUser.id,
            user_name = authorizedUser.user_name,
            company_id = authorizedUser.company_id,
            role = authorizedUser.role,
            exp = DateTimeOffset.UtcNow.Add(tokenLifetime).ToUnixTimeSeconds()
        };
        string payloadStr = JsonConvert.SerializeObject(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadStr);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);

        // Signature
        var signature = $"{headerBase64}.{payloadBase64}";
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signature));
            var signatureBase64 = Convert.ToBase64String(signatureBytes);

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }
    }

    public static JwtPayload ValidateAndDecodeToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null; // Invalid token format
        }

        var header = parts[0];
        var payload = parts[1];
        var signature = parts[2];

        var computedSignature = ComputeJwtSignature(header, payload, secret);
        if (signature != computedSignature)
        {
            return null; // Signature validation failed
        }

        return DecodePayload(payload);
    }

    private static string ComputeJwtSignature(string header, string payload, string secret)
    {
        var signature = $"{header}.{payload}";
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signature));
            return Convert.ToBase64String(signatureBytes);
        }
    }

    private static JwtPayload DecodePayload(string encodedPayload)
    {
        var jsonBytes = Convert.FromBase64String(encodedPayload);
        string payloadStr = Encoding.UTF8.GetString(jsonBytes);
        JwtPayload deserializedPayload = JsonConvert.DeserializeObject<JwtPayload>(payloadStr);
        return deserializedPayload;
    }
}
