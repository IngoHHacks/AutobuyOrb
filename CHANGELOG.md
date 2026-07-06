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