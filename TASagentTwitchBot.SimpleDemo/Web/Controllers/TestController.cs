using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.SimpleDemo.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Test/[action]")]
public class TestController : ControllerBase
{
    private readonly Commands.UpTimeSystem upTimeSystem;

    public TestController(
        Commands.UpTimeSystem upTimeSystem)
    {
        this.upTimeSystem = upTimeSystem;
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public async Task<ActionResult> TestUptime()
    {
        await upTimeSystem.PrintUpTime();
        return Ok();
    }
}
