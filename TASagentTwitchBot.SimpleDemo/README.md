# Simple Demo

A simple, but complete, demo of how to customize the TASagent Twitch Bot.

## Commands

Shows the creation of custom command-handlers.

### TestCommandSystem

Adds a `!test` command which triggers an image notification.

### UpTimeSystem

Adds an `!uptime` command which reports to chat how long the stream has been live.

## Database

Expands the database to include a `SupplementalData` table, wherein customized records reside.

## Notifications.CustomActivityProvider

Overrides the default `FullActivityProvider` to instead thank the followers anonymously instead of calling them out.  Also adds the custom testing method that `TestCommandSystem` invokes.

## PointsSpender

Adds a system of classes to handle the creation and management of a tracked channel point sink.

### PointSpenderHandler

The class that creates and manages the channel point redemption.  Also responsible for executing some of the chat commands.

### PointSpenderSystem

Registers the chat commands to interact with the `PointsSpender`.  Adds a `!points` command that checks your own spent channel points, a `!points <username>` command that checks how many channel points another user has spent, and a `!leaderboard` command that prints the top 5 points spenders.

## Web.Controllers.TestController

Adds REST endpoints for triggering some new functionality.  `TestNotification()` is invoked by POST calls to `/TASagentBotAPI/Test/TestNotification`, and `TestUptime()` is invoked by POST calls to `/TASagentBotAPI/Test/TestUptime`.

## Web.Startup

Registers all of the new classes so they can be constructed.