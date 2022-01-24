# WebTTS Only Demo

A stripped-down version of the bot featuring only the TTS command executed by webserver, sound effects, and the associated overlay.

To set this up:

* Download the release build
* Go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID and Client Secret.
    * Enter any name for the application, but ideally something descriptive
    * Use `http://localhost:5000/TASagentBotAPI/OAuth/BotCode` and `http://localhost:5000/TASagentBotAPI/OAuth/BroadcasterCode` as the OAuth Redirect URLs, and choose "Chat Bot" as the category.
* Create a Twitch account to act as the bot
* Run the application.
    * You will be prompted for several values, and it will prepare configuration files in your `Documents/TASagentBotWebTTSOnly` directory.
* Once it's configured, close the application with Ctrl + Q
* Edit `Documents/TASagentBotWebTTSOnly/Config/TTSConfig.json` to change features about the channel point redemption.

## Controlling the bot

To skip the current TTS, you can click on the bot window and press the `s` key, or you can type `!skip` in chat.  
To access the control page, open your web browser and go to the page `http://localhost:5000/API/ControlPage.html` and enter the admin password you chose. By default, this page will _not_ be accessible from other computers or the internet, unless your router is configured to forward web requests (it generally will not be).  
From another computer on your network (like your phone or a tablet), you may be able to access the control page by going to `http://192.168.1.50:5000/API/ControlPage.html` where `192.168.1.50` is your computer's local IP address.  
From the control page, you can Approve/Reject and replay TTS Requests. You can also approve a TTS request with `!approve #` in chat, where # is the TTS request number (seen in the console).

## Optional Features

* For the TTS Marquee (scrolling TTS text), add a Browser Source in OBS with a height of ~60 pixels and set the url to `http://localhost:5000/BrowserSource/ttsmarquee.html`.
* For the Emote Rain, add a full-screen browser source in OBS and set the url to `http://localhost:5000/BrowserSource/emoteRain.html`.
* For the Timer overlay, add a Browser Source in OBS with 600px Width and 400px Height and set the url to `http://localhost:5000/BrowserSource/timer.html`.
* For the SNES Input Display, add a Browser Source in OBS with 400px Width and 100px Height and set the url to `http://localhost:5000/BrowserSource/ControllerSpy.html`.