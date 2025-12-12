using AcrylicViews.Model;
using AcrylicViews.Utils;
using AcrylicViews.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AcrylicViews.Controls
{
    internal partial class AcrylicMenuControl : Control
    {
        #region Props
        public int MinWidth { get; private set; }

        private Padding _textMargin;
        public Padding TextMargin
        {
            get => _textMargin;
            set
            {
                if (_textMargin != value)
                {
                    _textMargin = value;
                    OnPropertyChanged();
                }
            }
        }

        private Padding _rectMargins;
        public Padding RectMargins
        {
            get => _rectMargins;
            set
            {
                if (_rectMargins != value)
                {
                    _rectMargins = value;
                    OnPropertyChanged();
                }
            }
        }

        private Padding _arrowMargin;
        public Padding ArrowMargin
        {
            get => _arrowMargin;
            set
            {
                if (_arrowMargin != value)
                {
                    _arrowMargin = value;
                    OnPropertyChanged();
                }
            }
        }

        private Color _mouseEnterColor;
        public Color MouseEnterColor
        {
            get => _mouseEnterColor;
            set
            {
                if (_mouseEnterColor != value)
                {
                    _mouseEnterColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Color _mouseLeaveColor;
        public Color MouseLeaveColor
        {
            get => _mouseLeaveColor;
            set
            {
                if (_mouseLeaveColor != value)
                {
                    _mouseLeaveColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Color _mouseClickColor;
        public Color MouseClickColor
        {
            get => _mouseClickColor;
            set
            {
                if (_mouseClickColor != value)
                {
                    _mouseClickColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Color _mouseDownColor;
        public Color MouseDownColor
        {
            get => _mouseDownColor;
            set
            {
                if (_mouseDownColor != value)
                {
                    _mouseDownColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _checkMarkSize;
        public float CheckMarkSize
        {
            get => _checkMarkSize;
            set
            {
                if (Math.Abs(_checkMarkSize - value) > 0.001f)
                {
                    _checkMarkSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _arrowSize;
        public float ArrowSize
        {
            get => _arrowSize;
            set
            {
                if (Math.Abs(_arrowSize - value) > 0.001f)
                {
                    _arrowSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _closeAfterClick;
        public bool CloseAfterClick
        {
            get => _closeAfterClick;
            set
            {
                if (_closeAfterClick != value)
                {
                    _closeAfterClick = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _checked;
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    OnPropertyChanged();
                }
            }
        }

        public DropDownManager DropDown;

        private List<AcrylicMenuItem> dropDownItems;
        public List<AcrylicMenuItem> DropDownItems
        {
            get => dropDownItems;
            set
            {
                dropDownItems = value;
                DropDown = new DropDownManager(menu, this);
                DropDown.OnDropDownHidden = OnDropDownHidden;
                DropDown.OnDropDownShown = OnDropDownShown;
                DropDown.DropDownItems = value;
            }
        }
        #endregion

        private ContextMenuView menu;
        private Color currentColor;
        protected bool isDisposed = false;
        public Func<Task> OnPaintCompletedAsync;

        public AcrylicMenuControl(ContextMenuView menu)
        {
            this.menu = menu;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            BackColor = Colors.Transparent;
            currentColor = MouseLeaveColor;

            SizeChanged += OnSizedChanged;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseClick += OnMouseClick;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
        }

        #region Events

        private void OnSizedChanged(object sender, EventArgs e)
        {
            if (isDisposed) return;
            Invalidate();
        }

        private async void OnMouseEnter(object sender, EventArgs e)
        {
            if (isDisposed) return;
            currentColor = MouseEnterColor;
            Invalidate();
            try
            {
                await DropDown.Show();
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("(OnMouseEnter) Была попытка доступа к уничтоженному объекту!");
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (isDisposed) return;
            currentColor = MouseDownColor;
            Invalidate();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (isDisposed) return;
            currentColor = MouseLeaveColor;
            Invalidate();
            try
            {
                DropDown.CancelHoverTask();
            }
            catch (ObjectDisposedException) { }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (isDisposed) return;
            if (CloseAfterClick)
            {
                menu.Close();
            }
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (isDisposed) return;
            currentColor = MouseClickColor;
            Invalidate();
        }

        private void OnDropDownShown()
        {
        }

        private void OnDropDownHidden()
        {
            currentColor = MouseLeaveColor;
            Invalidate();
        }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            if (isDisposed) return;

            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            CustomPaint.DrawRoundedRect(g, Size, currentColor, RectMargins, Constants.DEFAULT_CORNER_RADIUS);

            if (Checked)
            {
                CustomPaint.DrawCheckMark(g, ForeColor, CheckMarkSize, TextMargin, Size);
            }

            if (DropDownItems != null && DropDownItems.Count != 0)
            {
                CustomPaint.DrawArrow(g, ForeColor, ArrowSize, ArrowMargin, Size);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                CustomPaint.DrawText(g, Text, Font, ForeColor, TextMargin, Size);
            }

            if (menu.AutoWidth)
            {
                SizeF textSize = g.MeasureString(this.Text ?? "", this.Font);
                float totalWidth = textSize.Width + TextMargin.Left + TextMargin.Right;
                int textWidth = (int)Math.Ceiling(totalWidth);
                MinWidth = textWidth;
            }

            if (MinWidth != 0)
            {
                if (!isDisposed)
                {
                    var handler = OnPaintCompletedAsync;
                    if (handler != null) _ = handler().ContinueWith(t => {
                        if (t.Exception != null) Debug.WriteLine(t.Exception);
                    }, TaskScheduler.Default);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                isDisposed = true;
                try
                {
                    DropDown.CancelHoverTask();
                    DropDown.Hide();
                }
                catch (ObjectDisposedException) { }
            }
            base.Dispose(disposing);
        }

        protected void OnPropertyChanged()
        {
            Invalidate();
        }
    }
}
