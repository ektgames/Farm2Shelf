using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    public partial class EKTPhoneManager
    {
        private readonly int[][] recruitmentCounts = { new int[6], new int[1], new int[2] };
        private bool confirmingRecruitment;
        private readonly int[] lastRecruitmentResult = new int[3];

        // Pending choices are guidance only; tutorial completion still uses actual hired staff.
        public int GetPendingRecruitmentCount(int group, StaffRole role)
        {
            if (group < 0 || group >= recruitmentCounts.Length) return 0;
            int total = 0;
            for (int i = 0; i < recruitmentCounts[group].Length; i++)
                if (RecruitmentRole(group, i) == role) total += recruitmentCounts[group][i];
            return total;
        }

        private void ClearRecruitmentSelection()
        {
            foreach (var counts in recruitmentCounts) Array.Clear(counts, 0, counts.Length);
            Array.Clear(lastRecruitmentResult, 0, lastRecruitmentResult.Length);
        }

        private static string RecruitText(string tr, string en) =>
            LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish ? en : tr;

        private StaffRole RecruitmentRole(int group, int index) =>
            group == 0 ? (StaffRole)index : group == 1 ? StaffRole.Çiftçi : StaffRole.Kurye;

        private RectTransform RecruitmentBox(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Text RecruitmentLabel(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color)
        {
            var rect = RecruitmentBox(parent, "Label", min, max);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = globalFont; label.text = value; label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14; label.resizeTextMaxSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = color; label.raycastTarget = false;
            return label;
        }

        private Button RecruitmentButton(Transform parent, string name, Vector2 min, Vector2 max, string value, Action action, out Text label)
        {
            var rect = RecruitmentBox(parent, name, min, max);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(260, 48, 16, new Color(.15f, .55f, .29f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
            button.onClick.AddListener(() => action());
            label = RecruitmentLabel(rect, value, new Vector2(.04f, .05f), new Vector2(.96f, .95f), 20, Color.white);
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        private Transform RecruitmentCard(Transform parent, string name, Color accent)
        {
            var rect = RecruitmentBox(parent, name, Vector2.zero, Vector2.one);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(292, 220, 16, 2, accent, new Color(.12f, .16f, .22f, .96f));
            bg.raycastTarget = false;
            return rect;
        }

        private void RenderBulkRecruitment(Transform content, int group)
        {
            if (content == null || confirmingRecruitment) return;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(292, 220);
            grid.spacing = new Vector2(12, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;

            var counts = recruitmentCounts[group];
            var labels = new Text[counts.Length];
            var minusButtons = new Button[counts.Length];
            var plusButtons = new Button[counts.Length];
            Text summary = null, confirmLabel = null;
            Button confirm = null;
            Action update = () =>
            {
                int total = 0, salary = 0;
                for (int i = 0; i < counts.Length; i++)
                {
                    total += counts[i];
                    if (StaffManager.Instance != null) salary += counts[i] * StaffManager.Instance.GetRoleDailySalary(RecruitmentRole(group, i));
                    if (labels[i] != null) labels[i].text = RecruitText("+ İŞE AL", "+ HIRE") + " (" + counts[i] + ")";
                    if (minusButtons[i] != null) minusButtons[i].interactable = counts[i] > 0;
                    if (plusButtons[i] != null) plusButtons[i].interactable = counts[i] < 99 && StaffManager.Instance != null &&
                        (TutorialManager.Instance == null || GetPendingRecruitmentCount(group, RecruitmentRole(group, i)) <
                        TutorialManager.Instance.RecruitmentRemaining(group, RecruitmentRole(group, i)));
                }
                if (summary != null) summary.text = RecruitText($"Seçilen: {total} personel\nGünlük maaş: {salary:N0} C\nİşe alım ücretsiz", $"Selected: {total} staff\nDaily wages: {salary:N0} C\nNo hiring fee");
                if (summary != null && lastRecruitmentResult[group] > 0)
                    summary.text += RecruitText($"\n{lastRecruitmentResult[group]} personel işe alındı.", $"\n{lastRecruitmentResult[group]} staff hired.");
                if (confirmLabel != null) confirmLabel.text = RecruitText("ONAYLA", "CONFIRM") + " (" + total + ")";
                if (confirm != null) confirm.interactable = total > 0 && StaffManager.Instance != null && !confirmingRecruitment;
            };

            string[] trDescriptions = { "Kasa ve ödemeler", "Depodan reyon dizimi", "Hijyen ve temizlik", "Otopark ve güvenlik", "Müşteri ilişkileri", "Reklam ve tanıtım" };
            string[] enDescriptions = { "Checkout and payments", "Shelf restocking", "Store cleaning", "Parking and security", "Customer assistance", "Advertising and promotion" };
            for (int i = 0; i < counts.Length; i++)
            {
                int index = i;
                var role = RecruitmentRole(group, i);
                var card = RecruitmentCard(content, "Recruitment_" + group + "_" + i, new Color(.15f, .85f, .65f));
                if (role == StaffRole.Kasiyer) TutorialFocusTarget.Tag(card.gameObject, "hire_cashier");
                if (role == StaffRole.Reyoncu) TutorialFocusTarget.Tag(card.gameObject, "hire_restocker");
                if (role == StaffRole.Çiftçi) TutorialFocusTarget.Tag(card.gameObject, "hire_farmer");
                TutorialFocusTarget.Tag(card.gameObject, "hire_role_" + role);
                string title = group == 0 ? GetRoleCategoryName(i) : group == 1 ? RecruitText("Çiftlik İşçisi", "Farm Worker") :
                    i == 0 ? RecruitText("Kadın Kurye", "Female Courier") : RecruitText("Erkek Kurye", "Male Courier");
                string description = group == 0 ? RecruitText(trDescriptions[i], enDescriptions[i]) : group == 1 ?
                    RecruitText("Sulama, çapa ve hasat", "Watering, hoeing and harvesting") : RecruitText("Sipariş ve kontrat teslimatı", "Order and contract deliveries");
                RecruitmentLabel(card, title, new Vector2(.05f,.78f), new Vector2(.95f,.95f), 22, Color.white).fontStyle = FontStyle.Bold;
                RecruitmentLabel(card, description, new Vector2(.05f,.58f), new Vector2(.95f,.77f), 17, new Color(.75f,.82f,.88f));
                int wage = StaffManager.Instance != null ? StaffManager.Instance.GetRoleDailySalary(role) : 0;
                RecruitmentLabel(card, RecruitText($"Günlük maaş: {wage} C", $"Daily wage: {wage} C"), new Vector2(.05f,.39f), new Vector2(.95f,.57f), 18, Color.green);
                minusButtons[i] = RecruitmentButton(card, "Decrease", new Vector2(.05f,.08f), new Vector2(.23f,.31f), "−", () =>
                { if (counts[index] > 0) counts[index]--; lastRecruitmentResult[group] = 0; update(); }, out _);
                plusButtons[i] = RecruitmentButton(card, "HireBtn", new Vector2(.27f,.08f), new Vector2(.95f,.31f), "", () =>
                {
                    if (counts[index] < 99 && (TutorialManager.Instance == null ||
                        GetPendingRecruitmentCount(group, role) < TutorialManager.Instance.RecruitmentRemaining(group, role)))
                        counts[index]++;
                    lastRecruitmentResult[group] = 0; update();
                }, out labels[i]);
            }

            var footer = RecruitmentCard(content, "RecruitmentConfirmation", new Color(.95f,.72f,.18f));
            summary = RecruitmentLabel(footer, "", new Vector2(.05f,.36f), new Vector2(.95f,.94f), 20, Color.white);
            confirm = RecruitmentButton(footer, "ConfirmHire", new Vector2(.06f,.08f), new Vector2(.94f,.31f), "", () =>
            {
                ConfirmRecruitment(group);
                update();
            }, out confirmLabel);
            TutorialFocusTarget.Tag(confirm.gameObject, "hire_confirm_" + group);
            update();
        }

        private void ConfirmRecruitment(int group)
        {
            var manager = StaffManager.Instance;
            if (confirmingRecruitment || manager == null) return;
            confirmingRecruitment = true;
            lastRecruitmentResult[group] = 0;
            try
            {
                var counts = recruitmentCounts[group];
                for (int i = 0; i < counts.Length; i++)
                {
                    var role = RecruitmentRole(group, i);
                    while (counts[i] > 0)
                    {
                        StaffMember hired = group == 0 ? manager.HireStaffByRole(role) : group == 1 ? manager.HireFarmWorker() : manager.HireCourier(i == 0);
                        if (hired == null) break;
                        counts[i]--;
                        lastRecruitmentResult[group]++;
                        if (TutorialManager.Instance != null) TutorialManager.Instance.NotifyStaffHired(role);
                    }
                }
                if (group == 2 && CourierManager.Instance != null) CourierManager.Instance.AutoAssignCouriersToMotorcycles();
            }
            finally { confirmingRecruitment = false; }
        }
    }
}
