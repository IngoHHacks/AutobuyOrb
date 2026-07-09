## 1.1.4-beta
- Added an option (`MaxTimeFramePct`) to set a maximum time per frame for the autobuyer to run to reduce performance impact. By default, the autobuyer will run for a maximum of 50% of the time per frame (based on the FPS limit setting, 60 if FPS limit is set to unlimited). Note that it will only check the time per frame after each attribute purchase, so if an attribute takes a long time to purchase, it may exceed the time limit.
- Added an option (`BuyInterval`) to set a minimum time between autobuyer runs. By default, this is set to 0, meaning the autobuyer will run every frame. Setting it to a higher value will reduce the performance impact of the autobuyer, but may also reduce its effectiveness.
- Added an option (`MinBuy`) to set a minimum number of attributes of a type to buy at the same time. Set to 0 (default) to use the bulk buy limit.
- Added an option (`BuyMoreThanMin`) to buy as many attributes of a type as possible if the total cost is still below the limit.
- Added an option (`CheapestFirst`) to enable/disable buying attributes in order of cheapest to most expensive. If disabled, the autobuyer will buy attributes an arbitrary cycle. Cheapest first was the default behavior before this update, but this option is now disabled by default to reduce performance impact.
- Added an option (`CheatBypassQueuelimit`) for testing purposes to bypass the queue limit. This is not recommended for normal use, as it may cause issues with the game. It is only intended for testing and debugging purposes.
- All non-cheat config options are now editable in-game through the settings panel in the main menu. Cheat options (currently only `CheatBypassQueuelimit`) are not editable in-game and can only be changed in the config file due to not being intended for normal use.
- If minimum queue space left (negative bulk buy limit) is set to be greater than or equal to the total number of queue spaces, the autobuyer will now buy one attribute if the queue is empty, instead of doing nothing. This is to prevent the autobuyer from not doing anything in the early game when queue space is limited, if the minimum queue space left is set to a high number.

## 1.1.3
- Fixed negative bulk buy limit (leaving queue space) not working correctly.

## 1.1.2

- Fixed an issue where the autobuyer would consider attributes with a cost of less than 1 and a current resource
  quantity of 0 due to the BigDouble class handling infinity comparisons incorrectly, causing it to get stuck in a loop,
  resulting in significant slowdowns with high Max Bulk Purchase settings.
- Bulk buy limit can now be set to a negative number to limit purchases to leave the given number of queue space free.
  For example, setting the limit to -5 will make the autobuyer buy attributes until there are 5 free queue spaces left.
  This can be useful for players who want to keep some queue space free for manual purchases while still using the
  autobuyer to buy attributes automatically. Note that this option is treated as a 0 with an extra cap, so there is no
  limit to how many attributes can be bought in one tick besides the queue space.

## 1.1.1

- Keybinds are now configurable in the config file
- Added settings panel to the main menu, allowing editing the config file in-game and automatically applying changes
  without restarting the game
- Added a setting to increase the number of purchases per tick from the default of 1 to a higher number, allowing for
  faster autobuying at the cost of performance.
- Added a setting to increase the maximum tweens running at once to prevent the UI from breaking when buying many
  attributes at once and set the default to 4000 (up from 400).
- Added an option to enable/disable the autobuyer from considering the action modifier The action modifier would cause
  the autobuyer to buy more than one attribute at a time, resulting in higher costs than expected when set to a value
  greater than 1. The action modifier is now ignored by default, but can be enabled in the config file/settings panel.
- Changed the default keybinds to `LAlt+Period`, `LAlt+Comma`, and `LAlt+M` to avoid triggering the action modifier
  increase (if the 'respect action modifier' setting is enabled).

## 1.1.0

- Added purchasability display

## 1.0.0

- Initial release