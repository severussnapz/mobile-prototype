using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class NoOpSecretEncryptionService : ISecretEncryptionService
{
    public string Encrypt(string plaintext)
    {
        return plaintext;
    }

    public string Decrypt(string ciphertext)
    {
        return ciphertext;
    }

    public string Mask(string ciphertext)
    {
        return "***";
    }

    public string MaskWithSuffix(string ciphertext, int suffixLength)
    {
        return "***";
    }
}
