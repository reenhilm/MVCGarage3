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
using MVCGarage.Models.ViewModels;

namespace MVCGarage.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly MVCGarageContext _context;
        private readonly IOptions<PriceSettings> options;

        public VehiclesController(MVCGarageContext context, IOptions<PriceSettings> options)
        {
            _context = context;
            this.options = options;
        }

        public async Task<IActionResult> Index(ListViewModel lvm)
        {
            if (_context.Vehicle != null)
            {
                lvm.HasExpandedSearchItem = 
                    lvm.SearchType != null || lvm.SearchWheelCount != null ||
                    !string.IsNullOrEmpty(lvm.SearchBrand) || !string.IsNullOrEmpty(lvm.SearchModel);

                var dbVehicles = await _context.Vehicle!
                    .Include(v => v.Member)
                    .Join(_context.VehicleAssignment!,
                    v => v.Id,
                    va => va.VehicleId,
                    (v, va) => new { vehicle = v, asgnmt = va })                    
                    .WhereIf(lvm.SearchRegistrationNumber != null, x => x.vehicle.RegistrationNumber != null && x.vehicle.RegistrationNumber.StartsWith(lvm.SearchRegistrationNumber!.Trim()))
                    .WhereIf(lvm.SearchBrand != null, x => x.vehicle.Brand != null && x.vehicle.Brand.StartsWith(lvm.SearchBrand!.Trim()))
                    .WhereIf(lvm.SearchWheelCount != null, x => x.vehicle.WheelCount == lvm.SearchWheelCount)
                    .WhereIf(lvm.SearchModel != null, x => x.vehicle.Model != null && x.vehicle.Model.StartsWith(lvm.SearchModel!.Trim()))
                    .WhereIf(lvm.SearchType != null, x => x.vehicle.VehicleType.Id == lvm.SearchType)
                    .Select(v => new IndexVehicleViewModel()
                    {
                        Id = v.vehicle.Id,
                        RegistrationNumber = v.vehicle.RegistrationNumber,
                        Type = new VehicleType()
                        {
                            Id = v.vehicle.VehicleType.Id,
                            Name = v.vehicle.VehicleType.Name,
                            NeededSize = v.vehicle.VehicleType.NeededSize
                        },
                        ArrivalTime = v.asgnmt.ArrivalDate,
                        ParkedTime = DateTime.Now.Subtract(v.asgnmt.ArrivalDate),
                        Owner = $"{v.vehicle.Member.FirstName} {v.vehicle.Member.LastName}",
                        MembershipType = new Membership(v.vehicle.Member.ProMembershipToDate).Type
                    })                    
                    .ToListAsync();

                var orderedVehicles =
                    lvm.Order == Order.RegistrationNumber ? dbVehicles.OrderAscOrDesc(lvm.Desc, v => v.RegistrationNumber)
                  : lvm.Order == Order.Type ? dbVehicles.OrderAscOrDesc(lvm.Desc, v => v.Type.Id)
                  : lvm.Order == Order.ParkedTime ? dbVehicles.OrderAscOrDesc(lvm.Desc, v => v.ParkedTime)
                  : dbVehicles.OrderAscOrDesc(lvm.Desc, v => v.ArrivalTime);

                lvm.VehicleList = orderedVehicles.GroupBy(x => x.Id).Select(y => y.First());

                lvm.VehicleTypes = await _context.VehicleType.ToListAsync();
                return View(lvm);                
            }
            else return Problem("Entity set 'MVCGarageContext.Vehicle'  is null.");
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicle
                .Include(v => v.VehicleType)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            var member = await _context.Member.FirstOrDefaultAsync(m => m.Id == vehicle.MemberId);

            var vehicleType = vehicle.VehicleType;

            if (member == null || vehicleType == null)
            {
                return NotFound();
            }

            var model = new DetailsViewModel
            {
                Brand = vehicle.Brand,
                Color = vehicle.Color,
                Id = vehicle.Id,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                VehicleTypeName = vehicleType.Name,
                WheelCount = vehicle.WheelCount,
                OwnerFirstName = member.FirstName,
                OwnerLastName = member.LastName
            };

            var va = vehicle.VehicleAssignments.FirstOrDefault();

            if (va != null)
            {
                model.ArrivalTime = va.ArrivalDate;
                model.ParkedTime = DateTime.Now.Subtract(va.ArrivalDate);
            };

            return View(model);
        }

        public async Task<IActionResult> Add(int? id)
        {
            if (id == null || !await _context.Member.AnyAsync(m => m.Id == id))
            {
                return NotFound();
            }

            var avvm = new AddVehicleViewModel
            {
                MemberId = (int)id,
                VehicleTypes = await _context.VehicleType.ToListAsync()
            };

            return View(avvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddVehicleViewModel avvm)
        {
            if (ModelState.IsValid)
            {
                var vehicle = new Vehicle
                {
                    Brand = avvm.Brand,
                    Color = avvm.Color,
                    Model = avvm.Model,
                    RegistrationNumber = avvm.RegistrationNumber,
                    WheelCount = avvm.WheelCount,
                    MemberId = avvm.MemberId,
                    VehicleTypeId = avvm.VehicleTypeId
                };
                _context.Add(vehicle);
                await _context.SaveChangesAsync();
                avvm.AddSuccess = true;
            }
            return View(avvm);
        }


        // GET: Vehicles/Create
        public IActionResult Park()
        {
            var pvm = new ParkViewModel
            {
                Price = options.Value.HourPrice
            };
            return View(pvm);
        }

        public string MinifyAndUpperCaseString(string stringToMinify)
        {
            return stringToMinify.Replace(" ", "").Replace("-", "");
        }

        // POST: Vehicles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Park(ParkViewModel pvm)
        {
            if (ModelState.IsValid)
            {
                pvm.RegistrationNumber = MinifyAndUpperCaseString(pvm.RegistrationNumber!);
                pvm.Error = "";
                await Task.Delay(100);
                throw new Exception("TODO Arrivaltime changed, type changed");
                /*
                var Vehicle = new Vehicle
                {
                    Brand = pvm.Brand,
                    Color = pvm.Color,
                    Id = pvm.Id,
                    Model = pvm.Model,
                    RegistrationNumber = pvm.RegistrationNumber,
                    //Type = pvm.Type,
                    WheelCount = pvm.WheelCount
                    //ArrivalTime = DateTime.Now
                };

                _context.Add(Vehicle);
                bool bParkSuccess = true;
                try
                {
                    await _context.SaveChangesAsync();
                }
                //TODO: Log the error somewhere
                catch(DbUpdateException e)
                {
                    if (e.InnerException != null && e.InnerException.Message.StartsWith("Cannot insert duplicate"))
                        pvm.Error = "A vehicle with that registration number is already parked. Try modifying the vehicle instead.";
                    else
                    {
                        pvm.Error = "Your vehicle was not parked due to an error";
                    }
                    bParkSuccess = false;
                }
                catch
                {
                    pvm.Error = "Your vehicle was not parked due to an error";
                    bParkSuccess = false;
                }
                pvm.ParkSuccess = bParkSuccess;
                */
            }
            return View(pvm);
        }

        public async Task<IActionResult> CheckIfRegIsUnique(string registrationNumber)
        {
            try
            {
                if (await _context.Vehicle!.AnyAsync(v => v.RegistrationNumber == registrationNumber))
                    return Json("A vehicle with that registration number is already parked. Try modifying the vehicle instead.");
            }
            catch
            {                
            }
            //if database messed up on validation it is not a big deal if this was not validated (since it is not to be trusted once we reach backend), we validate once more on database index
            return Json(true);
        }

        // GET
        public async Task<IActionResult> Modify(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicle
                .Include(v => v.VehicleType)
                .Include(v => v.Member)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            var viewModel = new ChangeViewModel()
            {
                Id = vehicle.Id,
                Brand = vehicle.Brand,
                Color = vehicle.Color,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                WheelCount = vehicle.WheelCount,
                VehicleTypeName = vehicle.VehicleType.Name,
                OwnerFirstName = vehicle.Member.FirstName,
                OwnerLastName = vehicle.Member.LastName
            };
            return View(viewModel);

        }

        // POST
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modify(ChangeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                viewModel.RegistrationNumber = MinifyAndUpperCaseString(viewModel.RegistrationNumber!);
                viewModel.Error = "";

                var vehicle = new Vehicle()
                {
                    Id = viewModel.Id,
                    WheelCount = viewModel.WheelCount,
                    Model = viewModel.Model,
                    RegistrationNumber = viewModel.RegistrationNumber,
                    Brand = viewModel.Brand,
                    Color = viewModel.Color
                };

                _context.Update(vehicle);

                _context.Entry(vehicle).Property(x => x.MemberId).IsModified = false;

                // TODO: Possibly add change VehicleType functionality. Would require checking if Vehicle is parked and if there's space in the garage.
                _context.Entry(vehicle).Property(x => x.VehicleTypeId).IsModified = false;

                await _context.SaveChangesAsync();
                viewModel.ModifySuccess = true;
                return View(viewModel);
            }
            
            return View(viewModel);
        }

        // GET: Vehicles/Checkout/5
        public async Task<IActionResult> Checkout(int? id)
        {
            if (id == null || _context.Vehicle == null)
            {
                return NotFound();
            }

            var Vehicle = await _context.Vehicle
                .Include(x => x.VehicleAssignments)                
                .FirstOrDefaultAsync(m => m.Id == id);
            if (Vehicle == null)
            {
                return NotFound();
            }

            var ArrivalTime = Vehicle.VehicleAssignments.ToList()[0].ArrivalDate;
            var parkedTime = DateTime.Now.Subtract(ArrivalTime);

            var cvm = new CheckoutViewModel()
            {
                ArrivalTime = ArrivalTime,
                Brand = Vehicle.Brand,
                Color = Vehicle.Color,
                CheckoutTime = DateTime.Now,
                Id = Vehicle.Id,
                Model = Vehicle.Model,
                Price = CalculatePrice(parkedTime.TotalHours),
                ParkedTime = parkedTime,
                RegistrationNumber = Vehicle.RegistrationNumber,
                Type = Vehicle.VehicleType,
                WheelCount = Vehicle.WheelCount
            };
            
            return View(cvm);
        }

        private decimal CalculatePrice(double totalHour)
        {
            int iHourPrice = options.Value.HourPrice;
            return (decimal)(totalHour * iHourPrice);
        }

        // POST: Vehicles/Checkout/5
        [HttpPost, ActionName("Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirmed(int id)
        {
            if (_context.Vehicle == null)
            {
                return Problem("Entity set 'MVCGarageContext.Vehicle'  is null.");
            }

            ReceiptViewModel rvm = new();
            var Vehicle = await _context.Vehicle
                .Include(v => v.VehicleAssignments)
                .Where(v => v.Id == id)
                .FirstOrDefaultAsync();

            if (Vehicle != null)
            {
                foreach(var va in Vehicle.VehicleAssignments)
                    _context.VehicleAssignment.Remove(va);

                var ArrivalTime = (DateTime)(Vehicle.VehicleAssignments.FirstOrDefault()?.ArrivalDate!);
                var parkedTime = DateTime.Now.Subtract(ArrivalTime);

                rvm = new ReceiptViewModel()
                {
                    ArrivalTime = ArrivalTime,
                    Brand = Vehicle.Brand,
                    Color = Vehicle.Color,
                    CheckoutTime = DateTime.Now,
                    Model = Vehicle.Model,
                    Price = CalculatePrice(parkedTime.TotalHours),
                    ParkedTime = parkedTime,
                    RegistrationNumber = Vehicle.RegistrationNumber,
                    Type = Vehicle.VehicleType
                };
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Receipt), rvm);
        }

        public IActionResult Receipt(ReceiptViewModel rvm)
        {
            return View(rvm);
        }

        private bool VehicleExists(int id)
        {
            return (_context.Vehicle?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
