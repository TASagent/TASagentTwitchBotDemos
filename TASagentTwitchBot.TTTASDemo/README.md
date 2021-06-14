# Text-To-TAS Demo

A stripped-down version of the bot featuring only Text-To-TAS, sound effects, and overlays.

To set this up:

* Download the release build
* Go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID and Client Secret.
    * Enter any name for the application, but ideally something descriptive
    * Use `http://localhost:5000/TASagentBotAPI/OAuth/BotCode` and `http://localhost:5000/TASagentBotAPI/OAuth/BroadcasterCode` as the OAuth Redirect URLs, and choose "Chat Bot" as the category.
* Create a Twitch account to act as the bot
* Run the application.
    * You will be prompted for several values, and it will prepare configuration files in your `Documents/TASagentBotDemo` directory.
* Once it's configured, close the application with Ctrl + Q
* Edit `Documents/TASagentBotDemo/Config/TTTAS/TTTASConfig.json` to change features about the channel point redemption.
* Add a full-screen Browser Source in OBS and set the url to `http://localhost:5000/BrowserSource/TTTASOverlay.html`.