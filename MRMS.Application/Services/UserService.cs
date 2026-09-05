using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Security;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UserService(IUnitOfWork uow, IPermissionService permissions,
        ICurrentUserService currentUser, IAuditService audit)
    {
        _uow = uow;
        _permissions = permissions;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        _permissions.Require(Permission.ManageUsers);
        var users = await _uow.Users.GetAllAsync();
        return users.OrderBy(u => u.Id).Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> GetTechniciansAsync()
    {
        var users = await _uow.Users.FindAsync(u => u.Role == UserRole.Technician && u.IsActive);
        return users.OrderBy(u => u.FullName).Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        _permissions.Require(Permission.ManageUsers);
        var user = await _uow.Users.GetByIdAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        _permissions.Require(Permission.ManageUsers);

        var username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("اسم المستخدم مطلوب");
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("الاسم الكامل مطلوب");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4)
            throw new InvalidOperationException("كلمة المرور يجب أن تكون 4 أحرف على الأقل");

        var existing = await _uow.Users.FindAsync(u => u.Username == username);
        if (existing.Any())
            throw new InvalidOperationException($"اسم المستخدم \"{username}\" مستخدم بالفعل");

        var (hash, salt) = PasswordHasher.Hash(dto.Password);
        var user = new User
        {
            Username = username,
            FullName = dto.FullName.Trim(),
            Role = dto.Role,
            IsActive = true,
            PasswordHash = hash,
            PasswordSalt = salt,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();
            await _audit.LogAsync("User.Create", nameof(User), user.Id, $"{username} ({dto.Role})");
            await _uow.CompleteAsync();
        });

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
    {
        _permissions.Require(Permission.ManageUsers);

        var user = await _uow.Users.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("الاسم الكامل مطلوب");

        var demotingLastOwner = user.Role == UserRole.Owner && dto.Role != UserRole.Owner;
        var deactivatingLastOwner = user.Role == UserRole.Owner && !dto.IsActive;
        if (demotingLastOwner || deactivatingLastOwner)
        {
            var otherActiveOwners = (await _uow.Users.FindAsync(
                u => u.Role == UserRole.Owner && u.IsActive && u.Id != user.Id)).Count();
            if (otherActiveOwners == 0)
                throw new InvalidOperationException("لا يمكن تنزيل أو إيقاف آخر حساب مالك نشط");
        }

        if (_currentUser.User?.Id == user.Id && dto.Role != user.Role)
            throw new InvalidOperationException("لا يمكنك تغيير دور حسابك أنت");

        user.FullName = dto.FullName.Trim();
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            _uow.Users.Update(user);
            await _audit.LogAsync("User.Update", nameof(User), user.Id,
                $"{user.Username}: {dto.Role}, {(dto.IsActive ? "نشط" : "موقوف")}");
            await _uow.CompleteAsync();
        });

        return MapToDto(user);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        _permissions.Require(Permission.ManageUsers);

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
            throw new InvalidOperationException("كلمة المرور يجب أن تكون 4 أحرف على الأقل");

        var user = await _uow.Users.GetByIdAsync(dto.UserId)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        (user.PasswordHash, user.PasswordSalt) = PasswordHasher.Hash(dto.NewPassword);
        user.MustChangePassword = true;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            _uow.Users.Update(user);
            await _audit.LogAsync("User.ResetPassword", nameof(User), user.Id, user.Username);
            await _uow.CompleteAsync();
        });
    }

    private static UserDto MapToDto(User u) =>
        new(u.Id, u.Username, u.FullName, u.Role, u.IsActive, u.LastLoginAt);
}
