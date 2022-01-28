# Emote Effects Demo

A stripped-down version of the bot featuring primarily the emote effects, and some simple overlays.

To set this up:

* Download the release build
* Go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID and Client Secret.
    * Enter any name for the application, but ideally something descriptive
    * Use `http://localhost:5000/TASagentBotAPI/OAuth/BotCode` and `http://localhost:5000/TASagentBotAPI/OAuth/BroadcasterCode` as the OAuth Redirect URLs, and choose "Chat Bot" as the category.
* Create a Twitch account to act as the bot
* Run the application.
    * You will be prompted for several values, and it will prepare configuration files in your `Documents/TASagentBotEmoteEffectsDemo` directory.
* Once it's configured, close the application with Ctrl + Q

## Controlling the bot

To access the control page, open your web browser and go to the page `http://localhost:5000/API/ControlPage.html` and enter the admin password you chose. By default, this page will _not_ be accessible from other computers or the internet, unless your router is configured to forward web requests (it generally will not be).  
From another computer on your network (like your phone or a tablet), you may be able to access the control page by going to `http://192.168.1.50:5000/API/ControlPage.html` where `192.168.1.50` is your computer's local IP address.  

## Optional Features

* For the Emote Rain, add a full-screen browser source in OBS and set the url to `http://localhost:5000/BrowserSource/emoteRain.html`.
* For the Timer overlay, add a Browser Source in OBS with 600px Width and 400px Height and set the url to `http://localhost:5000/BrowserSource/timer.html`.
* For the SNES Input Display, add a Browser Source in OBS with 400px Width and 100px Height and set the url to `http://localhost:5000/BrowserSource/ControllerSpy.html`. You'll also need to properly set the input COM port on the control page.
