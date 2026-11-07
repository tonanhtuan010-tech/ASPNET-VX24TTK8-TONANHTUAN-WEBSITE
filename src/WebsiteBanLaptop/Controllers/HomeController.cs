using Microsoft.AspNetCore.Mvc;

namespace WebsiteBanLaptop.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
}
