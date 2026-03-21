using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordHelper
{
    public static (string hash, string salt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty");

        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        string salt = Convert.ToBase64String(saltBytes);

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
        {
            byte[] hashBytes = pbkdf2.GetBytes(32);
            string hash = Convert.ToBase64String(hashBytes);
            return (hash, salt);
        }
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty");

        if (string.IsNullOrEmpty(storedHash))
            throw new ArgumentException("Stored hash cannot be null or empty");

        if (string.IsNullOrEmpty(storedSalt))
            throw new ArgumentException("Stored salt cannot be null or empty");

        try
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                byte[] testHash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(testHash) == storedHash;
            }
        }
        catch (FormatException ex)
        {
            throw new Exception("Invalid hash or salt format. Expected Base64 string.", ex);
        }
    }
}