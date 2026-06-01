using System.Security.Cryptography;

namespace DllTeste.Banco.Autenticacao;

/// <summary>
/// Hash de senha PBKDF2-SHA256. Sem dependências externas.
/// Formato armazenado: {iteracoes}.{saltBase64}.{hashBase64}
///
///   var hash = PasswordHasher.Hash("minhaSenha");      // ao cadastrar
///   bool ok  = PasswordHasher.Verificar(senha, hash);  // ao validar
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;        // 128 bits
    private const int KeySize = 32;         // 256 bits
    private const int Iteracoes = 100_000;

    public static string Hash(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            senha, salt, Iteracoes, HashAlgorithmName.SHA256, KeySize);

        return $"{Iteracoes}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(hashArmazenado))
            return false;

        string[] partes = hashArmazenado.Split('.', 3);
        if (partes.Length != 3)
            return false;

        if (!int.TryParse(partes[0], out int iteracoes))
            return false;

        byte[] salt;
        byte[] hashEsperado;
        try
        {
            salt = Convert.FromBase64String(partes[1]);
            hashEsperado = Convert.FromBase64String(partes[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
            senha, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

        // Comparação em tempo constante (evita timing attack)
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
