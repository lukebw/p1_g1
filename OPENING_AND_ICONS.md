# Opening narrative and upgrade icons

Generated with the built-in `image_gen` tool. Original PNG assets are in `Assets/Art/OpeningV1`; the complete per-asset prompt set is saved in `OPENING_ART_PROMPTS.json`. The original hand-drawn UI assets are preserved.

Seven transparent inventory icons represent Speed Boots, Cheese Magnet, Spring Tonic, Growth Potion, Haste Potion, Cozy Cellar, and Forest Charm. `TabletSettings.asset` binds them by stable upgrade ID. Unity imports icons at a maximum of 128 pixels with Point filtering, no mipmaps, and no compression. The PNG originals remain at their generated resolution.

The opening uses three atmospheric backgrounds: autumn, an empty winter storeroom, and the coming harvest. It appears once per new Play session on entering a scene containing the player. Three short English passages introduce Mousetown's fading guardian, the Great Cheese Tree, the approaching winter, and Leo's journey to gather cheese and revive the tree. There are no numbered headings, page counters, or promises that 100 cheese is sufficient.

Click or press Space/Enter to continue. Escape or the visible Skip button skips the opening. Transitions fade through dark. Advancement waits until the fade and a short reading guard finish. World controls, tablet shortcuts, pickup collisions, and production are blocked during the opening. The entire gameplay Canvas, including the wallet and mail HUD, is hidden until the opening ends. The final input is held for one extra frame to prevent it from chopping a tree behind the overlay. The existing harvesting tutorial resumes afterward. Scene reloads do not replay the opening; a new Play session does.

Edit `Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/PrologueView.prefab` in Prefab Mode to change the chapter text, background Sprite references, font/layout, fade duration, and minimum reading time. It uses the existing baked pixel font. Clear the Prologue Prefab field in Tablet Settings to disable the opening. The creation command preserves an existing edited prefab.

This is an opening presentation, not a start/quit menu or an ending-environment implementation. The current upgrade curve and wallet-based story thresholds are unchanged.

The build entry now points to the existing Wilderness scene; the previous SampleScene entry referenced a deleted scene.

Editor Play now uses normal domain/scene initialization. Testing found that the previous disabled-domain-reload setting left session/UI properties null. Re-enabling that optimization requires fixing the older session lifecycle first. Both prior project settings are included in the rollback snapshot.

Validation: `OpeningChecks.RunBatch` creates/imports the assets, checks chapter ordering, text fit, advance/skip callbacks, and runs a fresh Wilderness session to verify input blocking, tutorial handoff, and no replay after scene reload. Run this in a test copy; it opens Wilderness. Economy checks skip the opening before exercising the shop.

The project-adjacent `supplies-art-backup-20261006` directory restores the latest hand-drawn button replacement. Restore it before `supplies-tree-backup-20261006` (town tree and caption), then `story-polish-backup-20261006`, then `opening-backup-20261006` if needed. Each `Restore.ps1` checks all changed-file hashes before restoring; subsequent edits cause a refusal instead of being overwritten. Stop Play before restoring. Earlier upgrade backups remain available.

## Story and equipment polish

The inventory icon is a centered child of its existing box, with a centered pivot and preserved aspect ratio. This fixes the previous top-left pivot's bias when fitting square art inside a wide rectangle. The header uses the supplied SUPPLIES lettering directly from `Assets/Art/UI/UI_upgrade.png`. The temporary text and backing overlay have been removed. The existing button hover/press tint applies to the complete artwork. The 198x66 PNG, existing Sprite GUID, Point filtering and button geometry are preserved.

Spring Tonic now revives the Great Tree; subsequent potions improve its harvest. Item descriptions, the dormant-tree status, acquisition feedback, short tutorial prompts and early letters share this premise. The 100-cheese letter celebrates initial supplies rather than declaring the game objective complete. The later surplus/spoiling/departure story still plays out. Prices, production rates, purchase IDs, wallet thresholds and the 10,000-cheese ending are unchanged.

`OpeningAssets.ApplyStoryPolish` is an explicit prefab migration command, not an automatic Play-mode rebuild. Subsequent manual layout/text changes remain editable in Prefab Mode. `OpeningChecks.RunVisualBatch` checks the opening and saves native Unity renders in `Logs/StoryPolish` inside the test project.

The town page uses the supplied `Assets/Art/UI/UI_big_tree.png`. The 1280x720 source PNG is preserved byte for byte. Its single Sprite slice selects the painted tree (372x350 pixels) without its transparent margins, then displays at 186x175 logical pixels for the established 2x export workflow. Import uses Point filtering, no mipmaps and no compression. The tree remains an editable Image in `TabletView.prefab`; `TabletSettings.cheeseTree` uses the same Sprite for fallback layouts. Wild tree prefabs and their gameplay effects are unchanged.
