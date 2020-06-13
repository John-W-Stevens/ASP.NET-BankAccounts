using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // for session
using Microsoft.AspNetCore.Identity; // for password hashing
using BankAccounts.Models;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {

        private BAContext dbContext;

        public HomeController(BAContext context)
        {
            dbContext = context;
        }

        // ROUTE:               METHOD:                VIEW:
        // -----------------------------------------------------------------------------------
        // GET("")              Index()                Index.cshtml
        // POST("/register")    Create(User user)      ------ (Index.cshtml to display errors)
        // POST("/login")       Login(LoginUser user)  ------ (Index.cshtml to display errors)
        // GET("/logout")       Logout()               ------
        // GET("/success")      Success()              Success.cshtml

        [HttpGet("")]
        public IActionResult Index()
        {
            //List<User> AllUsers = dbContext.Users.ToList();
            return View();
        }

        [HttpPost("/register")]
        public IActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                // If a User exists with provided email
                if (dbContext.Users.Any(u => u.Email == user.Email))
                {
                    // Manually add a ModelState error to the Email field
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }

                // hash password
                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                user.Password = Hasher.HashPassword(user, user.Password);

                // create user
                dbContext.Add(user);
                dbContext.SaveChanges();

                // sign user into session
                var NewUser = dbContext.Users.FirstOrDefault(u => u.Email == user.Email);
                int UserId = NewUser.UserId;
                HttpContext.Session.SetInt32("UserId", UserId);

                // go to success
                return RedirectToAction("Account");
            }
            // display errors
            else
            {
                return View("Index");
            }
        }

        [HttpPost("/login")]
        public IActionResult Login(LoginUser user)
        {
            if (ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == user.LoginEmail);
                if (userInDb == null)
                {
                    // Add an error to ModelState and return to View!
                    ModelState.AddModelError("LoginEmail", "Invalid Email/Password");
                    return View("Index");
                }
                // Initialize hasher object
                var hasher = new PasswordHasher<LoginUser>();

                // verify provided password against hash stored in db
                var result = hasher.VerifyHashedPassword(user, userInDb.Password, user.LoginPassword);
                if (result == 0)
                {
                    // handle failure (this should be similar to how "existing email" is handled)
                    ModelState.AddModelError("LoginPassword", "Password is invalid.");
                    return View("Index");
                }

                // sign user into session
                int UserId = userInDb.UserId;
                HttpContext.Session.SetInt32("UserId", UserId);

                return RedirectToAction("Account");
            }
            // display errors
            else
            {
                return View("Index");
            }
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet("success")]
        public IActionResult Success()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpGet("/account")]
        public IActionResult Account()
        {
            ViewBag.ErrorMessage = "";
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index");
            }
            int uId = userId ?? default(int);

            User user = dbContext.Users
                .Include(u => u.CreatedTransactions)
                .FirstOrDefault(u => u.UserId == uId);
            
            return View("Account", user);
        }

        [HttpPost("/account")]
        public IActionResult UpdateAccount()
        {

            int? userId = HttpContext.Session.GetInt32("UserId");
            int uId = userId ?? default(int);
            User user = dbContext.Users
                .Include(u => u.CreatedTransactions)
                .FirstOrDefault(u => u.UserId == uId);

            if (Request.Form["Amount"] == "")
            {
                ViewBag.ErrorMessage = "Invalid submission. Please enter a valid amount.";
                return View("Account", user);
            }

            decimal amount = Convert.ToDecimal(Request.Form["Amount"]);

            if (user.Balance + amount < 0)
            {
                ViewBag.ErrorMessage = "Insufficient funds for this transaction.";
                return View("Account", user);
            }

            Transaction transaction = new Transaction() { Amount = amount, UserId = user.UserId };
            ViewBag.ErrorMessage = "";
            dbContext.Add(transaction);
            dbContext.SaveChanges();

            user.Balance += amount;
            user.UpdatedAt = DateTime.Now;
            dbContext.SaveChanges();

            return RedirectToAction("Account");
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

