# AutoBuyOrb

Autobuyer for Orb Of Creation.

### Installation

AutoBuyOrb is a BepInEx mod. You can find instructions on how to install BepInEx [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html). *TLDR: Download BepInEx, extract it to the game's root folder, and (optionally) run the game once to generate the BepInEx folder structure.*
The latest version of BepInEx can be downloaded from [here](https://github.com/BepInEx/BepInEx/releases). Only BepInEx 5.x is supported. The BepInEx 6.x beta will not work with this mod.

After installing BepInEx, download the latest release of AutoBuyOrb from the [releases page](https://github.com/IngoHHacks/AutobuyOrb/releases/latest) and place the `AutobuyOrb.dll` file in the `BepInEx/plugins` folder. (If `plugins` does not exist, create it.)

For Linux/Steam Deck users, you should set the following launch option to make sure BepInEx works correctly:
```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

If you want to report bugs, please send the log file located in `BepInEx/LogOutput.log` along with a description of the issue. You can also enable the console window in the config file (Set `Enabled` to `true` in `BepInEx/config/BepInEx.cfg` under the `[Logging.Console]` section) to see the log output in real time.

### Keybinds

(Configurable in the config file/settings panel)

`LAlt+Period` - Cycle modes  
`LAlt+Comma` - Cycle modes backwards  
`LAlt+Equals` - Cycle Min Buy amount
`LAlt+Minus` - Cycle Min Buy amount backwards
`LAlt+M` - Buy max (one time)  
`F1` - Toggle affordability display

### Modes

When enabled, the autobuyer will continuously buy attributes based on the selected mode. The modes are:

`Disabled` - No autobuying  
`Buy all` - Buy all attributes  
`Buy at 10 times excess` - Buy all attributes that cost at least 10 times less than your current resources  
`Buy at 100 times excess` - Buy all attributes that cost at least 100 times less than your current resources  
`Buy at 1000 times excess` - Buy all attributes that cost at least 1000 times less than your current resources

### Affordability display

When enabled, attributes will display the number of times you can afford to buy them based on your current resources in
square brackets next to their current level.  
The affordability display will be updated every game tick.  
Affordability also accounts for whether there is enough room in the queue and will be capped based on the current free
queue space. If you could afford more than the free queue space, the display will show a `*` next to the displayed
number to indicate that you could afford more if queue space was available.

### Extra information

When enabled, the autobuyer will buy attributes every game frame if possible based on the mode. The number of frames per second depends on your FPS limit setting.

The number of attributes bought per tick is limited by the Max Bulk Buy setting.
The default Max Bulk Buy setting is 1, meaning the autobuyer will only buy one attribute per tick.
Setting Max Bulk Buy to 0 will remove the limit (except the limit of available queue space). Setting Max Bulk Buy to a negative number will limit purchases to leave the given number of queue space free. For example, setting the limit to -5 will make the autobuyer buy attributes until there are 5 free queue spaces left.

If `BuyInterval` is set to a value greater than 0, the autobuyer will only run every `BuyInterval` seconds instead of every frame. This can be a decimal value (e.g. 0.5).

The buy max function buys as many attributes as possible in one tick, regardless of most settings, including Max Bulk Buy, Min Buy, Buy Interval, and autobuyer mode. It will still respect the queue space limit and will not buy locked attributes or non-attribute purchases.

All modes and the buy max function work cheapest to most expensive, relative to the ratio of the cost to your current
resources. For attributes with multiple costs, the autobuyer considers the most expensive cost (relative to your current
resources) when determining whether to buy it and in what order to buy it.

Locked attributes and non-attribute purchases will not be bought.

Extra info about the configuration can be found in the config file `AutobuyOrb.cfg` in the `BepInEx/config` folder. Some config entries can also be edited in-game through the settings panel in the main menu.