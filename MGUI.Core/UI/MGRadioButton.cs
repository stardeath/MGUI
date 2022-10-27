﻿using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Rendering;
using MGUI.Shared.Input.Mouse;

namespace MGUI.Core.UI
{
    /// <summary>This class handles mutual exclusion between the <see cref="MGRadioButton.IsChecked"/> property of multiple <see cref="MGRadioButton"/>s belonging to the same <see cref="MGRadioButtonGroup"/></summary>
    public class MGRadioButtonGroup : ViewModelBase
    {
        public MGWindow Window { get; }
        public string Name { get; }

        /// <summary>If true, the <see cref="CheckedItem"/> can be unchecked by clicking it again.<br/>
        /// Only relevant if <see cref="AllowNullCheckedItem"/>=true.<para/>
        /// Default value: false<para/>See also: <see cref="ActualAllowUnchecking"/>, <see cref="AllowNullCheckedItem"/></summary>
        public bool AllowUnchecking { get; set; }
        public bool ActualAllowUnchecking => AllowUnchecking && AllowNullCheckedItem;

        private bool _AllowNullCheckedItem;
        /// <summary>If true, <see cref="CheckedItem"/> can be set to null.<para/>
        /// Default value: false</summary>
        public bool AllowNullCheckedItem
        {
            get => _AllowNullCheckedItem;
            set
            {
                if (_AllowNullCheckedItem != value)
                {
                    _AllowNullCheckedItem = value;
                    NPC(nameof(AllowNullCheckedItem));
                    NPC(nameof(ActualAllowUnchecking));

                    if (!AllowNullCheckedItem)
                        CheckedItem ??= _RadioButtons.FirstOrDefault();
                }
            }
        }

        private MGRadioButton _CheckedItem;
        public MGRadioButton CheckedItem
        {
            get => _CheckedItem;
            set
            {
                MGRadioButton Value = AllowNullCheckedItem ? value : value ?? _RadioButtons.FirstOrDefault();
                if (_CheckedItem != Value)
                {
                    MGRadioButton Previous = CheckedItem;
                    _CheckedItem = Value;
                    NPC(nameof(CheckedItem));
                    Previous?.HandleCheckStateChanged();
                    CheckedItem?.HandleCheckStateChanged();
                }
            }
        }

        private ObservableCollection<MGRadioButton> _RadioButtons { get; }
        public IReadOnlyList<MGRadioButton> RadioButtons => _RadioButtons;
        public void AddRadioButton(MGRadioButton RB) => _RadioButtons.Add(RB);
        public bool RemoveRadioButton(MGRadioButton RB) => _RadioButtons.Remove(RB);

        public MGRadioButtonGroup(MGWindow Window, string Name)
        {
            if (Window.HasRadioButtonGroup(Name))
                throw new ArgumentException($"{nameof(Name)} '{Name}' must be unique within the scope of its {nameof(MGWindow)}.");

            this.Name = Name;
            this._RadioButtons = new();
            this.AllowUnchecking = false;
            this.AllowNullCheckedItem = false;

            _RadioButtons.CollectionChanged += (sender, e) =>
            {
                if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                {
                    bool RemovedCheckedItem = CheckedItem != null && e.OldItems.Cast<MGRadioButton>().Contains(CheckedItem);
                    if (RemovedCheckedItem)
                        CheckedItem = null;
                }

                if (!AllowNullCheckedItem)
                    CheckedItem ??= _RadioButtons.FirstOrDefault();
            };
        }
    }

    public class MGRadioButton : MGSingleContentHost
    {
        /// <summary>Provides direct access to the button component that appears to the left of this radiobutton's content.</summary>
        public MGComponent<MGButton> ButtonComponent { get; }
        /// <summary>The checkable button portion of this <see cref="MGRadioButton"/></summary>
        private MGButton ButtonElement { get; }

        /// <summary>The default width/height of the checkable part of an <see cref="MGRadioButton"/></summary>
        public const int DefaultBubbleSize = 16;
        /// <summary>The default empty width between the checkable part of an <see cref="MGRadioButton"/> and its <see cref="MGSingleContentHost.Content"/></summary>
        public const int DefaultBubbleSpacingWidth = 5;

        public MGRadioButtonGroup Group { get; }

        private Size GetButtonComponentPreferredSize() => new(BubbleComponentSize, BubbleComponentSize);

        private int _BubbleComponentSize;
        /// <summary>The dimensions of the checkable part of this <see cref="MGRadioButton"/>.<para/>
        /// See also: <see cref="DefaultBubbleSize"/></summary>
        public int BubbleComponentSize
        {
            get => _BubbleComponentSize;
            set
            {
                if (_BubbleComponentSize != value)
                {
                    _BubbleComponentSize = value;
                    NPC(nameof(BubbleComponentSize));

                    Size ButtonSize = GetButtonComponentPreferredSize();
                    ButtonElement.PreferredWidth = ButtonSize.Width;
                    ButtonElement.PreferredHeight = ButtonSize.Height;
                }
            }
        }

        private Color _BubbleComponentBorderColor;
        /// <summary>The <see cref="Color"/> to use when drawing the Border of the checkable part.</summary>
        public Color BubbleComponentBorderColor
        {
            get => _BubbleComponentBorderColor;
            set
            {
                if (_BubbleComponentBorderColor != value)
                {
                    _BubbleComponentBorderColor = value;
                    NPC(nameof(BubbleComponentBorderColor));
                }
            }
        }

        private float _BubbleComponentBorderThickness;
        /// <summary>The thickness to use for the checkable part's border.<br/>
        /// This value cannot exceed <see cref="BubbleComponentSize"/>/2.0<para/>
        /// Recommended value: 1 or 2</summary>
        public float BubbleComponentBorderThickness
        {
            get => _BubbleComponentBorderThickness;
            set
            {
                if (_BubbleComponentBorderThickness != value)
                {
                    _BubbleComponentBorderThickness = value;
                    NPC(nameof(BubbleComponentBorderThickness));
                }
            }
        }

        private Color _BubbleComponentFillColor;
        /// <summary>The background <see cref="Color"/> to use when drawing the checkable part.</summary>
        public Color BubbleComponentFillColor
        {
            get => _BubbleComponentFillColor;
            set
            {
                if (_BubbleComponentFillColor != value)
                {
                    _BubbleComponentFillColor = value;
                    NPC(nameof(BubbleComponentFillColor));
                }
            }
        }

        private Color _BubbleCheckedColor;
        /// <summary>The <see cref="Color"/> to use when filling in the checkable bubble part when <see cref="IsChecked"/> is true.</summary>
        public Color BubbleCheckedColor
        {
            get => _BubbleCheckedColor;
            set
            {
                if (_BubbleCheckedColor != value)
                {
                    _BubbleCheckedColor = value;
                    NPC(nameof(BubbleCheckedColor));
                }
            }
        }

        /// <summary>The reserved empty width between the checkable part of this <see cref="MGRadioButton"/> and its <see cref="MGSingleContentHost.Content"/>.<para/>
        /// See also: <see cref="DefaultBubbleSpacingWidth"/>.<para/>
        /// This value is functionally equivalent to <see cref="ButtonElement"/>'s right <see cref="MGElement.Margin"/></summary>
        public int SpacingWidth
        {
            get => ButtonElement.Margin.Right;
            set => ButtonElement.Margin = new(ButtonElement.Margin.Left, ButtonElement.Margin.Top, value, ButtonElement.Margin.Bottom);
        }

        private Color _HoveredHighlightColor;
        /// <summary>An overlay color that is drawn overtop of checkable portion of this <see cref="MGRadioButton"/>'s Background if the mouse is currently hovering it.<br/>
        /// Recommended to use a transparent color.<para/>
        /// Default Value: <see cref="MGElement.DefaultHoveredHighlightColor"/></summary>
        public Color HoveredHighlightColor
        {
            get => _HoveredHighlightColor;
            set
            {
                if (_HoveredHighlightColor != value)
                {
                    _HoveredHighlightColor = value;
                    NPC(nameof(HoveredHighlightColor));
                    HoveredOverlay = HoveredHighlightColor;
                    PressedOverlay = HoveredHighlightColor.Darken(PressedDarkenIntensity);
                    HoveredBorderOverlay = HoveredOverlay;
                    PressedBorderOverlay = PressedOverlay;
                }
            }
        }

        private float _PressedDarkenIntensity;
        /// <summary>A percentage to darken the background color by when the mouse is currently pressed, but not yet released, overtop of the checkable portion of this <see cref="MGCheckBox"/>.<br/>
        /// Use a larger value to apply a more obvious background overlay while this <see cref="MGElement"/> is pressed. Use a smaller value for a more subtle change.<para/>
        /// Default value: <see cref="MGElement.DefaultPressedDarkenIntensity"/></summary>
        public float PressedDarkenIntensity
        {
            get => _PressedDarkenIntensity;
            set
            {
                if (_PressedDarkenIntensity != value)
                {
                    _PressedDarkenIntensity = value;
                    NPC(nameof(PressedDarkenIntensity));
                    PressedOverlay = HoveredHighlightColor.Darken(PressedDarkenIntensity);
                    PressedBorderOverlay = PressedOverlay;
                }
            }
        }

        private Color HoveredOverlay { get; set; }
        private Color PressedOverlay { get; set; }
        private Color HoveredBorderOverlay { get; set; }
        private Color PressedBorderOverlay { get; set; }

        public bool IsChecked
        {
            get => Group.CheckedItem == this;
            set
            {
                if (value)
                    Group.CheckedItem = this;
                else if (IsChecked && Group.AllowUnchecking)
                    Group.CheckedItem = null;
            }
        }

        internal void HandleCheckStateChanged()
        {
            NPC(nameof(IsChecked));
            OnCheckStateChanged?.Invoke(this, IsChecked);
            if (IsChecked)
                OnChecked?.Invoke(this, EventArgs.Empty);
            else
                OnUnchecked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Note: This event is invoked before <see cref="OnChecked"/> / <see cref="OnUnchecked"/></summary>
        public event EventHandler<bool> OnCheckStateChanged;
        public event EventHandler<EventArgs> OnChecked;
        public event EventHandler<EventArgs> OnUnchecked;

        public MGRadioButton(MGWindow Window, string GroupName)
            : this(Window, Window.GetOrCreateRadioButtonGroup(GroupName)) { }

        public MGRadioButton(MGWindow Window, MGRadioButtonGroup Group)
            : base(Window, MGElementType.RadioButton)
        {
            using (BeginInitializing())
            {
                this.Group = Group;
                Group.AddRadioButton(this);

                this.ButtonElement = new(Window, x => this.IsChecked = !this.IsChecked);
                this.ButtonElement.Visibility = Visibility.Hidden;
                this.ButtonElement.CanHandleInputsWhileHidden = true;

                this.ButtonComponent = new(ButtonElement, false, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Center, ComponentSize.Size));
                AddComponent(ButtonComponent);

                this.BubbleComponentSize = DefaultBubbleSize;
                this.BubbleComponentBorderColor = Color.Black;
                this.BubbleComponentBorderThickness = 1;
                this.BubbleComponentFillColor = GetTheme().RadioButtonBubbleFillColor;
                this.BubbleCheckedColor = Color.Green;

                this.SpacingWidth = DefaultBubbleSpacingWidth;

                this.HoveredHighlightColor = GetTheme().HoveredColor;
                this.PressedDarkenIntensity = GetTheme().PressedDarkenModifier;
            }
        }

        /// <summary>The number of sides to use when approximating a circle as a polygon.<para/>
        /// Recommended value: 32</summary>
        private const int CircleDetailLevel = 32; // 16 looks bad

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;

            Rectangle BubblePartBounds = ButtonElement.LayoutBounds;
            bool IsBubblePartPressed = MouseHandler.Tracker.IsPressedInside(MouseButton.Left, BubblePartBounds, AsIViewport().GetOffset());
            bool IsBubblePartHovered = BubblePartBounds.ContainsInclusive(MouseHandler.Tracker.CurrentPosition.ToVector2() + AsIViewport().GetOffset());

            Point BubbleCenter = BubblePartBounds.Center + DA.Offset;
            int BubbleRadius = BubblePartBounds.Width / 2;

            DT.StrokeAndFillCircle(BubbleCenter.ToVector2(), BubbleComponentBorderColor * Opacity, BubbleComponentFillColor * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);

            if (!ParentWindow.HasModalWindow)
            {
                if (IsBubblePartPressed)
                {
                    DT.StrokeAndFillCircle(BubbleCenter.ToVector2(), PressedBorderOverlay * Opacity, PressedOverlay * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);
                }
                else if (IsBubblePartHovered)
                {
                    DT.StrokeAndFillCircle(BubbleCenter.ToVector2(), HoveredBorderOverlay * Opacity, HoveredOverlay * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);
                }
            }

            if (IsChecked)
            {
                int InnerRadius = BubbleRadius - 4;
                DT.FillCircle(BubbleCenter.ToVector2(), BubbleCheckedColor * Opacity, InnerRadius, CircleDetailLevel);
            }
        }
    }
}
