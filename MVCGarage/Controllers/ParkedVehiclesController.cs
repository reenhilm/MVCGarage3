﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCGarage.Data;
using MVCGarage.Models.Entities;
using MVCGarage.Models.ViewModels;

namespace MVCGarage.Controllers
{
    public class ParkedVehiclesController : Controller
    {
        private readonly MVCGarageContext _context;

        public ParkedVehiclesController(MVCGarageContext context)
        {
            _context = context;
        }

        // GET: ParkedVehicles
        public async Task<IActionResult> Index()
        {
            return _context.ParkedVehicle != null ?
                        View(await _context.ParkedVehicle.ToListAsync()) :
                        Problem("Entity set 'MVCGarageContext.ParkedVehicle'  is null.");
        }

        // GET: ParkedVehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ParkedVehicle == null)
            {
                return NotFound();
            }

            var parkedVehicle = await _context.ParkedVehicle
                .FirstOrDefaultAsync(m => m.Id == id);
            if (parkedVehicle == null)
            {
                return NotFound();
            }
            return View(parkedVehicle);
        }

        // GET: ParkedVehicles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ParkedVehicles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Color,Type,RegistrationNumber,Brand,Model,WheelCount,ArrivalTime")] ParkedVehicle parkedVehicle)
        {
            if (ModelState.IsValid)
            {
                _context.Add(parkedVehicle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(parkedVehicle);
        }

        // GET: ParkedVehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ParkedVehicle == null)
            {
                return NotFound();
            }

            var parkedVehicle = await _context.ParkedVehicle.FindAsync(id);
            if (parkedVehicle == null)
            {
                return NotFound();
            }
            var parkedVehicleVM = new ChangeViewModel()
            {
                Id = parkedVehicle.Id,
                Brand = parkedVehicle.Brand,
                Color = parkedVehicle.Color,
                Colors = Enum.GetValues<Color>()
                                .Select(g => new SelectListItem
                                {
                                    Text = g.ToString(),
                                    Value = g.ToString()
                                })
                                .ToList(),
                Model = parkedVehicle.Model,
                RegistrationNumber = parkedVehicle.RegistrationNumber,
                Type = parkedVehicle.Type,
                Types = Enum.GetValues<VehicleType>()
                                .Select(g => new SelectListItem
                                {
                                    Text = g.ToString(),
                                    Value = g.ToString()
                                })
                                .ToList(),
                WheelCount = parkedVehicle.WheelCount

            };
            return View(parkedVehicleVM);
        }

        // POST: ParkedVehicles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChangeViewModel parkedVehicle)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var v = new ParkedVehicle()
                    {
                        Id = parkedVehicle.Id,
                        Color = parkedVehicle.Color,
                        Brand = parkedVehicle.Brand,
                        Model = parkedVehicle.Model,
                        RegistrationNumber = parkedVehicle.RegistrationNumber,
                        Type = parkedVehicle.Type,
                        WheelCount = parkedVehicle.WheelCount
                    };
                    _context.Update(v);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParkedVehicleExists(parkedVehicle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(parkedVehicle);
        }

        // GET: ParkedVehicles/Checkout/5
        public async Task<IActionResult> Checkout(int? id)
        {
            if (id == null || _context.ParkedVehicle == null)
            {
                return NotFound();
            }

            var parkedVehicle = await _context.ParkedVehicle
                .FirstOrDefaultAsync(m => m.Id == id);
            if (parkedVehicle == null)
            {
                return NotFound();
            }

            return View(parkedVehicle);
        }

        // POST: ParkedVehicles/Checkout/5
        [HttpPost, ActionName("Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirmed(int id)
        {
            if (_context.ParkedVehicle == null)
            {
                return Problem("Entity set 'MVCGarageContext.ParkedVehicle'  is null.");
            }
            var parkedVehicle = await _context.ParkedVehicle.FindAsync(id);
            if (parkedVehicle != null)
            {
                _context.ParkedVehicle.Remove(parkedVehicle);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ParkedVehicleExists(int id)
        {
            return (_context.ParkedVehicle?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
