﻿using System.Threading.Tasks;
using BRM.BL.Exceptions;
using BRM.BL.Models.UserDto;
using BRM.BL.Models.UserRoleDto;
using BRM.BL.UserService;
using BRM.DAO.Entities;
using BRM.DAO.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BRM.BL.UsersRolesService
{
    public class UsersRolesService : IUsersRolesService
    {
        public IUserService UserService { get; }
        private readonly IRepository<UsersRoles> _usersRoles;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleService;

        public UsersRolesService(
            IUserService userService,
            IRepository<User> userRepository,
            IRepository<UsersRoles> usersRoles,
            IRepository<Role> roleService)
        {
            UserService = userService;
            _userRepository = userRepository;
            _usersRoles = usersRoles;
            _userRepository = userRepository;
            _roleService = roleService;
        }

        public async Task<UserReturnDto> AddRoleToUser(UserRoleOrPermissionUpdateDto dto)
        {
            var user =
                await _userRepository.GetByIdAsync(dto.UserId);

            if (user == null)
            {
                throw new ObjectNotFoundException("User not found.");
            }

            var role =
                await _roleService.GetByIdAsync(dto.RoleOrPermissionId);

            if (role == null)
            {
                throw new ObjectNotFoundException("Role not found.");
            }

            var userToRoleConnection =
                await (await _usersRoles.GetAllAsync(d => d.User == user && d.Role == role))
                    .FirstOrDefaultAsync();

            if (userToRoleConnection != null)
            {
                throw new ObjectNotFoundException("User already have role.");
            }

            var userToRoleForDb = new UsersRoles
            {
                User = user,
                Role = role
            };

            var connection = (await _usersRoles.InsertAsync(userToRoleForDb));

            return await UserService.GetUser(connection.User.UserName);
        }

        public async Task<UserReturnDto> DeleteRoleFromUser(UserRoleOrPermissionUpdateDto dto)
        {
            var user =
                await _userRepository.GetByIdAsync(dto.UserId);

            if (user == null)
            {
                throw new ObjectNotFoundException("User not found.");
            }

            var userToRoleConnection =
                await (await _usersRoles.GetAllAsync(d => d.User == user && d.Role.Id == dto.RoleOrPermissionId))
                    .FirstOrDefaultAsync();

            if (userToRoleConnection == null)
            {
                throw new ObjectNotFoundException("User role not found.");
            }

            var removedRole = await _usersRoles.RemoveAsync(userToRoleConnection);

            return await UserService.GetUser(removedRole.User.UserName);
        }

        public async Task DeleteAllRoleConnections(long roleId)
        {
            var role =
                await _roleService.GetByIdAsync(roleId);

            if (role == null)
            {
                throw new ObjectNotFoundException("Role not found.");
            }

            var allRoleConnections =
                await (await _usersRoles.GetAllAsync(d => d.Role == role)).ToListAsync();

            foreach (var roleConnection in allRoleConnections)
            {
                await _usersRoles.RemoveAsync(roleConnection);
            }
        }
    }
}