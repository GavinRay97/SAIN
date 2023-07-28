﻿using BepInEx.Configuration;
using UnityEngine;
using static SAIN.Editor.EditorParameters;
using static SAIN.Editor.ConfigValues;
using static SAIN.Editor.StyleOptions;
using static SAIN.Editor.ToolTips;
using SAIN.BotPresets;
using System.Collections.Generic;
using static SAIN.Editor.Names.StyleNames;
using SAIN.Editor.Abstract;
using SAIN.Editor.Util;

namespace SAIN.Editor
{
    public class BuilderClass : EditorAbstract
    {
        public BuilderClass(SAINEditor editor) : base(editor) { }

        private static float ExpandMenuWidth => 150f;
        private static float MinMaxWidth => SliderMinMaxWidth.Value;
        private static float SlWidth => SliderWidth.Value;
        private static float ResultWidth => SliderResultWidth.Value;
        private static float LabelWidth => SliderLabelWidth.Value;

        CustomStyleClass CustomStyle => Editor.StyleOptions.CustomStyle;

        public string SelectionGridExpandHeight(Rect menuRect, string[] options, string selectedOption, Rect[] optionRects, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            BeginGroup(menuRect);
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                bool selected = selectedOption == option;

                optionRects[i] = AnimateHeight(optionRects[i], selected, menuRect.height, out bool hovering, min, incPerFrame, closeMulti);

                GUIStyle style = StyleHandler(selected, hovering);

                bool toggleActivated = GUI.Button(optionRects[i], option, style);
                if (toggleActivated && selected)
                {
                    selectedOption = "None";
                }
                if (toggleActivated && !selected)
                {
                    selectedOption = option;
                }
            }
            EndGroup();
            return selectedOption;
        }

        GUIStyle StyleHandler(bool selected, bool hovering)
        {
            var style = CustomStyle.GetFontStyleDynamic(selectionGrid, selected);
            Texture2D texture;
            if (selected)
            {
                //Color ColorGold = Editor.Colors.GetColor(Names.ColorNames.Gold);
                texture = Editor.TexturesClass.GetColor(Names.ColorNames.DarkRed);
                //ApplyToStyle.ApplyTextColorAllStates(style, ColorGold);
            }
            else if (hovering)
            {
                texture = Editor.TexturesClass.GetColor(Names.ColorNames.LightRed);
            }
            else
            {
                texture = Editor.TexturesClass.GetColor(Names.ColorNames.MidGray);
            }
            ApplyToStyle.BGAllStates(style, texture);
            return style;
        }

        public string SelectionGridExpandWidth(Rect menuRect, string[] options, string selectedOption, Rect[] optionRects, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            BeginGroup(menuRect);
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                bool selected = selectedOption == option;

                optionRects[i] = AnimateWidth(optionRects[i], selected, menuRect.width, out bool hovering, min, incPerFrame, closeMulti);

                GUIStyle style = StyleHandler(selected, hovering);
                bool toggleActivated = GUI.Button(optionRects[i], option, style);
                if (toggleActivated && selected)
                {
                    selectedOption = "None";
                }
                if (toggleActivated && !selected)
                {
                    selectedOption = option;
                }
            }
            EndGroup();
            return selectedOption;
        }

        public void SelectionGridExpandWidth(Rect menuRect, string[] options, List<string> selectedList, Rect[] optionRects, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            //BeginGroup(menuRect);
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                bool selected = selectedList.Contains(option);

                optionRects[i] = AnimateWidth(optionRects[i], selected, menuRect.width, out bool hovering, min, incPerFrame, closeMulti);

                GUIStyle style = StyleHandler(selected, hovering);

                bool toggleActivated = GUI.Button(optionRects[i], option, style);
                if (toggleActivated && selected)
                {
                    if (selectedList.Contains(option))
                    {
                        selectedList.Remove(option);
                    }
                }
                if (toggleActivated && !selected)
                {
                    if (!selectedList.Contains(option))
                    {
                        selectedList.Add(option);
                    }
                }
            }
            //EndGroup();
        }

        public Rect[] VerticalGridRects(Rect MenuRect, int count, float startWidth)
        {
            Rect[] rects = new Rect[count];

            float optionHeight = MenuRect.height / count;
            float X = 0;

            for (int i = 0; i < rects.Length; i++)
            {
                float Y = optionHeight * i;
                rects[i] = new Rect
                {
                    x = X,
                    y = Y,
                    width = startWidth,
                    height = optionHeight
                };
            }
            return rects;
        }

        public Rect[] HorizontalGridRects(Rect MenuRect, int count, float startHeight)
        {
            Rect[] rects = new Rect[count];

            float optionWidth = MenuRect.width / count;
            float Y = 0;

            for (int i = 0; i < rects.Length; i++)
            {
                float X = optionWidth * i;
                rects[i] = new Rect
                {
                    x = X,
                    y = Y,
                    width = optionWidth,
                    height = startHeight
                };
            }
            return rects;
        }

        private Rect AnimateHeight(Rect rect, bool selected, float max, out bool hovering, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            Rect detectRect = rect;
            detectRect.height = max;
            hovering = MouseFunctions.IsMouseInside(detectRect);
            rect.height = Animate(rect.height, hovering, selected, max, min, incPerFrame, closeMulti);
            return rect;
        }

        private Rect AnimateWidth(Rect rect, bool selected, float max, out bool hovering, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            Rect detectRect = rect;
            detectRect.width = max;
            hovering = MouseFunctions.IsMouseInside(detectRect);
            rect.width = Animate(rect.width, hovering, selected, max, min, incPerFrame, closeMulti);
            return rect;
        }

        private float Animate(float current, bool mouseHover, bool selected, float max, float min = 15f, float incPerFrame = 3f, float closeMulti = 0.66f)
        {
            if (mouseHover || selected)
            {
                current += incPerFrame;
            }
            else
            {
                current -= incPerFrame * closeMulti;
            }
            current = Mathf.Clamp(current, min, max);
            return current;
        }

        public bool ExpandableMenu(string name, bool value, string description = null)
        {
            BeginHorizontal();
            ButtonsClass.InfoBox(description);
            Label(name, Width(ExpandMenuWidth));
            value = Toggle(value, value ? "[Close]" : "[Open]");
            EndHorizontal();
            return value;
        }

        public bool CreateButtonOption(ConfigEntry<bool> entry)
        {
            bool value = false;
            BeginHorizontal();
            if (ButtonsClass.ButtonConfigEntry(entry))
            {
                value = true;
            }
            EndHorizontal();
            return value;
        }

        public bool CreateButtonOption(SAINProperty<bool> entry, BotDifficulty difficulty)
        {
            bool value = false;
            BeginHorizontal();
            if (ButtonsClass.ButtonProperty(entry, difficulty))
            {
                value = true;
            }
            EndHorizontal();
            return value;
        }

        public void HorizSlider<T>(ConfigEntry<T> entry, float rounding)
        {
            BeginHorizontal();

            ButtonsClass.InfoBox(entry.Description.Description, 35f);
            Box(entry.Definition.Key, Width(LabelWidth), Height(35));

            object min = MinMax(entry, out object max);

            if (min != null)
            {
                BlankBox(min.ToString(), "Minimum", Width(MinMaxWidth), Height(35));
                CheckMouse("Min");
            }

            if (ReturnFloat(entry, out float sliderValue))
            {
                sliderValue = CreateSlider(sliderValue, min, max, rounding, 35f);
                AssignValue(entry, sliderValue);
            }
            else
            {
                bool boolValue = (bool)(object)entry.Value;
                boolValue = Toggle((bool)(object)entry.Value, boolValue ? "On" : "Off", Height(35));
                AssignValue((ConfigEntry<bool>)(object)entry, boolValue);
            }

            if (max != null)
            {
                BlankBox(max.ToString(), "Maximum", Width(MinMaxWidth), Height(35));
                CheckMouse("Max");
            }

            Box(entry.Value.ToString(), "Set Value", Width(ResultWidth), Height(35));
            FlexibleSpace();
            ButtonsClass.ResetButton(entry, 35f);
            EndHorizontal();
        }

        private object MinMax<T>(ConfigEntry<T> entry, out object max)
        {
            if (entry?.Description?.AcceptableValues == null || entry.SettingType == typeof(bool))
            {
                max = null;
                return null;
            }
            if (entry.SettingType == typeof(float))
            {
                max = entry.Description.AcceptableValues.Clamp(float.MaxValue);
                return entry.Description.AcceptableValues.Clamp(float.MinValue);
            }
            else
            {
                max = entry.Description.AcceptableValues.Clamp(int.MaxValue);
                return entry.Description.AcceptableValues.Clamp(int.MinValue);
            }
        }

        public void HorizSlider(SAINProperty<float> entry, BotDifficulty difficulty)
        {
            float value = (float)entry.GetValue(difficulty);
            BeginHorizontal();

            ButtonsClass.InfoBox(entry.Description, 35f);
            Box(entry.Name, Width(LabelWidth), Height(35));

            FlexibleSpace();

            BlankBox(entry.Min.ToString(), "Minimum", Width(MinMaxWidth), Height(35));
            CheckMouse("Min");

            value = CreateSlider(value, entry.Min, entry.Max, entry.Rounding, 35f);

            BlankBox(entry.Max.ToString(), "Maximum", Width(MinMaxWidth), Height(35));
            CheckMouse("Max");

            Box(value.ToString(), "Set Value", Width(ResultWidth), Height(35));
            ButtonsClass.ResetButton(entry, difficulty, 35f);
            EndHorizontal();

            entry.SetValue(difficulty, value);
        }

        public float HorizSlider(string name, float value, float min, float max, float rounding = 1f, string description = null)
        {
            BeginHorizontal();

            ButtonsClass.InfoBox(description, 35f);
            Box(name, Width(LabelWidth), Height(35));

            Box(min.ToString(), "Minimum", Width(MinMaxWidth), Height(35));

            float result = CreateSlider(value, min, max, rounding, 35f);

            Box(max.ToString(), "Maximum", Width(MinMaxWidth), Height(35));

            Box(value.ToString(), Width(ResultWidth), Height(35));

            EndHorizontal();
            return result;
        }

        public float CreateSlider(float value, object min, object max, float rounding = 1f, float? height = null)
        {
            float progress = (value - (float)min) / ((float)max - (float)min);
            if (height != null)
            {
                value = HorizontalSlider(value, (float)min, (float)max, SlWidth, height.Value);
            }
            else
            {
                value = HorizontalSlider(value, (float)min, (float)max, SlWidth);
            }
            TexturesClass.DrawSliderBackGrounds(progress);

            value = Mathf.Round(value * rounding) / rounding;
            return value;
        }
    }
}