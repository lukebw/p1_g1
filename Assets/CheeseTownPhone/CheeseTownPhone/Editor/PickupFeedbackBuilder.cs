// BEGIN ADDED: Add a reusable flight prefab without rebuilding the artist-edited tablet hierarchy.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class PickupFeedbackBuilder
    {
        public const string FlightPath = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/CheesePickupFlight.prefab";
        [MenuItem("Cheese Town/UI/Install Pickup Feedback")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing feedback.");
            var root = PrefabUtility.LoadPrefabContents(TabletPrefabBuilder.Path);
            try
            {
                var view = root.GetComponent<TabletView>();
                var target = view.worldHud.GetComponentsInChildren<Image>(true).Single(i => i.name == "HUD cheese icon");
                var prefab = AssetDatabase.LoadAssetAtPath<CheesePickupFlight>(FlightPath);
                if (prefab == null) prefab = CreateFlight(target.sprite);
                if (view.pickupFeedback == null)
                {
                    var area = new GameObject("Pickup Feedback - nonblocking UI", typeof(RectTransform), typeof(CanvasGroup), typeof(CheesePickupFeedback));
                    area.transform.SetParent(root.transform, false);
                    var rect = area.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var group = area.GetComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
                    var feedback = area.GetComponent<CheesePickupFeedback>(); feedback.flightPrefab = prefab;
                    feedback.view = view; feedback.targetIcon = target.rectTransform; view.pickupFeedback = feedback;
                    PrefabUtility.SaveAsPrefabAsset(root, TabletPrefabBuilder.Path);
                }
                AssetDatabase.SaveAssets(); Debug.Log("PICKUP_PREFAB_READY: pooled flight, pixel trail, two particle bursts and HUD pulse.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static CheesePickupFlight CreateFlight(Sprite sprite)
        {
            var root = new GameObject("CheesePickupFlight", typeof(RectTransform), typeof(CanvasGroup), typeof(CheesePickupFlight));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                var group = root.GetComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
                var flight = root.GetComponent<CheesePickupFlight>();
                flight.trail = Enumerable.Range(0, 6).Select(i => Graphic(root.transform, "Trail " + (i + 1), sprite, 18)).ToArray();
                flight.particles = Enumerable.Range(0, 12).Select(i => Graphic(root.transform,
                    (i < 6 ? "Takeoff pixel " : "Arrival pixel ") + (i % 6 + 1), null, i % 2 == 0 ? 2 : 3)).ToArray();
                flight.icon = Graphic(root.transform, "Flying cheese", sprite, 18);
                return PrefabUtility.SaveAsPrefabAsset(root, FlightPath).GetComponent<CheesePickupFlight>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static Image Graphic(Transform parent, string name, Sprite sprite, float size)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); image.rectTransform.sizeDelta = Vector2.one * size;
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            if (name != "Flying cheese") image.color = Color.clear;
            return image;
        }
    }
}
// END ADDED
