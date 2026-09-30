using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    [DisallowMultipleComponent]
    public sealed class CheeseTownDemo : MonoBehaviour
    {
        [SerializeField] TabletSettings settings;
        public TabletSettings Settings => settings;
        public TownProgress Progress { get; private set; }
        public bool PhoneOpen => tablet != null && tablet.activeSelf;
        public Canvas ScreenCanvas { get; private set; }
        readonly Color ink = new Color(.08f,.12f,.16f), paper = new Color(.92f,.94f,.92f);
        readonly Color muted = new Color(.59f,.69f,.71f), accent = new Color(.86f,.73f,.36f);
        Font font;
        InputActionMap controls;
        RectTransform stage, tree;
        GameObject tablet, shop, mail;
        Text wallet, stock, stats, priceLabel, notice, letter, reply, shopWallet;
        Button collectButton, replyButton, previousLetter, nextLetter;
        Text unreadBadge, closedUnread;
        int selectedLetter = -1;
        readonly List<Row> rows = new List<Row>();
        int revision, page, filter;
        float noticeUntil, nextRefresh;
        bool built; bool worldScene;
        sealed class Row { public UpgradeOption option; public Text level, current, next, cost; public Button buy; }

        void Awake() { Build(); }
        public void Build()
        {
            if (built) return;
            built = true;
            settings = TownSession.Instance.Settings;
            Progress = TownSession.Instance.Progress;
            worldScene = FindAnyObjectByType<PlayerController>() != null;
            if (FindAnyObjectByType<Camera>() == null) {
            var cameraObject = new GameObject("Tablet camera", typeof(Camera));
            cameraObject.transform.SetParent(transform);
            var cam = cameraObject.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = ink;
            cam.orthographic = true; cam.transform.position = new Vector3(0, 0, -10);
            }
            var canvas = new GameObject("Landscape tablet UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform);
            ScreenCanvas = canvas.GetComponent<Canvas>();
            ScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            stage = new GameObject("Tablet layout", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(canvas.transform, false);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f, .5f);
            stage.sizeDelta = new Vector2(1600, 900);
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Tablet input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Render(false);
            controls = new InputActionMap("Cheese Town Tablet");
            controls.AddAction("Tablet", InputActionType.Button, "<Keyboard>/tab").performed += _ => TogglePhone();
            controls.AddAction("Back", InputActionType.Button, "<Keyboard>/escape").performed += _ =>
            { if (page != 0) ShowPage(0); else if (PhoneOpen) TogglePhone(); };
            controls.AddAction("Collect", InputActionType.Button, "<Keyboard>/e").performed += _ => TryHarvest();
            controls.Enable();
        }
        static void Release(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1);
            r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); return r;
        }
        RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var r = Rect(parent,name,x,y,w,h);
            var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
        }
        Text Label(Transform p, string name, string value, float x, float y, float w, float h, int size, Color color, bool bold = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            var t = Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; t.alignment = align;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate; return t;
        }
        Image Artwork(Transform p, string name, Sprite sprite, string fallback, float x, float y, float w, float h, Color color, bool preserve = true)
        {
            var r = Box(p,name,x,y,w,h,sprite == null ? color : Color.white);
            var image = r.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = preserve;
            if (sprite == null && !string.IsNullOrEmpty(fallback))
                Label(r,"Placeholder label",fallback,6,4,w-12,h-8,14,paper,true,TextAnchor.MiddleCenter);
            return image;
        }
        Button Action(Transform p, string name, string text, float x, float y, float w, float h, UnityAction click, bool gold = false)
        {
            var r = Box(p,name,x,y,w,h,gold ? accent : new Color(.18f,.26f,.30f));
            var image = r.GetComponent<Image>(); image.raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = new Color(.85f,.93f,.95f);
            colors.pressedColor = new Color(.65f,.78f,.82f); colors.disabledColor = new Color(.5f,.5f,.5f,.65f); b.colors = colors;
            b.onClick.AddListener(click);
            Label(r,"Label",text,8,0,w-16,h,17,gold ? ink : paper,true,TextAnchor.MiddleCenter);
            return b;
        }
        void AppButton(Transform p, string name, string text, Sprite icon, float x, UnityAction click)
        {
            var b = Action(p,name,"",x,20,132,52,click);
            Artwork(b.transform,name+" icon slot",icon,text,4,4,124,44,new Color(0,0,0,0));
        }
        void Render(bool open)
        {
            foreach (Transform child in stage) { child.gameObject.SetActive(false); Release(child.gameObject); }
            rows.Clear(); revision = settings.Revision;
            font = settings.interfaceFont != null ? settings.interfaceFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!worldScene) {
            Box(stage,"Outside tablet",0,0,1600,900,ink);
            Label(stage,"Closed title","TOWN TABLET",450,318,700,60,42,paper,true,TextAnchor.MiddleCenter);
            Label(stage,"Closed hint","Your town, messages and upgrades in one place.",430,394,740,45,21,muted,false,TextAnchor.MiddleCenter);
            Action(stage,"Open tablet","OPEN TABLET   [TAB]",625,480,350,62,TogglePhone,true);
            } else Action(stage,"Open tablet","TABLET [TAB]",1220,20,300,52,TogglePhone,true);
            closedUnread = Label(stage,"Closed unread","",1120,78,400,35,19,accent,true,TextAnchor.MiddleRight);
            tablet = Box(stage,"Landscape tablet",70,55,1460,790,new Color(.055f,.075f,.09f)).gameObject;
            tablet.GetComponent<Image>().raycastTarget = true;
            var frame = tablet.transform;
            Artwork(frame,"Town Background Sprite Slot",settings.townBackground,"",16,92,1428,682,settings.backgroundColor,false);
            Label(frame,"Tablet heading","TOWN",36,22,180,28,25,paper,true);
            Label(frame,"Tablet caption","CHEESE TREE",36,54,225,20,12,muted);
            wallet = Label(frame,"Wallet","",540,25,340,36,25,accent,true,TextAnchor.MiddleRight);
            AppButton(frame,"Letters button","ENVELOPE",settings.envelopeIcon,942,() => ShowPage(2));
            unreadBadge = Label(frame,"Unread mail","",942,74,280,24,14,accent,true);
            AppButton(frame,"Shop button","UPGRADES",settings.shopIcon,1086,() => ShowPage(1));
            AppButton(frame,"Close tablet","CLOSE",settings.closeIcon,1230,TogglePhone);
            if (settings.townBackground == null)
                Label(frame,"Background placeholder","TOWN BACKGROUND",39,113,430,26,15,muted,true);
            var treeImage = Artwork(frame,"Cheese Tree Sprite Slot",settings.cheeseTree,"CHEESE\nTREE",0,0,settings.treeSize.x,settings.treeSize.y,settings.treeColor);
            tree = treeImage.rectTransform; tree.anchorMin = tree.anchorMax = new Vector2(.5f,.5f);
            tree.pivot = new Vector2(.5f,.5f); tree.anchoredPosition = new Vector2(0,12);
            Label(frame,"Tree caption","CHEESE TREE",555,568,350,33,22,paper,true,TextAnchor.MiddleCenter);
            stock = Label(frame,"Tree storage","",490,606,480,29,18,paper,false,TextAnchor.MiddleCenter);
            Box(frame,"Readouts",35,666,1390,87,new Color(.08f,.14f,.17f,.94f));
            stats = Label(frame,"Player attributes","",54,680,445,62,15,muted);
            priceLabel = Label(frame,"Production attributes","",505,680,505,62,15,muted);
            collectButton = Action(frame,"Collect cheese","COLLECT   [E]",1100,683,303,50,TryHarvest,true);
            notice = Label(frame,"Notification","",335,126,780,46,20,accent,true,TextAnchor.MiddleCenter);
            BuildShop(frame); BuildMail(frame);
            tablet.SetActive(open); ShowPage(page); Refresh();
        }
        void BuildShop(Transform frame)
        {
            shop = Box(frame,"Upgrade shop panel",28,98,1404,663,new Color(.08f,.13f,.17f)).gameObject;
            shop.GetComponent<Image>().raycastTarget = true;
            Label(shop.transform,"Shop title","UPGRADE SHOP",22,15,350,40,28,paper,true);
            shopWallet = Label(shop.transform,"Shop wallet","",875,20,310,30,19,accent,true,TextAnchor.MiddleRight);
            Action(shop.transform,"Shop back","BACK",1220,14,162,43,() => ShowPage(0));
            Action(shop.transform,"All filter","ALL",22,66,110,36,() => ChangeFilter(0));
            Action(shop.transform,"Player filter","PLAYER",144,66,125,36,() => ChangeFilter(1));
            Action(shop.transform,"Tree filter","TREE",281,66,110,36,() => ChangeFilter(2));
            Label(shop.transform,"Rules","Production = quantity   /   Yield = cheeses per harvest",461,73,909,25,15,muted,false,TextAnchor.MiddleRight);
            Label(shop.transform,"Option header","UPGRADE",112,118,320,22,12,muted,true);
            Label(shop.transform,"Target header","APPLIES TO",524,118,130,22,12,muted,true);
            Label(shop.transform,"Effect header","CURRENT  >  NEXT",685,118,300,22,12,muted,true);
            Label(shop.transform,"Price header","NEXT LEVEL PRICE",1160,118,218,22,12,muted,true);
            var viewport = Box(shop.transform,"Shop scroll viewport",18,149,1368,452,new Color(.10f,.17f,.20f));
            viewport.GetComponent<Image>().raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport,"Upgrade rows",0,0,1348,0);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            var track = Box(shop.transform,"Scroll track",1374,149,10,452,new Color(.15f,.22f,.25f));
            var handle = Box(track,"Scroll handle",0,0,10,100,muted);
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>();
            handle.GetComponent<Image>().raycastTarget = true; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            int index = 0;
            foreach (var option in settings.upgrades)
            {
                if (option == null || !option.available || filter == 1 && option.Target != UpgradeTarget.Player || filter == 2 && option.Target != UpgradeTarget.Tree) continue;
                var item = Box(content,"Upgrade "+option.id,0,index*90,1348,84,new Color(.14f,.21f,.25f));
                Artwork(item,"Upgrade Icon Sprite Slot",option.icon,"ICON",10,10,64,64,new Color(.22f,.31f,.35f));
                var title = Label(item,"Title",option.title,91,9,399,27,19,paper,true);
                title.resizeTextForBestFit = true; title.resizeTextMinSize = 12; title.resizeTextMaxSize = 19;
                var description = Label(item,"Description",option.description,91,39,399,38,13,muted);
                description.resizeTextForBestFit = true; description.resizeTextMinSize = 10; description.resizeTextMaxSize = 13;
                Label(item,"Target",option.Target == UpgradeTarget.Player ? "PLAYER" : "TREE",506,13,130,25,14,accent,true);
                var row = new Row { option = option };
                row.level = Label(item,"Level","",506,44,150,23,14,muted);
                row.current = Label(item,"Current value","",666,12,439,29,16,paper);
                row.next = Label(item,"Next value","",666,46,439,27,14,muted);
                row.buy = Action(item,"Buy "+option.id,"",1132,17,198,49,() => Buy(option),true);
                if (settings.upgradeButtonArtwork != null) { var img = row.buy.GetComponent<Image>(); img.sprite = settings.upgradeButtonArtwork; img.color = Color.white; img.type = Image.Type.Sliced; }
                row.cost = row.buy.GetComponentInChildren<Text>(); row.cost.fontSize = 14;
                rows.Add(row); index++;
            }
            content.sizeDelta = new Vector2(1348,Mathf.Max(452,index*90));
            if (index == 0) Label(content,"Empty shop","No upgrades in this category.",280,160,740,50,23,muted,false,TextAnchor.MiddleCenter);
            Label(shop.transform,"Shop footer","Player: movement, range, auto collect     |     Tree: size, production, cheese value",22,620,1360,25,14,muted,false,TextAnchor.MiddleCenter);
        }
        void BuildMail(Transform frame)
        {
            mail = Box(frame,"Mayor mailbox",250,122,960,608,new Color(.09f,.15f,.18f)).gameObject;
            mail.GetComponent<Image>().raycastTarget = true;
            Label(mail.transform,"Mail title","MAYOR'S LETTERS",32,25,660,45,29,paper,true);
            Action(mail.transform,"Mail back","BACK",775,23,150,43,() => ShowPage(0));
            Label(mail.transform,"Sender","MAYOR ELLIS  /  TOWN HALL",34,81,710,29,14,accent,true);
            letter = Label(mail.transform,"Mayor message","",34,134,887,258,23,paper);
            reply = Label(mail.transform,"Reply text","",34,405,887,67,18,muted);
            previousLetter = Action(mail.transform,"Previous letter","PREVIOUS",34,499,210,66,() => SelectLetter(selectedLetter - 1));
            replyButton = Action(mail.transform,"Reply to mayor","MARK AS READ",264,499,397,66,ReplyToMayor,true);
            nextLetter = Action(mail.transform,"Next letter","NEXT",681,499,240,66,() => SelectLetter(selectedLetter + 1));
        }
        void ChangeFilter(int value) { filter = value; Render(PhoneOpen); }
        public void TogglePhone() { tablet.SetActive(!tablet.activeSelf); if (PhoneOpen) ShowPage(0); }
        public void ShowPage(int value)
        {
            page = value;
            if (shop != null) shop.SetActive(page == 1);
            if (mail != null) mail.SetActive(page == 2);
            Refresh();
        }
        public void ApplyConfiguration()
        {
            if (!built) return;
            settings.ValidateSettings(); Progress.Reconfigure(settings); Render(PhoneOpen);
        }
        public void UseSettings(TabletSettings value)
        {
            if (value == null || value == settings) return;
            settings = value; TownSession.Instance.Configure(value);
            ApplyConfiguration();
        }
        void Update()
        {
            if (!built) return;
            if (revision != settings.Revision) { Progress.Reconfigure(settings); Render(PhoneOpen); }

            if (Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + .1f; }
            if (noticeUntil > 0 && Time.unscaledTime > noticeUntil) { notice.text = ""; noticeUntil = 0; }
        }
        public void TryHarvest()
        {
            if (!PhoneOpen || page != 0) return;
            int amount = Progress.Collect();
            Notify(amount == 0 ? "The tree is growing more cheese." : amount+" cheese  =  +"+((long)amount*Progress.UnitPrice)+" cheeses");
            Refresh();
        }
        public void Buy(UpgradeOption option)
        {
            if (Progress.Buy(option)) Notify(option.title+" upgraded.");
            Refresh();
        }
        public void ReplyToMayor()
        {
            Progress.ReadLetter(selectedLetter);
            Refresh();
        }
        void SelectLetter(int index)
        {
            selectedLetter = Mathf.Clamp(index, 0, Progress.Letters.Count - 1);
            Refresh();
        }
        void Notify(string value) { notice.text = value; noticeUntil = Time.unscaledTime+4; }
        public void Refresh()
        {
            if (wallet == null || letter == null) return;
            wallet.text = Progress.Cheeses+"  CHEESES"; shopWallet.text = "WALLET   "+Progress.Cheeses+" CHEESES";
            stock.text = "Stored: "+Mathf.FloorToInt(Progress.Stock)+" / "+Progress.Capacity+" cheese";
            stats.text = "PLAYER PARAMETERS\nSpeed "+Progress.MoveSpeed.ToString("0.##")+"   |   Range "+Progress.CollectRange.ToString("0.##")+
                "   |   Auto "+(Progress.AutoEnabled ? Progress.AutoRate.ToString("0.##")+"/s" : "OFF");
            priceLabel.text = "TREE PARAMETERS\nProduction "+Progress.Production.ToString("0.##")+"/s   |   Value "+Progress.UnitPrice+" cheeses / harvest";
            float treeFit = Mathf.Min(1, 340 / (settings.treeSize.y * Progress.TreeScale), 600 / (settings.treeSize.x * Progress.TreeScale));
            tree.localScale = Vector3.one * Progress.TreeScale * treeFit;
            collectButton.interactable = Progress.Stock >= 1;
            foreach (var row in rows)
            {
                int level = Progress.Level(row.option), max = row.option.levels.Count;
                row.level.text = "LEVEL "+level+" / "+max;
                row.current.text = Current(row.option.effect);
                row.next.text = level < max ? "Next: "+Effect(row.option.effect,row.option.levels[level]) : max == 0 ? "No levels configured" : "Fully upgraded";
                row.buy.interactable = Progress.CanBuy(row.option);
                row.cost.text = max == 0 ? "UNAVAILABLE" : level >= max ? "MAX LEVEL" :
                    Progress.Cheeses < row.option.levels[level].price ? "NEED "+(row.option.levels[level].price-Progress.Cheeses)+" CHEESES" :
                    row.option.levels[level].price+" CHEESES  /  BUY";
            }
            int count = Progress.Letters.Count;
            if (selectedLetter < 0 && count > 0) selectedLetter = 0;
            string unread = Progress.UnreadCount > 0 ? Progress.UnreadCount + " UNREAD LETTERS" : "";
            unreadBadge.text = unread;
            closedUnread.text = unread;
            closedUnread.gameObject.SetActive(!PhoneOpen);
            letter.text = count == 0 ? "No letters yet.\n\nCollect cheese to hear from Mayor Ellis." : Progress.Letters[selectedLetter].Body;
            reply.text = count == 0 ? "Collected: " + Progress.TotalCollected :
                "LETTER " + (selectedLetter + 1) + " / " + count +
                (Progress.Letters[selectedLetter].IsRead ? "  |  READ" : "  |  UNREAD");
            if (Progress.MailStopped) reply.text += "\nOrdinary mail has ended. Your letter history remains available.";
            previousLetter.interactable = selectedLetter > 0;
            nextLetter.interactable = selectedLetter >= 0 && selectedLetter < count - 1;
            replyButton.interactable = count > 0 && !Progress.Letters[selectedLetter].IsRead;
        }
        string Current(UpgradeEffect effect)
        {
            switch(effect)
            {
                case UpgradeEffect.MoveSpeed: return "Speed: "+Progress.MoveSpeed.ToString("0.##");
                case UpgradeEffect.CollectRange: return "Radius: "+Progress.CollectRange.ToString("0.##");
                case UpgradeEffect.TreeGrowth: return Progress.Production.ToString("0.##")+" cheese/s  |  Size "+Progress.TreeScale.ToString("0.##")+"x";
                case UpgradeEffect.AutoCollect: return Progress.AutoEnabled ? "Auto: "+Progress.AutoRate.ToString("0.##")+" cheese/s" : "Auto collection: OFF";
                default: return "Value: "+Progress.UnitPrice+" cheeses / harvest";
            }
        }
        string Effect(UpgradeEffect effect, UpgradeLevel level)
        {
            switch(effect)
            {
                case UpgradeEffect.MoveSpeed: return "speed "+level.value.ToString("0.##");
                case UpgradeEffect.CollectRange: return "radius "+level.value.ToString("0.##");
                case UpgradeEffect.TreeGrowth: return level.value.ToString("0.##")+" cheese/s, size "+level.treeScale.ToString("0.##")+"x";
                case UpgradeEffect.AutoCollect: return "match tree production / second";
                default: return level.value.ToString("0.##")+"x cheese yield (quantity unchanged)";
            }
        }
        void OnEnable() { controls?.Enable(); }
        void OnDisable() { controls?.Disable(); }
        void OnDestroy() { controls?.Dispose();  }
    }
}
