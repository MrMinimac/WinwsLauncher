using AcrylicViews.Model;
using AcrylicViews.Utils;
using AcrylicViews.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AcrylicViews
{
    public class AcrylicContextMenu : IDisposable
    {
        private ContextMenuView menu;
        private bool disposed;

        #region Props
        public List<AcrylicMenuItem> Items { get; set; } = new List<AcrylicMenuItem>();
        public Color BackColor { get; set; } = Colors.DarkGray;

        private bool isAcrylic = true;
        public bool IsAcrylic
        {
            get => isAcrylic;
            set
            {
                if (isAcrylic != value)
                {
                    isAcrylic = value;
                }
            }
        }
        public CornerPreference CornerPreference { get; set; } = CornerPreference.Round; // Только Windows 11
        public bool AutoHeight { get; set; } = true;
        public bool AutoWidth { get; set; } = true;
        public Padding Margins { get; set; } = new Padding(Constants.DEFAULT_MARGIN_LEFT, Constants.DEFAULT_MARGIN_TOP, Constants.DEFAULT_MARGIN_RIGHT, Constants.DEFAULT_MARGIN_BOTTOM);
        public int Width { get; set; } = Constants.DEFAULT_MENU_WIDTH;
        public int Height { get; set; } = Constants.DEFAULT_MENU_HEIGHT;
        public Color BackTintColor { get; set; } = Colors.BackTintColor;
        public MenuAnimator MenuAnimator { get; set; }
        #endregion

        public void Show()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(AcrylicContextMenu));

            // Закрываем старое меню, если оно ещё активно
            if (menu != null && !menu.IsClosed)
            {
                UnsubscribeAllFlat(Items);
                menu.Close();
            }

            if (MenuAnimator == null)
                throw new Exception("MenuAnimator can't be null");

            menu = new ContextMenuView();
            menu.Margins = Margins;
            menu.Height = Height;
            menu.Width = Width;
            menu.AutoHeight = AutoHeight;
            menu.CornerPreference = CornerPreference;
            menu.AutoWidth = AutoWidth;
            menu.IsAcrylic = IsAcrylic;
            menu.BackgroundColor = IsAcrylic ? Colors.Transparent : BackColor;
            menu.BackTintColor = BackTintColor;
            menu.MenuAnimator = MenuAnimator;
            menu.FormClosed += (s, e) =>
            {
                if (!disposed)
                    UnsubscribeAllFlat(Items);
            };

            foreach (var item in Items)
            {
                var control = menu.AddItem(item);
                item.control = control;
            }

            SubscribeAllFlat(Items);
            menu.Show();
        }

        private void SubscribeAllFlat(IEnumerable<AcrylicMenuItem> items)
        {
            var stack = new Stack<AcrylicMenuItem>(items);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                current.PropertyChanged += MenuItem_PropertyChanged;

                if (current.DropDownItems != null)
                {
                    foreach (var child in current.DropDownItems)
                        stack.Push(child);
                }
            }
        }

        private void UnsubscribeAllFlat(IEnumerable<AcrylicMenuItem> items)
        {
            var stack = new Stack<AcrylicMenuItem>(items);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                current.PropertyChanged -= MenuItem_PropertyChanged;
                if (current.DropDownItems != null)
                {
                    foreach (var child in current.DropDownItems) stack.Push(child);
                }
            }

        }

        public void AddItem(AcrylicMenuItem item)
        {
            Items.Add(item);
        }

        private void MenuItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as AcrylicMenuItem;
            if (item?.control == null) return;

            var control = item.control;

            switch (e.PropertyName)
            {
                case nameof(AcrylicMenuItem.Text):
                    control.Text = item.Text;
                    break;
                case nameof(AcrylicMenuItem.Checked):
                    control.Checked = item.Checked;
                    break;
                case nameof(AcrylicMenuItem.Size):
                    control.Size = item.Size;
                    break;
                case nameof(AcrylicMenuItem.CheckMarkSize):
                    control.CheckMarkSize = item.CheckMarkSize;
                    break;
                case nameof(AcrylicMenuItem.ArrowSize):
                    control.ArrowSize = item.ArrowSize;
                    break;
                case nameof(AcrylicMenuItem.RectMargin):
                    control.RectMargins = item.RectMargin;
                    break;
                case nameof(AcrylicMenuItem.TextMargin):
                    control.TextMargin = item.TextMargin;
                    break;
                case nameof(AcrylicMenuItem.MouseEnterColor):
                    control.MouseEnterColor = item.MouseEnterColor;
                    break;
                case nameof(AcrylicMenuItem.MouseLeaveColor):
                    control.MouseLeaveColor = item.MouseLeaveColor;
                    break;
                case nameof(AcrylicMenuItem.MouseClickColor):
                    control.MouseClickColor = item.MouseClickColor;
                    break;
                case nameof(AcrylicMenuItem.MouseDownColor):
                    control.MouseDownColor = item.MouseDownColor;
                    break;
                case nameof(AcrylicMenuItem.ForeColor):
                    control.ForeColor = item.ForeColor;
                    break;
                case nameof(AcrylicMenuItem.Font):
                    control.Font = item.Font;
                    break;
                case nameof(AcrylicMenuItem.Enabled):
                    control.Enabled = item.Enabled;
                    break;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            UnsubscribeAllFlat(Items);

            if (menu != null)
            {
                try
                {
                    menu.Close();
                    menu.Dispose();
                }
                catch { }
                menu = null;
            }

            Items.Clear();
        }
    }
}
