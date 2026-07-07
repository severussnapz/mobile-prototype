using System.Security.Cryptography;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class AesSecretEncryptionService : ISecretEncryptionService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private static readonly int TagSizeBytes = AesGcm.TagByteSizes.MaxSize;

    private readonly byte[] _key;

    public AesSecretEncryptionService()
    {
        var keyValue = Environment.GetEnvironmentVariable("SECRET_ENCRYPTION_KEY")
            ?? throw new InvalidOperationException("Environment variable 'SECRET_ENCRYPTION_KEY' was not found.");

        _key = Convert.FromBase64String(keyValue);
        if (_key.Length != KeySizeBytes)
        {
            throw new InvalidOperationException("Environment variable 'SECRET_ENCRYPTION_KEY' must decode to exactly 32 bytes.");
        }
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        var payload = Convert.FromBase64String(ciphertext);
        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Ciphertext payload is invalid.");
        }

        var nonce = payload[..NonceSizeBytes];
        var tag = payload[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var encryptedBytes = payload[(NonceSizeBytes + TagSizeBytes)..];
        var plaintextBytes = new byte[encryptedBytes.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);

        try
        {
            aes.Decrypt(nonce, encryptedBytes, tag, plaintextBytes);
        }
        catch (AuthenticationTagMismatchException exception)
        {
            throw new CryptographicException("Ciphertext authentication failed.", exception);
        }

        return System.Text.Encoding.UTF8.GetString(plaintextBytes);
    }

    public string Mask(string ciphertext) => "••••••••";
}