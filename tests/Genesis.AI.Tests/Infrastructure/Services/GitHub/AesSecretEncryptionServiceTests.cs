using System.Security.Cryptography;
using Genesis.AI.Infrastructure.Services.GitHub;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class AesSecretEncryptionServiceTests : IDisposable
{
    private static readonly string ValidKey = Convert.ToBase64String(new byte[32]);
    private readonly string? _originalKey;

    public AesSecretEncryptionServiceTests()
    {
        _originalKey = Environment.GetEnvironmentVariable("SECRET_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("SECRET_ENCRYPTION_KEY", ValidKey);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var service = new AesSecretEncryptionService();

        var ciphertext = service.Encrypt("hello world");
        var plaintext = service.Decrypt(ciphertext);

        Assert.Equal("hello world", plaintext);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachCall()
    {
        var service = new AesSecretEncryptionService();

        var first = service.Encrypt("hello world");
        var second = service.Encrypt("hello world");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var service = new AesSecretEncryptionService();
        var ciphertext = service.Encrypt("test");

        var bytes = Convert.FromBase64String(ciphertext);
        bytes[bytes.Length / 2] ^= 0x01;
        var tamperedCiphertext = Convert.ToBase64String(bytes);

        Assert.Throws<CryptographicException>(() => service.Decrypt(tamperedCiphertext));
    }

    [Fact]
    public void Mask_ReturnsEightBullets_NeverDecrypts()
    {
        var service = new AesSecretEncryptionService();
        var ciphertext = service.Encrypt("secret");

        var masked = service.Mask(ciphertext);

        Assert.Equal("\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022", masked);
        Assert.DoesNotContain("secret", masked, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_MissingEnvironmentVariable_ThrowsInvalidOperationException()
    {
        Environment.SetEnvironmentVariable("SECRET_ENCRYPTION_KEY", null);

        Assert.Throws<InvalidOperationException>(() => _ = new AesSecretEncryptionService());
    }

    [Fact]
    public void Constructor_KeyTooShort_ThrowsInvalidOperationException()
    {
        Environment.SetEnvironmentVariable("SECRET_ENCRYPTION_KEY", Convert.ToBase64String(new byte[16]));

        Assert.Throws<InvalidOperationException>(() => _ = new AesSecretEncryptionService());
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("SECRET_ENCRYPTION_KEY", _originalKey);
    }
}
