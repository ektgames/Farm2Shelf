using UnityEngine;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Eğitim spotlight'ının hedeflediği UI parçası.
    /// </summary>
    public class TutorialFocusTarget : MonoBehaviour
    {
        public string focusId;

        public static void Tag(GameObject go, string id)
        {
            if (go == null || string.IsNullOrEmpty(id)) return;
            TutorialFocusTarget mark = go.GetComponent<TutorialFocusTarget>();
            if (mark == null) mark = go.AddComponent<TutorialFocusTarget>();
            mark.focusId = id;
        }

        public static RectTransform FindRect(string id)
        {
            TutorialFocusTarget[] marks = Object.FindObjectsByType<TutorialFocusTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < marks.Length; i++)
            {
                TutorialFocusTarget mark = marks[i];
                if (mark == null || mark.focusId != id) continue;
                if (!mark.gameObject.activeInHierarchy) continue;
                return mark.transform as RectTransform;
            }
            return null;
        }

        public static void CollectActive(string id, System.Collections.Generic.List<RectTransform> into)
        {
            if (into == null || string.IsNullOrEmpty(id)) return;
            TutorialFocusTarget[] marks = Object.FindObjectsByType<TutorialFocusTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < marks.Length; i++)
            {
                TutorialFocusTarget mark = marks[i];
                if (mark == null || mark.focusId != id) continue;
                if (!mark.gameObject.activeInHierarchy) continue;
                RectTransform rt = mark.transform as RectTransform;
                if (rt != null) into.Add(rt);
            }
        }
    }
}
