using System.ComponentModel.DataAnnotations.Schema;

namespace DllTeste.Banco.Models;

/// <summary>
/// Tabela de usuários para validação do login (fase pessoal).
/// Quando migrar para a empresa (AD), esta tabela pode deixar de ser usada
/// para autenticação e virar só um espelho de perfis, se quiser.
/// </summary>

[Table("Usuarios")]

public class Usuario
{
    public int Id { get; set; }

    public string NomeUsuario { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash gerado por PasswordHasher.Hash(...). Nunca a senha em texto.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public string NomeExibicao { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
