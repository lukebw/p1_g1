# Supplied pixel UI integration

- Copy the six supplied runtime PNGs unchanged into `Assets/Art/UI`; keep the full layout reference under `References`.
- Interpret the 1280 x 720 frame as a 640 x 360 layout. Runtime sprite dimensions are their exported PNG dimensions divided by two.
- Import full-rectangle sprites with Point filtering, no mipmaps, and no compression so transparent padding and artwork pixels survive.
- Import the selected `eas-vhs.ttf` under `Assets/Art/UI/Fonts` with hinted raster rendering.
- Bind the frame, Back, Upgrade, Mail, unread dot, and Collect artwork through the shared TabletSettings asset. Use the existing cheese and tree sprites for the wallet and central tree.
- Keep the reference image out of runtime layout construction; it documents positions only.
- At 1080p, enlarge the native UI by three. At other sizes at least 640 x 360, use the largest fitting integer factor and center the layout. Smaller editor previews fit fractionally so controls remain accessible.
- Keep the existing purchase, filter, mailbox, collection, unread, and ending callbacks. Back returns to the main page from subpages and closes the tablet from the main page.
- The new main-page skin is separated into a partial class. Missing frame artwork still uses the original placeholder layout.
- Shop and mailbox retain dynamic text and existing data; their native layouts fit inside the supplied frame. Their remaining art can be replaced through the existing slots later.
- Fix editor menu paths to the project's nested CheeseTownPhone directory and expose the new skin in the artwork inspector.

## Verification

Run **Cheese Town > Run Pixel UI Checks** outside Play mode. The checks open a temporary empty scene; reopen the working scene afterwards. The batch entry point is `CheeseTownPhone.Editor.PixelUIChecks.RunBatch`.

Results: `Logs/pixel-ui-checks.txt`. Unity-rendered 1080p previews: `Logs/PixelUI-Main-1080p.png`, `Logs/PixelUI-Shop-1080p.png`, `Logs/PixelUI-Mail-1080p.png`, and `Logs/PixelUI-Ending-1080p.png`.

Passed in Unity 6000.6.2f1: source import dimensions/filtering, EAS VHS glyphs, 1080p/720p/1440p integer factors, real pointer raycasts, Tab/Escape/Back, collection payout, category filters, purchase/debit/max level, visible titles, live wallet, unread/read dot, empty shop, and final-letter ending. Source PNG hashes match the supplied originals. Existing economy and mail data checks also pass.

Changed and added C# blocks carry short English BEGIN/END comments explaining why they exist. Native asset and importer changes are documented here instead of inserting comments into Unity YAML.
