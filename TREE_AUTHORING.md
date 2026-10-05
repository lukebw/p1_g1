# Tree prefabs

`Assets/Prefab/Tree.prefab` is the ordinary tree (2–4 cheese drops).
`Assets/Prefab/Tree_02.prefab` is an independent copy for the high-yield tree (8–10 cheese drops, inclusive).

Both use the same `Tree` script, cheese pickup prefab, health of 5, hit feedback, upgrade connection and stump conversion. On the prefab root, edit **Tree > Cheese drops > Min Cheese Count / Max Cheese Count** to tune each type independently. These are per-prefab settings, not global economy values. The new prefab is ready for placement; existing scene trees are not replaced.

Tree 02 uses `Cheese_tree_02.png` and `Cheese_tree_02_chopped.png`. Both source images retain their 160×256 canvas. Import settings and slice coordinates match the original standing tree/stump pair so their source-coordinate anchors stay aligned during stump conversion. The standing slice intentionally includes the empty space above the shorter 02 canopy. Keep both sprites at 100 pixels per unit, Point filtering and no mipmaps.

For future maps, distribute this tree less frequently than the ordinary tree if it should remain a valuable find. At the current drop settings, its average yield is 9 rather than 3 cheeses. Spawn frequency and health can be balanced separately without changing the shared tree script.

The map-prefab Play-mode checks subsequently verified the 02 tree's actual 8–10 cheese drops, corresponding stump artwork, stationary root and repeated-hit guard. See `Logs/map-prefab-checks.txt`.
