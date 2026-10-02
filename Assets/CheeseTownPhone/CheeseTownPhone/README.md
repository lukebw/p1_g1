# Cheese Town: shared upgrades

New play sessions begin with a three-page welcome from Mayor Ellis. Click NEXT, then LET'S BEGIN! to enter the game. The opening introduces cheese collection, the first letter at 10 cheeses, and TAB for the tablet. Movement, collection and tablet shortcuts are blocked during the conversation. Completing it is remembered across scene changes for the current session; restarting the game shows it again.

Reading the all-upgrades-complete letter (all-max) with FINISH READING now ends the game. The tablet switches to its existing town background with GAME OVER, hiding the mailbox, tree readouts and shop controls. TAB and Escape cannot dismiss the ending. Movement, chopping, cheese collection and production stop. The ending survives scene reloads within the same session. The ending reuses the existing TAB town interface and does not require additional artwork.

Open either CheeseTownPhone or Wilderness and press Play. Tab opens the town tablet and shop in both scenes. In Wilderness, close the tablet to move the mouse; press E (or the configured Interact action) near the tree to collect. The tablet does not replace the world camera or cover gameplay while closed.

All money is cheeses: starting wallet, purchase costs, collection, automatic income and mayor rewards. Double Cheese Value multiplies cheeses awarded per harvest without changing tree production. No separate coin currency remains.

TownSession owns one TownProgress and one production clock for the play session. Purchases, wallet, tree stock and quest progress survive scene changes and scene reloads. Stopping Play or restarting the game resets this session; disk save/load is not implemented.

PlayerController subscribes to shared upgrades whenever enabled, including newly spawned players. Movement uses the shop's absolute speeds (default 4, then 6 and 8) with normalized diagonal movement. Collection uses distance from the mouse to the tree collider: default radius 0 requires contact, then upgrades allow 2 and 4 world units. Tree growth updates the actual world tree and tablet tree. Production and automatic collection continue with the tablet closed, but respect the game's time scale. World trees and the tablet represent the same shared cheese stock; additional scene trees do not create extra production clocks.

Choose **Cheese Town > Select Tablet Configuration** to edit Resources/TabletSettings.asset. Its existing asset GUID is preserved. Upgrade IDs preserve levels across reordering; disabling or removing an option removes its effect without a refund. Same-effect options use the strongest value. Runtime configuration changes update players and trees.

Both gameplay scenes are registered in Build Settings. The former missing SampleScene entry was replaced.

## Checks

**Cheese Town > Run Data Checks** tests the economy and upgrade rules. **Run Cross Scene Checks** starts an isolated Play session, loads the actual Wilderness player and tree, purchases through UI buttons, verifies speed/radius/payouts and reloads both gameplay scenes. It stops Play afterward and writes Logs/cheese-cross-scene-checks.txt. Start from a fresh, stopped Play session for these checks.

**Run Demo Checks (Play Mode)** also tests tablet keyboard navigation, filters and editable artwork. Those interactive checks spend session currency and alter session purchases; restart Play afterward.

Keep Assets/Code, Assets/CheeseTownPhone and their .meta files together. No packages or external dependencies were added for this change.
