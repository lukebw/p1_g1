using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        RectTransform tutorialRoot;
        readonly RectTransform[] tutorialShade = new RectTransform[4];
        readonly RectTransform[] tutorialBorder = new RectTransform[4];
        TutorialShadeMesh tutorialShadeMesh;
        Text tutorialPrompt;
        Tree tutorialTree;
        Tree[] tutorialTrees;
        const string ChopInstruction = "WASD: MOVE TO A CHEESE TREE\nAIM AT THE TREE + LEFT CLICK TO CHOP";
        const string PickupInstruction = "Move closer to pick up the cheese.";
        PlayerController tutorialPlayer;
        public bool HarvestTutorialActive => worldScene && Progress != null &&
            Progress.TotalCollected == 0 && !EndingOpen && tutorialTree != null;
        public Tree TutorialTree => tutorialTree;

        void BuildTutorial()
        {
            if (!worldScene) return;
            tutorialPlayer = FindAnyObjectByType<PlayerController>();
            FindTutorialTree();
            tutorialRoot = new GameObject("First cheese tutorial", typeof(RectTransform)).GetComponent<RectTransform>();
            tutorialRoot.SetParent(ScreenCanvas.transform, false);
            tutorialRoot.anchorMin = Vector2.zero;
            tutorialRoot.anchorMax = Vector2.one;
            tutorialRoot.offsetMin = tutorialRoot.offsetMax = Vector2.zero;
            // Draw below the launcher; no tutorial graphics intercept gameplay input.
            tutorialRoot.SetAsFirstSibling();
            for (int i = 0; i < 4; i++)
                tutorialShade[i] = Box(tutorialRoot, "Tutorial shade " + i, 0, 0, 1, 1, new Color(.22f, .22f, .22f, .58f));
            for (int i = 0; i < 4; i++)
                tutorialBorder[i] = Box(tutorialRoot, "Tree highlight " + i, 0, 0, 1, 1, new Color(1, .83f, .22f));
            tutorialPrompt = Label(tutorialRoot, "Tutorial instruction", ChopInstruction, 0, 0, 1, 1,
                8, Color.white, false, TextAnchor.MiddleCenter);
            // BEGIN CHANGED: Use the same baked pixel font and integer sizing as the upgrade UI.
            tutorialPrompt.font = settings.upgradePixelFont != null ? settings.upgradePixelFont : font;
            tutorialPrompt.lineSpacing = 1.5f;
            UpgradeRowView.Sharpen(tutorialPrompt.font);
            if (HasPixelSkin && closedUnread != null)
            {
                closedUnread.font = tutorialPrompt.font;
                closedUnread.fontStyle = FontStyle.Normal; closedUnread.fontSize = 8;
            }
            BuildInteractionGuide();
            // END CHANGED
            var shadow = tutorialPrompt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);
            Refresh();
        }

        void FindTutorialTree()
        {
            if (tutorialPlayer == null) return;
            float nearest = float.PositiveInfinity;
            tutorialTrees = FindObjectsByType<Tree>();
            foreach (var candidate in tutorialTrees)
            {
                if (candidate.IsChopped) continue;
                float distance = (candidate.transform.position - tutorialPlayer.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                tutorialTree = candidate;
            }
        }

        static void TutorialRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(Mathf.Max(0, width), Mathf.Max(0, height));
        }

        void LateUpdate()
        {
            // BEGIN CHANGED: Prefab controls may opt out of automatic screen-edge placement.
            if (tabletView != null && tabletView.worldHud != null)
                tabletView.PositionHud((RectTransform)ScreenCanvas.transform);
            else if (worldScene && tabletLauncher != null && (tabletView == null || tabletView.anchorLauncherToScreen))
            // END CHANGED
            {
                // Anchor the entry to the actual screen, even when the pixel tablet is centered.
                var canvasRect = (RectTransform)ScreenCanvas.transform;
                var launcherRect = (RectTransform)tabletLauncher.transform;
                launcherRect.anchorMin = launcherRect.anchorMax = new Vector2(.5f, .5f);
                launcherRect.pivot = Vector2.one;
                // BEGIN CHANGED: Expose launcher margins without changing the tutorial flow.
                launcherRect.anchoredPosition = canvasRect.rect.size * .5f - (tabletView != null ? tabletView.launcherScreenInset : new Vector2(20, 20));
                // END CHANGED
                closedUnread.rectTransform.anchorMin = closedUnread.rectTransform.anchorMax = new Vector2(.5f, .5f);
                closedUnread.rectTransform.pivot = Vector2.one;
                // BEGIN CHANGED: Keep the unread gap editable with the launcher.
                closedUnread.rectTransform.anchoredPosition = launcherRect.anchoredPosition - new Vector2(0, launcherRect.rect.height + (tabletView != null ? tabletView.unreadGap : 5));
                // END CHANGED
            }
            if (tutorialRoot == null) return;
            UpdateInteractionGuide();
            if (Progress.TotalCollected == 0 && tutorialTree == null) FindTutorialTree();
            bool active = HarvestTutorialActive && !PhoneOpen && !TownSession.Instance.PrologueActive;
            tutorialRoot.gameObject.SetActive(active);
            if (!active) return;
            // If the player chops a different tree, guide them to its drops instead.
            if (!tutorialTree.IsChopped && tutorialTrees != null)
                foreach (var candidate in tutorialTrees)
                    if (candidate != null && candidate.IsChopped) { tutorialTree = candidate; break; }
            tutorialPrompt.text = tutorialTree.IsChopped ? PickupInstruction : ChopInstruction;
            var cam = Camera.main;
            var visual = tutorialTree.GetComponentInChildren<SpriteRenderer>();
            float scale = Mathf.Max(.01f, ScreenCanvas.scaleFactor);
            float width = Screen.width / scale, height = Screen.height / scale;
            float left = 0, right = width, bottom = 0, top = height;
            if (cam != null && visual != null)
            {
                var bounds = visual.bounds;
                // Keep the landing area visible after the tree becomes a stump.
                if (tutorialTree.IsChopped) bounds.Expand(new Vector3(5, 3, 0));
                Vector3 min = cam.WorldToScreenPoint(bounds.min) / scale;
                Vector3 max = cam.WorldToScreenPoint(bounds.max) / scale;
                float padding = 12 / scale;
                left = Mathf.Clamp(min.x - padding, 0, width);
                right = Mathf.Clamp(max.x + padding, left, width);
                bottom = Mathf.Clamp(min.y - padding, 0, height);
                top = Mathf.Clamp(max.y + padding, bottom, height);
            }
            // Cover the viewport's outermost pixels without overlapping the four shades.
            float bleed = 2 / scale;
            TutorialRect(tutorialShade[0], -bleed, -bleed, left + bleed, height + bleed * 2);
            TutorialRect(tutorialShade[1], right, -bleed, width - right + bleed, height + bleed * 2);
            TutorialRect(tutorialShade[2], left, -bleed, right - left, bottom + bleed);
            TutorialRect(tutorialShade[3], left, top, right - left, height - top + bleed);
            foreach (var shade in tutorialShade) shade.GetComponent<Image>().enabled = false;
            if (tutorialShadeMesh == null) tutorialShadeMesh = TutorialShadeMesh.Create(tutorialRoot);
            var origin = tutorialRoot.rect.min;
            tutorialShadeMesh.SetCoverage(UnityEngine.Rect.MinMaxRect(origin.x - bleed, origin.y - bleed,
                origin.x + width + bleed, origin.y + height + bleed),
                UnityEngine.Rect.MinMaxRect(origin.x + left, origin.y + bottom, origin.x + right, origin.y + top),
                new Color(.22f, .22f, .22f, .58f));
            float thickness = 3 / scale;
            TutorialRect(tutorialBorder[0], left, bottom, thickness, top - bottom);
            TutorialRect(tutorialBorder[1], right - thickness, bottom, thickness, top - bottom);
            TutorialRect(tutorialBorder[2], left, bottom, right - left, thickness);
            TutorialRect(tutorialBorder[3], left, top - thickness, right - left, thickness);
            float alpha = .75f + .25f * Mathf.Sin(Time.unscaledTime * 3);
            foreach (var edge in tutorialBorder) edge.GetComponent<Image>().color = new Color(1, .83f, .22f, alpha);
            tutorialPrompt.fontSize = HasPixelSkin ? 8 : 16;
            TutorialRect(tutorialPrompt.rectTransform, Mathf.Round(width * .05f), height - 62, Mathf.Round(width * .9f), 44);
        }
    }
}
