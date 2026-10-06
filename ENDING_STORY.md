# Ending narrative and continued play

The hidden threshold remains **10,000 cheese in the wallet**. Crossing it latches a pending final letter without stopping collection or cutting off its visual/audio feedback. `TownSession.LateUpdate` waits for both world and town pickup flights, queued credits, and the counter pulse to settle before publishing the letter. Closing a page settles its existing effects normally. Spending cheese after the threshold does not erase the queued milestone.

The last letter is selected by the existing latest-unread mailbox rule. Merely receiving or viewing it does not start the story: the player must press **MARK AS READ** on that letter. This freezes gameplay and production and starts the existing three-part epilogue. The opening remains unchanged. Skipping the epilogue also leads to the choice, never silently chooses an answer.

After the story, the old GAME OVER banner is replaced by:

> You can keep gathering cheese.
> Though no mouse needs it anymore.
> Keep going?

- **YES** restores exploration, collecting, production, upgrades, and music. The final letter and ending do not repeat, including after scene reloads.
- **NO** exits the application; in the Unity Editor it stops Play mode.

The interaction header and gameplay HUD both arrange navigation as Great Tree, Mail, Supplies. The HUD Supplies button opens the shop directly with its normal sound and card entrance. The supplied `UI_cancel.png` replaces the back arrow: clicking it (or Escape) closes the tablet from any page. The HUD and header tree icons both show the existing red-dot artwork while the awakened tree's stock is at capacity. Harvesting clears them; the existing unread-mail dot remains independent.

The choice uses the supplied transparent `UI_empty_frame-export.png` at half its export size. Its separate black shade covers the full viewport at 62% opacity; the old opaque tablet background is hidden. The viewport shade and centered wooden frame remain separate editable objects.

The enabled MARK AS READ button gently pulses between 55% and 100% opacity every 1.2 seconds. It pulses only on the settled mail page while the selected letter is unread, stops immediately after acknowledgement, and leaves hover/pressed color feedback intact. These values and the CanvasGroup reference are editable on `TabletView`.

Editable assets: `TabletView.prefab` contains the question, choice buttons, tree entry and stock dots; `EpilogueView.prefab` retains its editable narrative/backgrounds and uses `CLICK / SPACE TO CONTINUE`. The explicit `Cheese Town/UI/Install Ending Choice and Tree Navigation` command builds these assets; it does not run during ordinary imports.

Validation: `EndingChecks.RunBatch` exercises a real town harvest crossing the threshold, delayed final mail, read acknowledgement, stock dots, direct tree/exit navigation, narrative freeze and scene reload, interactive choices, continued collection/production, and no replay. `UpgradeCurveChecks.DataChecks` covers the corresponding economy transition. Screenshots are saved in `Logs/EndingStory`.

A pre-change copy is stored beside the project in `ending-mail-flow-backup-20261006`. No Git commit or push is performed automatically.
