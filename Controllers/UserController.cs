﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.IO;
using A.Data;
using A.Models;
using Microsoft.AspNetCore.Identity;

namespace Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = ApplicationUser.ADMIN_ROLE)]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        UserManager<ApplicationUser> _userManager;
        public UserController(ApplicationDbContext context, 
            ILogger<GuideController> logger,
                UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager
            )
        {
            _context = context;
            _logger = logger;
              _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserDTO>>> Get(
            int pageSize, int pageNumber, string sortField, string sortOrder, string name)
        {
            if (sortField == "id")
            {
                sortField = "UserIDid";
            }
            var items = _context.ApplicationUser 
                .Where(x => string.IsNullOrWhiteSpace(name) 
                || x.FirstName.Contains(name)
                || x.LastName.Contains(name)
                );

            var result =  items.ToPaged(pageNumber, pageSize,sortField, sortOrder);
            return GetDTOResult(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> Get(int id)
        {
            var item = _context.Users.FirstOrDefault(x=>x.UserIDid ==id);
            if (item == null)
            {
                return NotFound();
            }
            return new UserDTO(item);
        }

        public PagedResult<UserDTO> GetDTOResult (PagedResult<ApplicationUser> result)
        {
             var dtoResult = new PagedResult<UserDTO>
            {
                totalCount = result.totalCount,
                items = result.items
                .Select( x => new UserDTO(x)).ToList()
            };
            return dtoResult;
        }
    
          [HttpPut()]
        public async Task<ActionResult<PagedResult<UserDTO>>> UpdateStatusOrDelete(MultUpdateDeleteAction action)
        {
            var items = _context.Users.Where(x => action.ids.Contains(x.UserIDid));
            var result = items.ToPaged(0, action.ids.Length, "UserIDid", "desc");
            var dtoResut = GetDTOResult(result);
            if (action.status == "delete")
            {
                _context.RemoveRange(items);
            }
            _context.SaveChanges();
            return dtoResut;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute]int id,[FromBody] UserDTO dto)
        {
            if (id != dto.id)
            {
                return BadRequest();
            }

            var item = _context.ApplicationUser.FirstOrDefault(x => x.UserIDid == dto.id);
            _context.Entry(item).State = EntityState.Modified;

            item.FirstName = dto.FirstName;
            item.LastName = dto.LastName;
            item.Role = dto.Role;
            item.PhoneNumber = dto.PhoneNumber;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Exists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Guide>> Post(UserDTO item)
        {
            try
            {
                _context.AddUser(_userManager, item,true);
                return CreatedAtAction("Get", new { item.id }, item);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

        [HttpDelete("{id}")]
        public async Task<ActionResult<UserDTO>> Delete(int id)
        {
            var item = _context.ApplicationUser.FirstOrDefault(x => x.UserIDid == id);
            if (item == null)
            {
                return NotFound();
            }
            _context.Remove(item);
            await _context.SaveChangesAsync();
            return new UserDTO(item);
        }

        private bool Exists(int id)
        {
            return _context.Guide.Any(e => e.id == id);
        }
    }
}
