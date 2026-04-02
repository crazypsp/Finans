using Finans.Application.Abstractions.Identity;
using Finans.Application.Dtos.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly IUserService _users;
        private readonly IRoleService _roles;

        public AdminController(IUserService users, IRoleService roles)
        {
            _users = users;
            _roles = roles;
        }

        public async Task<IActionResult> Users(CancellationToken ct)
        {
            var list = await _users.GetUsersAsync(ct);
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateUser() => View(new UserCreateRequest());

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateRequest request, CancellationToken ct)
        {
            try
            {
                var id = await _users.CreateUserAsync(request, ct);
                return RedirectToAction(nameof(AssignRole), new { userId = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignRole(int userId, CancellationToken ct)
        {
            ViewBag.UserId = userId;
            ViewBag.Roles = await _roles.GetRolesAsync(ct);
            return View(new AssignRoleRequest { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(AssignRoleRequest request, CancellationToken ct)
        {
            try
            {
                await _users.AssignRoleAsync(request, ct);
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Roles = await _roles.GetRolesAsync(ct);
                return View(request);
            }
        }
    }
}