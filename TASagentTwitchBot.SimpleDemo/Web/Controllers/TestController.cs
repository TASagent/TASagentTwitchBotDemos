using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.SimpleDemo.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Test/[action]")]
public class TestController : ControllerBase
{
    private readonly Notifications.CustomActivityProvider customActivityProvider;
    private readonly Commands.UpTimeSystem upTimeSystem;

    public TestController(
        Notifications.CustomActivityProvider customActivityProvider,
        Commands.UpTimeSystem upTimeSystem)
    {
        this.customActivityProvider = customActivityProvider;
        this.upTimeSystem = upTimeSystem;
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult TestNotification()
    {
        customActivityProvider.TestNotification();
        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public async Task<ActionResult> TestUptime()
    {
        await upTimeSystem.PrintUpTime();
        return Ok();
    }
}
