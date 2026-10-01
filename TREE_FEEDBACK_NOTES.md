# Tree feedback changes

- Tree.cs: shake only the sprite to preserve root sorting and collisions; emit green leaf bursts; replace the chopped tree with aligned stump art; prevent repeated drops.
- CheeseDropMotion.cs: activate inactive templates and cache references on first use; animate ground travel separately from canopy height; restore pickup physics after descent and one bounce.
- CursorController.cs: pair input subscriptions on enable/disable so one click causes one hit per tree.
- CollectionRadius.cs: defer pickup until landing and check existing overlaps.
- TreeHurtbox.cs: exclude stumps from damage and manual collection.
- Tree.prefab: bind the stump sprite and expose Inspector tuning.
- Cheese.prefab: move the original renderer into a visual child; add ground sorting and drop motion; use trigger pickup to keep landed cheese in place.
- Cheese_tree_chopped.png: copy the supplied artwork unchanged; import the visible 122 x 58 pixel rectangle with point filtering at 100 pixels per unit.

## Tuning

Tree prefab: Shake Duration/Angle, Min/Max Leaf Count, Min/Max Cheese Count (inclusive; defaults 2-4, use 3-3 for fixed drops), Canopy Height, Spawn Width, and Min/Max Spread Radius.

Cheese prefab: Fall Duration (slightly randomized per fruit), Bounce Duration, and Bounce Height. Physics and pickup are suspended until the bounce finishes.

## Verification

Save the current scene, then run **Cheese Town > Run Tree Feedback Checks** outside Play mode. The checks open an empty scene and enter Play mode; reopen Wilderness afterwards. Results are written to Logs/tree-feedback-checks.txt. The check scene and economy are temporary Play mode state.

Verified successfully in a temporary project copy with Unity 6000.6.0f1. The original project version remains 6000.6.2f1. Checks cover input deduplication, shake/reset, leaf emission, stump alignment/collision/occlusion settings, single death payout, fixed and inclusive random counts, descent/landing, and delayed trigger pickup. The actual Unity-rendered preview is Logs/tree-feedback-preview.png.

Changed and added code blocks have short English BEGIN/END comments stating why the change exists. Scene placement, renderer-wide sorting settings, and the original source artwork are preserved.

Prefab changes are documented here; keep native serialized asset files free of inline review annotations so Unity imports every property correctly.
