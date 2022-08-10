﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MVCGarage.Data;
using MVCGarage.Models.Entities;
using MVCGarage.Models.ViewModels.Members;

namespace MVCGarage.Controllers
{
    public class MembersController : Controller
    {
        private readonly MVCGarageContext _context;
        private readonly IOptions<PriceSettings> options;

        public MembersController(MVCGarageContext context, IOptions<PriceSettings> options)
        {
            _context = context;
            this.options = options;
        }

        public async Task<IActionResult> Index(ListViewModel lvm)
        {
            if (_context.Member != null)
            {
                var dbMembers = await _context.Member!
                    .Select(m => new IndexMemberViewModel()
                    {
                        Id = m.Id,
                        FirstName = m.FirstName,
                        LastName = m.LastName,
                        NrOfVehicles = m.Vehicles.Count
                    })
                    .ToListAsync();
                lvm.MemberList = dbMembers;
                dbMembers.Sort(new TwoFirstCaseSensitiveOnModelsFirstname());
                return View(lvm);                
            }
            else return Problem("Entity set 'MVCGarageContext.Member'  is null.");
        }

        public class TwoFirstCaseSensitiveOnModelsFirstname : IComparer<Object>
        {
            public int Compare(object? x, object? y)
            {
                var xModel = x as IndexMemberViewModel;
                var yModel = y as IndexMemberViewModel;

                return string.Compare(xModel?.FirstName, 0, yModel?.FirstName, 0, 2, false);
            }
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Member == null)
            {
                return NotFound();
            }

            var Member = await _context.Member
                .Include(m => m.Vehicles)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (Member == null)
            {
                return NotFound();
            }

            var model = new DetailsViewModel
            {
                Id = Member.Id,
                FirstName = Member.FirstName,
                LastName = Member.LastName,
                Vehicles = Member.Vehicles.Select(v => new MemberVehicleModel()
                {
                    Id = v.Id,
                    Brand = v.Brand,
                    Model = v.Model,
                    RegistrationNumber = v.RegistrationNumber
                }).ToList()
            };

            return View(model);
        }
    }
}