using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebStore.Contexts;
using WebStore.Models;
using WebStore.Models.DTOs;

namespace WebStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, ProductDTO product)
        {
            // Get logged in user name
            var user = string.Empty;
            if (HttpContext.User.Identity is ClaimsIdentity identity)
            {
                user = identity.FindFirst(ClaimTypes.Name).Value;
            }

            var prod = _mapper.Map<Product>(product);
            prod.ProductId = id;
            prod.ModifiedBy = user;
            _context.Entry(prod).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) { return NotFound(); }
                else { throw; }
            }

            return NoContent();
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(ProductDTO product)
        {
            // Get logged in user name
            int user = -1;
            if (HttpContext.User.Identity is ClaimsIdentity identity)
            {
                user = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
            }

            string username = _context.Users.Where(x => x.Id == user).FirstOrDefault().Username;

            var prod = _mapper.Map<Product>(product);
            prod.CreatedBy = username; prod.ModifiedBy = username;
            _context.Products.Add(prod);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {

                throw;
            }

            int id = _context.Products.Where(x => x.ProductId == prod.ProductId).FirstOrDefault().ProductId;

            return CreatedAtAction("GetProduct", new { id = id }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("UploadImage")]
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                var formCollection = await Request.ReadFormAsync();
                var file = formCollection.Files.First();
                var folderName = Path.Combine("Resources", "Images");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (file.Length > 0)
                {
                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    var fullPath = Path.Combine(pathToSave, fileName);
                    var dbPath = Path.Combine(folderName, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create)) { file.CopyTo(stream); }

                    return Ok(new { fileName });
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("UploadProduct")]
        public async Task<IActionResult> UploadProduct()
        {
            try
            {
                var formCollection = await Request.ReadFormAsync();
                var file = formCollection.Files.First();

                var folderName = Path.Combine("Resources", "Products");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                // Images path
                var imagesFolderName = Path.Combine("Resources", "Images");
                var imagesPathToSave = Path.Combine(Directory.GetCurrentDirectory(), imagesFolderName);

                // Get logged in user name
                int user = -1;
                if (HttpContext.User.Identity is ClaimsIdentity identity)
                {
                    user = int.Parse(identity.FindFirst(ClaimTypes.Name).Value);
                }

                string username = _context.Users.Where(x => x.Id == user).FirstOrDefault().Username;

                if (file.Length > 0)
                {
                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    var fullPath = Path.Combine(pathToSave, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create)) { file.CopyTo(stream); }

                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using (var stream = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            List<Product> products = new List<Product>();
                            do
                            {
                                var x = 0;
                                while (reader.Read())
                                {
                                    Product prod = new Product();
                                    if (x != 0)
                                    {
                                        for (int column = 0; column < reader.FieldCount; column++)
                                        {
                                            string val = (string)reader.GetValue(column);
                                            if (column == 0) { prod.ProductCode = val; };
                                            if (column == 1) { prod.ProductName = val; };
                                            if (column == 2) { prod.ProductDescription = val; };
                                            if (column == 3) { prod.CategoryName = val; };
                                            if (column == 4) { prod.Price = decimal.Parse(val); };
                                            if (column == 5)
                                            {
                                                Guid g = Guid.NewGuid();
                                                var image = await DownloadFile(val);
                                                System.IO.File.WriteAllBytes($"{imagesPathToSave}/{g}.png", image);
                                                prod.PhotoFileName = $"{g}.png";
                                            }
                                            prod.CreatedBy = username; prod.ModifiedBy = username;
                                        }
                                    }
                                    if (prod != null && prod.ProductCode != null && prod.ProductName != null && prod.ProductDescription != null
                                        && prod.CategoryName != null && prod.PhotoFileName != null)
                                    { products.Add(prod); _context.Products.Add(prod); _context.SaveChanges(); }
                                    x++;
                                }
                            } while (reader.NextResult());
                        }
                    }

                    return Ok(new { fileName });
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        private static async Task<byte[]> DownloadFile(string url)
        {
            using (var client = new HttpClient())
            {

                using (var result = await client.GetAsync(url))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsByteArrayAsync();
                    }

                }
            }
            return null;
        }
    }
}