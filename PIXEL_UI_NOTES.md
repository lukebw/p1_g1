# Supplied pixel UI integration

- Copy the six supplied runtime PNGs unchanged into `Assets/Art/UI`; keep the full layout reference under `References`.
- Interpret the 1280 x 720 frame as a 640 x 360 layout. Runtime sprite dimensions are their exported PNG dimensions divided by two.
- Import full-rectangle sprites with Point filtering, no mipmaps, and no compression so transparent padding and artwork pixels survive.
- Import the selected `PressStart2P.ttf` under `Assets/Art/UI/Fonts` with hinted raster rendering. Keep the earlier EAS VHS file available, but bind Press Start 2P to the shared settings.
- Bind the frame, Back, Upgrade, Mail, unread dot, and Collect artwork through the shared TabletSettings asset. Use the existing cheese and tree sprites for the wallet and central tree.
- Keep the reference image out of runtime layout construction; it documents positions only.
- At 1080p, enlarge the native UI by three. At other sizes at least 640 x 360, use the largest fitting integer factor and center the layout. Smaller editor previews fit fractionally so controls remain accessible.
- Keep the existing purchase, filter, mailbox, collection, unread, and ending callbacks. Back returns to the main page from subpages and closes the tablet from the main page.
- The new main-page skin is separated into a partial class. Missing frame artwork still uses the original placeholder layout.
- Shop and mailbox retain dynamic text and existing data; their native layouts fit inside the supplied frame.
- Import the fifteen supplied upgrade-page sprites unchanged, with the same Point/full-rectangle settings. Keep `UI_Upgrade_ref.png` under `References` for layout only. Preserve source names, including `UI_Treel_unselect.png`.
- On the shop page, switch to the full-height upgrade frame and green backdrop, and hide the home footer. Keep Back, Upgrade, Mail, and the live wallet above the backdrop.
- Use the revised 1128 x 110 row artwork as a 564 x 55 prefab. Nine-slice its plain center to preserve the pixel border when height changes. Upgrade-specific icons remain editable data slots because only their background box was supplied.
- Bake a separate bitmap import of the same Press Start 2P source at eight pixels for row text and tooltips. Include printable ASCII and the right triangle (U+25B6) so the details cue shares the sharp glyph grid. Point sampling and 8/16-pixel sizes preserve that grid independently of Canvas resolution. The original dynamic font remains assigned to the rest of the tablet.
- The initial prefab uses eight-pixel titles, description previews, levels and effects, with sixteen-pixel prices in a four-digit field. Preserve later artist font-size edits (the current price size is fourteen). Nine-slice the Buy button to 176 x 37, widening only the blank price area. Values above 9999 show `10K+`; economy data is never truncated.
- A masked 221-pixel viewport displays three complete rows and part of the next. Stack each prefab's actual height and editable row spacing. Longer lists scroll with the wheel, including over Buy buttons. Clamp scrolling, disable inertia, and round positions to native pixels; short filtered lists hide the scrollbar.
- Selected/unselected category sprites track the current filter. Show only the numeric price beside the baked BUY text. Use the supplied disabled sprite for insufficient funds, full upgrades, and unavailable entries.
- Display the base state as level one: two paid upgrades read `LV 1/3`, `LV 2/3`, and `LV 3/3`. Preserve internal purchase counts and prices. Use one marker per displayed level, adding marker rows and increasing row height for longer upgrade chains.
- Keep the full effect description in the data and show a fitted two-line preview ending in `... ▶` when truncated. Hovering only the description text area for 0.35 seconds opens the full effect paragraph, without repeating the name, level, price, or value comparison. Leaving that area, changing pages, scrolling, or dragging the list dismisses it.
- Keep the tooltip inside the tablet and to the left of the purchase column. Its CanvasGroup and every graphic remain click-through, so it cannot intercept purchases. For exceptionally long descriptions, wheel input over the description text scrolls the tooltip; ordinary descriptions forward wheel input to the upgrade list.
- Attach `UpgradeDescriptionHover` to the description Text when binding each row. This moves hover handling off the row without rewriting the existing prefab, preserving artist changes to its colors, positions, and spacing.
- Fix editor menu paths to the project's nested CheeseTownPhone directory and expose the new skin in the artwork inspector.

## Verification

Run **Cheese Town > Run Pixel UI Checks** outside Play mode. The checks open a temporary empty scene; reopen the working scene afterwards. The batch entry point is `CheeseTownPhone.Editor.PixelUIChecks.RunBatch`.

Results: `Logs/pixel-ui-checks.txt`. Unity-rendered 1080p previews: `Logs/PixelUI-Main-1080p.png`, `Logs/PixelUI-Shop-1080p.png`, `Logs/PixelUI-Shop-Scrolled-1080p.png`, `Logs/PixelUI-Shop-Purchased-1080p.png`, `Logs/PixelUI-Shop-Tooltip-1080p.png`, `Logs/PixelUI-Shop-FourDigits-1080p.png`, `Logs/PixelUI-Mail-1080p.png`, and `Logs/PixelUI-Ending-1080p.png`.

Passed in an isolated Unity 6000.6.2f1 project copy: description-only hover, effect-only tooltip content, the `... ▶` glyph and preview cue, click-through graphics and Buy raycasts beneath the popup, immediate pointer-exit/list-scroll dismissal, and long descriptions scrolled through their text target. This change does not rewrite the artist's prefab layout, font sizes, or preview placeholder. Retained checks cover saved prefab binding, bitmap Point sampling, one-based level progression, four-digit prices, new upgrade configuration, actual PlayerController/TreeHurtbox reactions, source imports, integer scaling, wheel bounds, filters, disabled buttons, purchase/debit, collection, wallet, mail, empty lists, and the final ending. Existing economy and mail data checks also pass.

Changed and added C# blocks carry short English BEGIN/END comments explaining why they exist. Native asset and importer changes are documented here instead of inserting comments into Unity YAML.

## Editing and extending upgrade rows

- Open `Cheese Town > Select Upgrade Row Prefab`, then open `UpgradeRow.prefab` in Prefab Mode. Child RectTransforms, Text font sizes/colors, row height, spacing, and hover delay are editable. `UpgradeRowView` stores the component references; retain these references when adjusting the layout.
- Open `Cheese Town > Select Tablet Configuration` to add/reorder entries in `Upgrades`. Set title, description, icon, effect, and each paid level's price/value. The list creates another instance of the same prefab automatically; no prefab duplication or scene-object wiring is required for each new item.
- `UpgradeRowPrefabBuilder` creates the initial prefab only if it does not already exist. Applying artwork again preserves artist edits. The shared `Upgrade Row Prefab` field can also reference a prefab variant.
- `UpgradeRowView` displays data and forwards the purchase callback to `CheeseTownDemo.Buy`. `TownProgress` still debits the wallet and broadcasts changes to `PlayerController` and `TreeHurtbox`. The prefab never owns player/tree references or copies their upgrade state.
- Adding an existing effect is a data change. A new gameplay effect still requires extending `UpgradeEffect`, the shared calculation, and its gameplay consumer; the row prefab is reusable. Options with the same effect currently use the strongest purchased value, as defined by the existing economy.
