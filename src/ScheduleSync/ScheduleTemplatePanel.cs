using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONIUtilityTweaks.ScheduleSync
{
    internal static class ScheduleTemplatePanel
    {
        internal static readonly Color ButtonColor = new Color(0.22f, 0.28f, 0.34f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.31f, 0.38f, 0.45f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.14f, 0.18f, 0.22f, 1f);
        private static readonly Color SelectedColor = new Color(0.35f, 0.48f, 0.58f, 1f);
        private static readonly Color SelectedHoverColor = new Color(0.43f, 0.57f, 0.68f, 1f);
        private static readonly Color SelectedPressedColor = new Color(0.25f, 0.35f, 0.44f, 1f);

        private static GameObject dialogObject;
        private static KScreen dialogScreen;
        private static Transform lastParent;
        private static string selectedTemplateName;
        private static GameObject nameInput;
        private static readonly Dictionary<Schedule, GameObject> scheduleToggles = new Dictionary<Schedule, GameObject>();

        public static void Toggle(Transform parent)
        {
            if (dialogObject != null)
            {
                Close();
                return;
            }

            Show(parent, true);
        }

        private static void Show(Transform parent, bool resetSelection)
        {
            if (resetSelection)
                selectedTemplateName = null;

            lastParent = parent;
            scheduleToggles.Clear();
            nameInput = null;

            var dialog = new PDialog("ONIUtilityTweaksScheduleTemplates")
            {
                Title = "Saved Schedules",
                Size = new Vector2(860f, 560f),
                MaxSize = new Vector2(960f, 700f),
                SortKey = 1000f,
                RoundToNearestEven = true,
                DialogBackColor = PUITuning.Colors.OptionsBackground,
                DialogClosed = OnDialogClosed,
                Parent = ResolveDialogParent(parent)
            };

            dialog.Body.Direction = PanelDirection.Vertical;
            dialog.Body.Alignment = TextAnchor.UpperCenter;
            dialog.Body.Spacing = 8;
            dialog.Body.Margin = new RectOffset(8, 8, 8, 8);
            dialog.Body.AddChild(BuildDialogBody());
            dialog.Body.AddChild(new PLabel("CloudHint")
            {
                Text = "Saved schedules are mirrored into ONI cloud_save_files when available.",
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                FlexSize = Vector2.right
            });

            dialog.AddOnRealize(obj =>
            {
                dialogObject = obj;
                dialogScreen = obj.GetComponent<KScreen>();
                obj.transform.SetAsLastSibling();

                var canvas = obj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 1000;
                }
            });
            dialog.Show();
        }

        private static GameObject ResolveDialogParent(Transform scheduleScreenTransform)
        {
            if (scheduleScreenTransform != null)
                return scheduleScreenTransform.gameObject;

            return PDialog.GetParentObject();
        }

        private static IUIComponent BuildDialogBody()
        {
            var body = new PGridPanel("SavedSchedulesBody")
            {
                FlexSize = Vector2.one,
                Margin = new RectOffset(0, 0, 0, 0)
            };

            body.AddColumn(new GridColumnSpec(310f));
            body.AddColumn(new GridColumnSpec(12f));
            body.AddColumn(new GridColumnSpec(500f, 1f));
            body.AddRow(new GridRowSpec(370f, 1f));
            body.AddRow(new GridRowSpec());

            body.AddChild(BuildTemplateSection(), new GridComponentSpec(0, 0)
            {
                RowSpan = 2,
                Alignment = TextAnchor.UpperCenter
            });
            body.AddChild(BuildScheduleSection(), new GridComponentSpec(0, 2)
            {
                Alignment = TextAnchor.UpperCenter
            });
            body.AddChild(BuildActionSection(), new GridComponentSpec(1, 2)
            {
                Alignment = TextAnchor.LowerCenter,
                Margin = new RectOffset(0, 0, 8, 0)
            });

            return body;
        }

        private static IUIComponent BuildTemplateSection()
        {
            var templates = ScheduleTemplateStore.LoadTemplates();
            var list = new PPanel("TemplateList")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 4,
                FlexSize = Vector2.right
            };

            if (templates.Count == 0)
            {
                list.AddChild(CreateLabel("No saved templates yet.", TextAnchor.MiddleLeft));
            }
            else
            {
                for (var i = 0; i < templates.Count; i++)
                {
                    var template = templates[i];
                    var selected = string.Equals(template.Name, selectedTemplateName, StringComparison.OrdinalIgnoreCase);
                    var button = new PButton($"Template{i}")
                    {
                        Text = template.Name,
                        ToolTip = template.SavedAtUtc,
                        TextAlignment = TextAnchor.MiddleLeft,
                        FlexSize = Vector2.right,
                        OnClick = _ =>
                        {
                            selectedTemplateName = template.Name;
                            Refresh();
                        }
                    };

                    if (selected)
                        button.SetKleiPinkStyle();
                    else
                        button.SetKleiBlueStyle();

                    list.AddChild(button);
                }
            }

            return CreateSection("SavedTemplates", "Saved Templates", new PScrollPane("TemplateScroll")
            {
                Child = list,
                ScrollVertical = true,
                AlwaysShowVertical = false,
                TrackSize = 8f,
                FlexSize = Vector2.one,
                BackColor = PUITuning.Colors.OptionsBackground
            });
        }

        private static IUIComponent BuildScheduleSection()
        {
            var schedules = ScheduleManager.Instance?.GetSchedules();
            var list = new PPanel("ScheduleList")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 4,
                FlexSize = Vector2.right
            };

            if (schedules == null || schedules.Count == 0)
            {
                list.AddChild(CreateLabel("No schedules found in this colony.", TextAnchor.MiddleLeft));
            }
            else
            {
                for (var i = 0; i < schedules.Count; i++)
                {
                    var schedule = schedules[i];
                    var checkbox = new PCheckBox($"Schedule{i}")
                    {
                        Text = schedule.name,
                        ToolTip = schedule.name,
                        InitialState = PCheckBox.STATE_CHECKED,
                        CheckSize = new Vector2(20f, 20f),
                        TextAlignment = TextAnchor.MiddleLeft,
                        FlexSize = Vector2.right,
                        OnChecked = ToggleCheckbox
                    };

                    checkbox.SetKleiBlueStyle();
                    checkbox.AddOnRealize(obj => scheduleToggles[schedule] = obj);
                    list.AddChild(checkbox);
                }
            }

            return CreateSection("SchedulesToSave", "Schedules To Save", new PScrollPane("ScheduleScroll")
            {
                Child = list,
                ScrollVertical = true,
                AlwaysShowVertical = false,
                TrackSize = 8f,
                FlexSize = Vector2.one,
                BackColor = PUITuning.Colors.OptionsBackground
            });
        }

        private static IUIComponent BuildActionSection()
        {
            var section = new PPanel("TemplateActionsSection")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 8,
                Margin = new RectOffset(10, 10, 10, 10),
                BackColor = PUITuning.Colors.DialogDarkBackground,
                FlexSize = Vector2.right
            };

            section.AddChild(CreateLabel("Template name", TextAnchor.MiddleLeft));
            section.AddChild(new PTextField("TemplateNameInput")
            {
                Text = selectedTemplateName ?? $"Schedule {System.DateTime.Now:yyyy-MM-dd HH-mm}",
                PlaceholderText = "Template name",
                MaxLength = 96,
                MinWidth = 470,
                TextAlignment = TextAlignmentOptions.Left,
                BackColor = PUITuning.Colors.OptionsBackground,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                FlexSize = Vector2.right
            }.AddOnRealize(obj => nameInput = obj));

            var buttons = new PPanel("TemplateActions")
            {
                Direction = PanelDirection.Horizontal,
                Alignment = TextAnchor.MiddleCenter,
                Spacing = 8,
                FlexSize = Vector2.right
            };

            buttons.AddChild(new PButton("SaveAll")
            {
                Text = "Save All",
                OnClick = _ => SaveAll()
            }.SetKleiPinkStyle());
            buttons.AddChild(new PButton("SaveSelected")
            {
                Text = "Save Selected",
                OnClick = _ => SaveSelected()
            }.SetKleiPinkStyle());
            buttons.AddChild(new PButton("LoadSelected")
            {
                Text = "Load",
                OnClick = _ => LoadSelected()
            }.SetKleiBlueStyle());
            buttons.AddChild(new PButton("RemoveSelected")
            {
                Text = "Remove",
                OnClick = _ => RemoveSelected()
            }.SetKleiBlueStyle());

            section.AddChild(buttons);
            return section;
        }

        private static void SaveAll()
        {
            var templateName = GetTemplateName();
            ScheduleTemplateSync.SaveTemplate(templateName, ScheduleManager.Instance.GetSchedules());
            selectedTemplateName = templateName;
            Refresh();
        }

        private static void SaveSelected()
        {
            var selectedSchedules = scheduleToggles
                .Where(pair => PCheckBox.GetCheckState(pair.Value) == PCheckBox.STATE_CHECKED)
                .Select(pair => pair.Key)
                .ToList();

            if (selectedSchedules.Count == 0)
                selectedSchedules = ScheduleManager.Instance.GetSchedules();

            var templateName = GetTemplateName();
            ScheduleTemplateSync.SaveTemplate(templateName, selectedSchedules);
            selectedTemplateName = templateName;
            Refresh();
        }

        private static void LoadSelected()
        {
            var template = GetSelectedTemplate();
            if (template == null)
                return;

            ScheduleTemplateSync.ApplyTemplate(template);
            Close();
        }

        private static void RemoveSelected()
        {
            if (string.IsNullOrWhiteSpace(selectedTemplateName))
                return;

            ScheduleTemplateStore.Remove(selectedTemplateName);
            selectedTemplateName = null;
            Refresh();
        }

        private static ScheduleTemplateSet GetSelectedTemplate()
        {
            return ScheduleTemplateStore.LoadTemplates()
                .FirstOrDefault(template => string.Equals(template.Name, selectedTemplateName, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetTemplateName()
        {
            var text = nameInput == null ? null : PTextField.GetText(nameInput);
            return string.IsNullOrWhiteSpace(text)
                ? $"Schedule {System.DateTime.Now:yyyy-MM-dd HH-mm}"
                : text.Trim();
        }

        private static void Refresh()
        {
            if (dialogObject == null)
                return;

            var parent = lastParent;
            CloseDialogOnly();
            if (parent != null)
                Show(parent, false);
        }

        public static void Close()
        {
            if (dialogObject == null)
                return;

            selectedTemplateName = null;
            lastParent = null;
            CloseDialogOnly();
        }

        private static IUIComponent CreateSection(string name, string title, IUIComponent content)
        {
            return new PPanel(name)
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                Spacing = 6,
                Margin = new RectOffset(10, 10, 10, 10),
                BackColor = PUITuning.Colors.DialogDarkBackground,
                FlexSize = Vector2.one
            }
            .AddChild(CreateLabel(title, TextAnchor.MiddleLeft))
            .AddChild(content);
        }

        private static PLabel CreateLabel(string text, TextAnchor alignment)
        {
            return new PLabel("Label")
            {
                Text = text,
                TextAlignment = alignment,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                FlexSize = Vector2.right
            };
        }

        private static void ToggleCheckbox(GameObject source, int state)
        {
            PCheckBox.SetCheckState(source, state == PCheckBox.STATE_UNCHECKED
                ? PCheckBox.STATE_CHECKED
                : PCheckBox.STATE_UNCHECKED);
        }

        private static void OnDialogClosed(string option)
        {
            dialogObject = null;
            dialogScreen = null;
            nameInput = null;
            scheduleToggles.Clear();
            lastParent = null;
        }

        private static void CloseDialogOnly()
        {
            var screen = dialogScreen;
            var obj = dialogObject;
            dialogObject = null;
            dialogScreen = null;
            nameInput = null;
            scheduleToggles.Clear();

            if (screen != null)
                screen.Deactivate();
            else if (obj != null)
                UnityEngine.Object.Destroy(obj);
        }

        internal static void ConfigureButtonColors(Button button, bool selected = false)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = button.GetComponent<Image>();
            button.colors = CreateSelectableColors(selected);
        }

        private static ColorBlock CreateSelectableColors(bool selected)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = selected ? SelectedColor : ButtonColor;
            colors.highlightedColor = selected ? SelectedHoverColor : ButtonHoverColor;
            colors.pressedColor = selected ? SelectedPressedColor : ButtonPressedColor;
            colors.selectedColor = selected ? SelectedHoverColor : ButtonHoverColor;
            colors.disabledColor = new Color(0.12f, 0.13f, 0.15f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.04f;
            return colors;
        }
    }

    [HarmonyPatch(typeof(ScheduleScreen), "OnSpawn")]
    internal static class ScheduleScreenTemplateButtonPatch
    {
        private const float HeaderButtonWidth = 132f;
        private const float HeaderButtonHeight = 25f;
        private const float HeaderButtonGap = 8f;
        private const string HeaderButtonName = "ONIUtilityTweaksTemplatesButton";

        public static void Postfix(ScheduleScreen __instance)
        {
            EnsureButton(__instance);
        }

        public static void EnsureButton(ScheduleScreen screen)
        {
            if (screen == null)
                return;

            if (!ModSettings.Current.EnableScheduleTemplates)
            {
                RemoveButton(screen.transform);
                return;
            }

            if (HasButton(screen.transform))
                return;

            var closeButton = AccessTools.Field(typeof(ScheduleScreen), "closeButton")?.GetValue(screen) as KButton;
            var closeRect = closeButton != null ? closeButton.GetComponent<RectTransform>() : null;
            if (closeRect != null)
                CreateButtonNextToCloseButton(closeRect, screen.transform);
            else
                CreateHeaderButton(screen.transform);

            Debug.Log("[ONIUtilityTweaks] Added Saved Schedules button to Schedule screen.");
        }

        public static void ApplyCurrentSettings()
        {
            if (ScheduleScreen.Instance != null)
                EnsureButton(ScheduleScreen.Instance);
        }

        private static void CreateButtonNextToCloseButton(RectTransform closeRect, Transform scheduleScreenTransform)
        {
            GameObject obj = CreateButton(closeRect.parent, scheduleScreenTransform);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            var titleBar = FindScheduleEditorTitleBar(scheduleScreenTransform);
            rect.anchoredPosition = ResolveSavedButtonPosition(closeRect, titleBar, HeaderButtonGap);
        }

        private static Vector2 ResolveSavedButtonPosition(RectTransform closeRect, RectTransform titleBarRect, float gap)
        {
            var parentRect = closeRect.parent as RectTransform;
            if (parentRect == null)
                return new Vector2(-72f, 0f);

            var closeLeftWorld = closeRect.TransformPoint(new Vector3(closeRect.rect.xMin, closeRect.rect.center.y, 0f));
            var closeLeftLocal = parentRect.InverseTransformPoint(closeLeftWorld);

            var titleCenterLocal = titleBarRect != null
                ? parentRect.InverseTransformPoint(titleBarRect.TransformPoint(titleBarRect.rect.center))
                : new Vector3(closeLeftLocal.x, closeLeftLocal.y, 0f);

            var anchorLocal = parentRect.rect.center;
            return new Vector2(closeLeftLocal.x - gap, titleCenterLocal.y) - anchorLocal;
        }

        private static int ResolveScheduleEditorTitleFontSize(Transform scheduleScreenTransform)
        {
            var titleRect = FindScheduleEditorTitle(scheduleScreenTransform);
            if (titleRect == null)
                return 16;

            var locText = titleRect.GetComponent<LocText>();
            if (locText != null)
                return Mathf.RoundToInt(locText.fontSize);

            var uiText = titleRect.GetComponent<Text>();
            return uiText != null ? uiText.fontSize : 16;
        }

        private static void CreateHeaderButton(Transform parent)
        {
            GameObject obj = CreateButton(parent, parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(
                ResolveLeftOfCloseButtonAnchoredX(parent, HeaderButtonGap),
                ResolveTitleCenterAnchoredY(parent));
        }

        private static GameObject CreateButton(Transform parent, Transform scheduleScreenTransform)
        {
            var obj = new GameObject(HeaderButtonName, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsLastSibling();
            obj.GetComponent<LayoutElement>().ignoreLayout = true;

            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(HeaderButtonWidth, HeaderButtonHeight);
            obj.GetComponent<Image>().color = ScheduleTemplatePanel.ButtonColor;
            ScheduleTemplatePanelAccessor.CreateButtonText(
                obj.transform,
                "Saved",
                ResolveScheduleEditorTitleFontSize(scheduleScreenTransform));

            var button = obj.GetComponent<Button>();
            ScheduleTemplatePanel.ConfigureButtonColors(button);
            button.onClick.AddListener(() =>
            {
                if (ModSettings.Current.EnableScheduleTemplates)
                    ScheduleTemplatePanel.Toggle(scheduleScreenTransform);
            });
            return obj;
        }

        private static float ResolveTitleCenterAnchoredY(Transform scheduleScreenTransform)
        {
            var parentRect = scheduleScreenTransform as RectTransform;
            var titleRect = FindScheduleEditorTitle(scheduleScreenTransform);
            if (parentRect == null || titleRect == null)
                return -20f;

            var titleCenterWorld = titleRect.TransformPoint(titleRect.rect.center);
            var titleCenterLocal = parentRect.InverseTransformPoint(titleCenterWorld);
            return titleCenterLocal.y - parentRect.rect.yMax;
        }

        private static float ResolveLeftOfCloseButtonAnchoredX(Transform scheduleScreenTransform, float gap)
        {
            var parentRect = scheduleScreenTransform as RectTransform;
            var closeRect = FindCloseButtonRect(scheduleScreenTransform);
            if (parentRect == null || closeRect == null)
                return -72f;

            var closeLeftWorld = closeRect.TransformPoint(new Vector3(closeRect.rect.xMin, closeRect.rect.center.y, 0f));
            var closeLeftLocal = parentRect.InverseTransformPoint(closeLeftWorld);
            var buttonRightLocalX = closeLeftLocal.x - gap;
            return buttonRightLocalX - parentRect.rect.xMax;
        }

        private static RectTransform FindCloseButtonRect(Transform scheduleScreenTransform)
        {
            var closeButton = AccessTools.Field(typeof(ScheduleScreen), "closeButton")?.GetValue(ScheduleScreen.Instance) as KButton;
            var closeRect = closeButton != null ? closeButton.GetComponent<RectTransform>() : null;
            if (closeRect != null)
                return closeRect;

            var candidates = scheduleScreenTransform
                .GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.GetComponent<RectTransform>())
                .Where(rect => rect != null && rect.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(rect => rect.position.x)
                .ToList();

            return candidates.FirstOrDefault();
        }

        private static RectTransform FindScheduleEditorTitle(Transform scheduleScreenTransform)
        {
            for (var cursor = scheduleScreenTransform; cursor != null; cursor = cursor.parent)
            {
                foreach (var locText in cursor.GetComponentsInChildren<LocText>(true))
                {
                    if (IsScheduleEditorTitle(locText.key) || IsScheduleEditorTitle(locText.text))
                        return locText.GetComponent<RectTransform>();
                }

                foreach (var uiText in cursor.GetComponentsInChildren<Text>(true))
                {
                    if (IsScheduleEditorTitle(uiText.text))
                        return uiText.GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private static RectTransform FindScheduleEditorTitleBar(Transform scheduleScreenTransform)
        {
            var title = FindScheduleEditorTitle(scheduleScreenTransform);
            if (title == null)
                return null;

            var titleWidth = title.rect.width;
            for (var cursor = title.parent; cursor != null; cursor = cursor.parent)
            {
                var rect = cursor as RectTransform;
                if (rect == null)
                    continue;

                var image = cursor.GetComponent<Image>();
                var looksLikeTitleBar =
                    rect.rect.width > titleWidth * 2f &&
                    rect.rect.height >= 24f &&
                    rect.rect.height <= 80f;

                if (image != null && looksLikeTitleBar)
                    return rect;
            }

            return title.parent as RectTransform;
        }

        private static bool IsScheduleEditorTitle(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.IndexOf("SCHEDULE_EDITOR", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("SCHEDULE EDITOR", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasButton(Transform parent)
        {
            return parent.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == HeaderButtonName);
        }

        private static void RemoveButton(Transform parent)
        {
            if (parent == null)
                return;

            foreach (var child in parent.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name == HeaderButtonName)
                .ToList())
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(ScheduleScreen), "OnShow")]
    internal static class ScheduleScreenTemplateButtonOnShowPatch
    {
        public static void Postfix(ScheduleScreen __instance, bool show)
        {
            if (show)
                ScheduleScreenTemplateButtonPatch.EnsureButton(__instance);
        }
    }

    internal static class ScheduleTemplatePanelAccessor
    {
        public static void CreateButtonText(Transform parent, string text, int fontSize = 14)
        {
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = obj.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }
    }
}
