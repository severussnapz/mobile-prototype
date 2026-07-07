namespace Genesis.AI.Domain.Interfaces;

public interface ISecretEncryptionService
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);

    string Mask(string ciphertext);
}