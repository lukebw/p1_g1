using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class TownTreeHarvestBuilder
    {
        public const string Path = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TownTreeHarvestFeedback.prefab";
        [MenuItem("Cheese Town/UI/Install Town Tree Harvest Feedback")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing feedback.");
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            var prefab = AssetDatabase.LoadAssetAtPath<TownTreeHarvestFeedback>(Path);
            if (prefab == null)
            {
                var root = new GameObject("TownTreeHarvestFeedback", typeof(RectTransform), typeof(CanvasGroup), typeof(TownTreeHarvestFeedback));
                try
                {
                    root.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 360);
                    var group = root.GetComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
                    var feedback = root.GetComponent<TownTreeHarvestFeedback>();
                    feedback.flightPrefab = AssetDatabase.LoadAssetAtPath<CheesePickupFlight>(PickupFeedbackBuilder.FlightPath);
                    var image = new GameObject("Drop artwork - pooled at runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(root.transform, false); image.rectTransform.sizeDelta = new Vector2(18, 18);
                    image.sprite = settings.walletIcon; image.preserveAspect = true; image.raycastTarget = false;
                    feedback.dropTemplate = image; image.gameObject.SetActive(false);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, Path).GetComponent<TownTreeHarvestFeedback>();
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var tablet = PrefabUtility.LoadPrefabContents(TabletPrefabBuilder.Path);
            try
            {
                var view = tablet.GetComponent<TabletView>();
                if (view.harvestFeedback == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, view.panel);
                    view.harvestFeedback = instance.GetComponent<TownTreeHarvestFeedback>();
                    var rect = (RectTransform)instance.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero; rect.SetAsLastSibling();
                    view.townWalletIcon = view.header.GetComponentsInChildren<Image>(true).Single(i => i.name == "Wallet cheese icon").rectTransform;
                    PrefabUtility.SaveAsPrefabAsset(tablet, TabletPrefabBuilder.Path);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(tablet); }
            AssetDatabase.SaveAssets(); Debug.Log("TOWN_TREE_HARVEST_PREFAB_READY");
        }
    }
}
