using BudgetManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace BudgetManagement.Services;

public class UsuarioStore: IUserStore<Usuario>, IUserEmailStore<Usuario>, IUserPasswordStore<Usuario>
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public UsuarioStore(IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorioUsuarios = repositorioUsuarios;
    }
    
    public async Task<IdentityResult> CreateAsync(Usuario user, CancellationToken cancellationToken)
    {
        user.Id = await _repositorioUsuarios.CrearUsuario(user);
        return IdentityResult.Success;
    }
    
    public async Task<Usuario> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _repositorioUsuarios.BuscarUsuarioPorEmail(normalizedEmail);
    }
    
    public async Task<Usuario> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await _repositorioUsuarios.BuscarUsuarioPorEmail(normalizedUserName);
    }
    
    public Task<string> GetUserIdAsync(Usuario user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }
    
    public Task<string> GetEmailAsync(Usuario user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }
    
    public Task<string> GetPasswordHashAsync(Usuario user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }
    
    public Task<string> GetUserNameAsync(Usuario user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }
    
    public Task SetNormalizedEmailAsync(Usuario user, string normalizedEmail, CancellationToken cancellationToken)
    {
        user.EmailNormalizado = normalizedEmail;
        return Task.CompletedTask;
    }
    
    public Task SetNormalizedUserNameAsync(Usuario user, string normalizedName, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public Task SetPasswordHashAsync(Usuario user, string passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }
    
    
    
    
    public void Dispose()
    {
    }

    

    

    public Task SetUserNameAsync(Usuario user, string userName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetNormalizedUserNameAsync(Usuario user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    


    public async Task<IdentityResult> UpdateAsync(Usuario user, CancellationToken cancellationToken)
    {
        await _repositorioUsuarios.Actualizar(user);
        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(Usuario user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Usuario> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    

    public Task SetEmailAsync(Usuario user, string email, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    

    public Task<bool> GetEmailConfirmedAsync(Usuario user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetEmailConfirmedAsync(Usuario user, bool confirmed, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    

    public Task<string> GetNormalizedEmailAsync(Usuario user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    

    

    

    public Task<bool> HasPasswordAsync(Usuario user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}