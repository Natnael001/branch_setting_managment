using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordHelper
{
    private const int KeySize = 64;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;

    public static (string hash, string salt) HashPassword(string password, string username)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required");

        string normalizedUsername = username.ToLowerInvariant();

        byte[] saltBytes = RandomNumberGenerator.GetBytes(KeySize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password + normalizedUsername),
            saltBytes,
            Iterations,
            _hashAlgorithm,
            KeySize
        );

        return (Convert.ToHexString(hashBytes), Convert.ToHexString(saltBytes));
    }

    public static bool VerifyPassword(string password, string username, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            return false;

        try
        {
            string normalizedUsername = username.ToLowerInvariant();

            byte[] saltBytes = Convert.FromHexString(storedSalt);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password + normalizedUsername),
                saltBytes,
                Iterations,
                _hashAlgorithm,
                KeySize
            );

            return Convert.ToHexString(hashBytes).Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}