using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Data;
using Engineer_MVC.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Engineer_MVC.Data.Interfaces;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Engineer_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace Engineer_MVC.Controllers
{
    public class PostsController : CustomBaseController
    {
        private readonly EngineerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IPostService _postService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        public PostsController(EngineerContext context, 
            IWebHostEnvironment hostEnvironment,
            IPostService postService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _postService = postService;
            _localizer = localizer;
        }

        // GET: Posts
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            IQueryable<Post> postsQuery;
            if (currentCulture == "it-IT")
            {
                postsQuery = _context.Post.Where(p => p.Language == "Italiano");
            }
            else if (currentCulture == "pl-PL")
            {
                postsQuery = _context.Post.Where(p => p.Language == "Polski");
            }
            else
            {
                postsQuery = _context.Post;
            }

            var posts = postsQuery.Where(p=>p.IsVisible==true).ToList();

            return View(posts);
        }
        public IActionResult PostsView()
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            IQueryable<Post> postsQuery;
            if (currentCulture == "it-IT")
            {
                postsQuery = _context.Post.Where(p => p.Language == "Italiano");
            }
            else if (currentCulture == "pl-PL")
            {
                postsQuery = _context.Post.Where(p => p.Language == "Polski");
            }
            else
            {
                postsQuery = _context.Post;
            }

            var posts = postsQuery.Where(p => p.IsVisible == true).OrderByDescending(t => t.date).ToList();
            return View(posts);
        }
        public async Task<IActionResult> SinglePost(int? id)
        {
            if (id == null || _context.Post == null)
            {
                return NotFound();
            }

            var post = await _context.Post
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            TempData["PostDate"] = post.date.ToString();
            TempData["PostTitle"] = post.Title;
            return View(post);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult PolishPosts()
        {
            var posts = _context.Post.Where(a => a.Language == "Polski");
            return View(posts);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult ItalianPosts()
        {
            var posts = _context.Post.Where(a => a.Language == "Italiano");
            return View(posts);
        }
        public async Task<IActionResult> IndexAdmin(string selectedLanguage)
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            string italiano = null; string polish = null;
            if (currentCulture == "it-IT")
            {
                italiano = "Italiano";
                polish = "Polacco";
            }
            else
            {
                italiano = "Włoski";
                polish = "Polski";
            }
            IQueryable<Post> postsQuery = _context.Post;

            if (!string.IsNullOrEmpty(selectedLanguage))
            {
                postsQuery = selectedLanguage switch
                {
                    "Italiano" => postsQuery.Where(p => p.Language == "Italiano"),
                    "Włoski" => postsQuery.Where(p => p.Language == "Italiano"),
                    "Polacco"=> postsQuery.Where(p => p.Language == "Polski"),
                    "Polski" => postsQuery.Where(p => p.Language == "Polski"),
                    _ => postsQuery
                };
            }

            var posts = await postsQuery.ToListAsync();
            var model = new PostsAdminViewModel
            {
                Posts = posts,
                SelectedLanguage = selectedLanguage,
                LanguageOptions = new SelectList(new[] { italiano, polish })
            };

            return View(model);
        }
        public async Task<IActionResult> LoadPosts(int skip)
        {
            try
            {
                var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
                IQueryable<Post> postsQuery;
                if (currentCulture == "it-IT")
                {
                    postsQuery = _context.Post.Where(p => p.Language == "Italiano");
                }
                else if (currentCulture == "pl-PL")
                {
                    postsQuery = _context.Post.Where(p => p.Language == "Polski");
                }
                else
                {
                    postsQuery = _context.Post;
                }

                var posts = postsQuery.Where(p => p.IsVisible == true).OrderByDescending(t => t.date).Skip(skip).Take(6).ToList();

                return PartialView("_PostsPartial", posts);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }


        // GET: Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Post == null)
            {
                return NotFound();
            }

            var post = await _context.Post
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }
        [Authorize(Roles = "Admin")]
        // GET: Posts/Create
        public IActionResult Create()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Body,IsVisible,Language,ImageFile")] Post post, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    post.ImagePath = await _postService.UploadImage(file);
                }
                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Posts/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Post == null)
            {
                return NotFound();
            }

            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Body,IsDeleted,IsVisible,Language")] Post post, IFormFile file)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.Post.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingPost == null)
                    {
                        return NotFound();
                    }
                    if (file != null)
                    {
                        // Delete the previous image if it exists
                        if (!string.IsNullOrEmpty(existingPost.ImagePath))
                        {
                            _postService.DeleteImage(existingPost.ImagePath);
                        }

                        // Upload the new image
                        post.ImagePath = await _postService.UploadImage(file);
                    }
                    else
                    {
                        // If no new image was provided, keep the existing image path
                        post.ImagePath = existingPost.ImagePath;
                    }
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
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
            return View(post);
        }
        [Authorize(Roles = "Admin")]
        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Post == null)
            {
                return NotFound();
            }

            var post = await _context.Post
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Post == null)
            {
                return Problem("Entity set 'EngineerContext.Post'  is null.");
            }
            var post = await _context.Post.FindAsync(id);
            if (!string.IsNullOrEmpty(post.ImagePath))
            {
                _postService.DeleteImage(post.ImagePath);
            }
            if (post != null)
            {
                _context.Post.Remove(post);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
          return (_context.Post?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
