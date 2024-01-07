using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace Engineer_MVC.Controllers
{
    
    public class TreatmentsController : CustomBaseController
    {
        private readonly EngineerContext _context;

        public TreatmentsController(EngineerContext context)
        {
            _context = context;
        }

        // GET: Treatments
        public async Task<IActionResult> Index()
        {
              return _context.Treatment != null ? 
                          View(await _context.Treatment.ToListAsync()) :
                          Problem("Entity set 'EngineerContext.Treatment'  is null.");
        }
        
        public IActionResult TreatmentsList()
        {
            return View();
        }
        public IActionResult Lashes()
        {
            return View();
        }
        public IActionResult Eyebrows()
        {
            return View();
        }
        public IActionResult Skin()
        {
            return View();
        }
        public IActionResult Nails()
        {
            return View();
        }
        // GET: Treatments/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Treatment == null)
            {
                return NotFound();
            }

            var treatment = await _context.Treatment
                .FirstOrDefaultAsync(m => m.Id == id);
            if (treatment == null)
            {
                return NotFound();
            }

            return View(treatment);
        }

        // GET: Treatments/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Treatments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Type,AverageTime,AverageCost")] Treatment treatment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(treatment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(treatment);
        }

        // GET: Treatments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Treatment == null)
            {
                return NotFound();
            }

            var treatment = await _context.Treatment.FindAsync(id);
            if (treatment == null)
            {
                return NotFound();
            }
            return View(treatment);
        }

        // POST: Treatments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Type,AverageTime,AverageCost")] Treatment treatment)
        {
            if (id != treatment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(treatment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TreatmentExists(treatment.Id))
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
            return View(treatment);
        }

        // GET: Treatments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Treatment == null)
            {
                return NotFound();
            }

            var treatment = await _context.Treatment
                .FirstOrDefaultAsync(m => m.Id == id);
            if (treatment == null)
            {
                return NotFound();
            }

            return View(treatment);
        }

        // POST: Treatments/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Treatment == null)
            {
                return Problem("Entity set 'EngineerContext.Treatment'  is null.");
            }
            var treatment = await _context.Treatment.FindAsync(id);
            if (treatment != null)
            {
                _context.Treatment.Remove(treatment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TreatmentExists(int id)
        {
          return (_context.Treatment?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
