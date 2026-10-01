using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace VibeCast.Infrastructure.Storage;

public static class Helpers
{
    public static string BuildOwnerKey(string ownerId)
    {
        byte[] ownerBytes = Encoding.UTF8.GetBytes(ownerId);

        byte[] hash = SHA256.HashData(ownerBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
