using AcrylicViews.Controls;
using AcrylicViews.Model;
using AcrylicViews.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AcrylicViews.Model.WinApi;

namespace AcrylicViews.View
{
    internal partial class ContextMenuView : Form
    {
        public Padding Margins { get; set; }
        public Color BackTintColor { get; set; }
        public bool IsAcrylic { get; set; }
        public CornerPreference CornerPreference { get; set; }
        public Color BackgroundColor { get; set; }
        public bool AutoHeight { get; set; }
        public bool AutoWidth { get; set; }
        public Point DropDownLocation { get; set; }
        public int DropDownButtonHeight { get; set; }
        public MenuAnimator MenuAnimator { get; set; }
        public bool IsClosed { get; private set; } = false;

        private ContextMenuView owner;

        private int readyItems = 0;

        public ContextMenuView(ContextMenuView owner = null)
        {
            this.owner = owner;

            InitializeComponent();

            Opacity = 0;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;

            Load += OnLoad;
            Shown += OnShown;
            Deactivate += OnDeactivate;
        }

        #region Events
        private void OnLoad(object sender, EventArgs e)
        {
            if (IsAcrylic)
            {
                EnableAcrylic.Enable(this, BackTintColor);
            }

            RoundedCorners.Apply(this, CornerPreference);
        }

        private void OnShown(object sender, EventArgs e)
        {
            Activate();
        }

        private async Task OnItemReady()
        {
            readyItems++;

            if (readyItems == Controls.Count)
            {
                if (AutoWidth)
                    SetUniformWidth();

                SetPosition();

                if (MenuAnimator != null)
                { 
                    await MenuAnimator.ShowAnimated(this);
                }
                else
                {
                    Opacity = 1;
                }
            }
        }

        private async void OnDeactivate(object sender, EventArgs e)
        {
            await Task.Delay(Constants.DEACTIVATE_DELAY_MS);

            if (IsClosed || IsDisposed || Disposing) return;

            foreach (var dropDownButton in Controls.OfType<AcrylicMenuControl>())
            {
                if (dropDownButton.DropDown.DropDownVisible)
                    return;
            }

            var cursorPos = Cursor.Position;
            if (Bounds.Contains(cursorPos))
                return;

            if (owner != null && owner.Bounds.Contains(cursorPos))
                return;

            if (Controls.OfType<AcrylicMenuControl>().Any(c => c.Bounds.Contains(PointToClient(cursorPos))))
                return;

            try
            {
                if (MenuAnimator != null && Visible && !IsClosed)
                {
                    await MenuAnimator.HideAnimated(this);
                }

                if (!IsClosed && !IsDisposed)
                    Close();
            }
            catch (ObjectDisposedException)
            {
                // уже закрыто — просто игнорируем
            }
            catch (InvalidOperationException)
            {
                // окно могло быть уничтожено — тоже игнорируем
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (IsClosed) return;
            e.Graphics.Clear(BackgroundColor);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsClosed = true;

                foreach (var control in Controls.OfType<AcrylicMenuControl>())
                {
                    if (control.DropDown.DropDownVisible)
                    {
                        control.DropDown.CancelHoverTask();
                        control.DropDown.Hide();
                    }
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (owner == null || owner.IsClosed || owner.IsDisposed || owner.Disposing) return;
            owner.Close();
        }

        #endregion

        public AcrylicMenuControl AddItem(AcrylicMenuItem item)
        {
            if (IsClosed) return null;

            int LocationY = GetNextControlY();

            var control = new AcrylicMenuControl(this);
            control.Location = new Point(0, LocationY);
            control.Text = item.Text;
            control.Size = item.Size;

            if (control.Height < Constants.DEFAULT_ITEM_HEIGHT)
            {
                control.Height = Constants.DEFAULT_ITEM_HEIGHT;
            }
            control.RectMargins = item.RectMargin;
            control.TextMargin = item.TextMargin;
            control.ArrowMargin = item.ArrowMargins;
            control.MouseEnterColor = item.MouseEnterColor;
            control.MouseLeaveColor = item.MouseLeaveColor;
            control.MouseClickColor = item.MouseClickColor;
            control.MouseDownColor = item.MouseDownColor;
            control.MouseClick += (s, e) => item.MouseClick?.Invoke(s, e);
            control.MouseDown += (s, e) => item.MouseDown?.Invoke(s, e);
            control.Checked = item.Checked;
            control.Enabled = item.Enabled;
            control.CheckMarkSize = item.CheckMarkSize;
            control.CloseAfterClick = item.CloseAfterClick;
            control.Font = item.Font;
            control.ForeColor = item.ForeColor;
            control.ArrowSize = item.ArrowSize;
            control.DropDownItems = (item.DropDownItems != null && owner == null) ? item.DropDownItems : null;
            control.Tag = item;
            control.OnPaintCompletedAsync = OnItemReady;

            if (control != null)
            {
                Controls.Add(control);

                if (AutoHeight)
                {
                    // Убеждаемся, что у элемента есть минимальная высота
                    int itemHeight = Math.Max(item.Size.Height, Constants.DEFAULT_ITEM_HEIGHT);
                    var menuHeight = Controls.Count * itemHeight + (Margins.Top + Margins.Bottom);
                    Height = menuHeight;
                }
            }

            return control;
        }

        private void SetPosition()
        {
            if (IsClosed) return;

            Point mousePosition = Cursor.Position;
            Rectangle workingArea = Screen.GetWorkingArea(mousePosition);

            if (owner == null)
            {
                int x = mousePosition.X - (Width / 2);
                int y = mousePosition.Y - Height;

                if (y + Height > workingArea.Bottom)
                {
                    var offsetY = (y + Height) - workingArea.Bottom;
                    y -= offsetY;
                }

                Location = new Point(x, y);
            }
            else
            {
                // Позиционируем dropdown меню справа от родительского меню
                int x = owner.Location.X + owner.Width;
                int y = owner.Location.Y + DropDownLocation.Y;

                // Получаем доступное пространство справа и снизу
                int screenRight = Screen.FromControl(owner).WorkingArea.Right;
                int screenBottom = Screen.FromControl(owner).WorkingArea.Bottom;
                int spaceRight = screenRight - (x + Width);
                int spaceBelow = screenBottom - (y + Height);

                // Если справа не помещается, открываем слева
                if (spaceRight < 0)
                {
                    x = owner.Location.X - Width;
                }

                // Если снизу не помещается, открываем вверх
                if (spaceBelow < 0)
                {
                    y = owner.Location.Y + DropDownLocation.Y + DropDownButtonHeight - Height + Margins.Bottom;
                }

                Location = new Point(x, y);
            }

        }

        private int GetNextControlY()
        {
            if (Controls.Count == 0)
            {
                return Margins.Top;
            }

            var last = Controls[Controls.Count - 1];
            int nextY = last.Location.Y + last.Height;
            return nextY;
        }

        private void SetUniformWidth()
        {
            if (IsClosed) return;

            var controls = Controls.OfType<AcrylicMenuControl>().ToList();
            if (controls.Count == 0) return;

            int maxWidth = controls.Max(c => c.MinWidth);

            foreach (var control in controls)
            {
                if (control.Width != maxWidth && maxWidth != 0)
                {
                    control.Width = maxWidth;
                }
            }

            if (Width != maxWidth && maxWidth != 0)
            {
                Width = maxWidth;
            }
        }

        public ContextMenuView Clone()
        {
            return new ContextMenuView(this)
            {
                Height = this.Height,
                Width = this.Width,
                Margins = this.Margins,
                BackgroundColor = this.BackgroundColor,
                BackTintColor = this.BackTintColor,
                IsAcrylic = this.IsAcrylic,
                AutoHeight = this.AutoHeight,
                CornerPreference = this.CornerPreference,
                MenuAnimator = new MenuAnimator { 
                    Duration = this.MenuAnimator.Duration,
                    Steps = this.MenuAnimator.Steps,
                    Type = this.MenuAnimator.Type
                },
                AutoWidth = this.AutoWidth,
            };
        }
    }
}
