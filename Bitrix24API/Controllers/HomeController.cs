using Bitrix24API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bitrix24API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}