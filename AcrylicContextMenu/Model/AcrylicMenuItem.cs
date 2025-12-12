using AcrylicViews.Controls;
using AcrylicViews.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AcrylicViews.Model
{
    public class AcrylicMenuItem : INotifyPropertyChanged
    {

        private string text = "Item";
        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        private bool _checked = false;
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    OnPropertyChanged(nameof(Checked));
                }
            }
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged(nameof(Enabled));
                }
            }
        }

        private Size size = new Size(Constants.DEFAULT_ITEM_WIDTH, Constants.DEFAULT_ITEM_HEIGHT);
        public Size Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        private float checkMarkSize = Constants.DEFAULT_CHECK_MARK_SIZE;
        public float CheckMarkSize
        {
            get => checkMarkSize;
            set
            {
                if (checkMarkSize != value)
                {
                    checkMarkSize = value;
                    OnPropertyChanged(nameof(CheckMarkSize));
                }
            }
        }

        private float arrowSize = Constants.DEFAULT_ARROW_SIZE;
        public float ArrowSize
        {
            get => arrowSize;
            set
            {
                if (arrowSize != value)
                {
                    arrowSize = value;
                    OnPropertyChanged(nameof(ArrowSize));
                }
            }
        }

        private Padding rectMargins = new Padding(Constants.DEFAULT_RECT_MARGIN_LEFT, Constants.DEFAULT_RECT_MARGIN_TOP,
            Constants.DEFAULT_RECT_MARGIN_RIGHT, Constants.DEFAULT_RECT_MARGIN_BOTTOM);

        public Padding RectMargin
        {
            get => rectMargins;
            set
            {
                if (rectMargins != value)
                {
                    rectMargins = value;
                    OnPropertyChanged(nameof(RectMargin));
                }
            }
        }

        private Padding textMargins = new Padding(Constants.DEFAULT_TEXT_MARGIN_LEFT, Constants.DEFAULT_TEXT_MARGIN_TOP,
            Constants.DEFAULT_TEXT_MARGIN_RIGHT, Constants.DEFAULT_TEXT_MARGIN_BOTTOM);

        public Padding TextMargin
        {
            get => textMargins;
            set
            {
                if (textMargins != value)
                {
                    textMargins = value;
                    OnPropertyChanged(nameof(TextMargin));
                }
            }
        }

        private Padding arrowMargins = new Padding(Constants.DEFAULT_ARROW_MARGIN_LEFT, Constants.DEFAULT_ARROW_MARGIN_TOP,
            Constants.DEFAULT_ARROW_MARGIN_RIGHT, Constants.DEFAULT_ARROW_MARGIN_BOTTOM);

        public Padding ArrowMargins
        {
            get => arrowMargins;
            set
            {
                if (arrowMargins != value)
                {
                    arrowMargins = value;
                    OnPropertyChanged(nameof(ArrowMargins));
                }
            }
        }

        private Color mouseEnterColor = Colors.GetAccentColor();
        public Color MouseEnterColor
        {
            get => mouseEnterColor;
            set
            {
                if (mouseEnterColor != value)
                {
                    mouseEnterColor = value;
                    OnPropertyChanged(nameof(MouseEnterColor));
                }
            }
        }

        private Color mouseLeaveColor = Colors.ControlNormal;
        public Color MouseLeaveColor
        {
            get => mouseLeaveColor;
            set
            {
                if (mouseLeaveColor != value)
                {
                    mouseLeaveColor = value;
                    OnPropertyChanged(nameof(MouseLeaveColor));
                }
            }
        }

        private Color mouseClickColor = Colors.GetAccentColor();
        public Color MouseClickColor
        {
            get => mouseClickColor;
            set
            {
                if (mouseClickColor != value)
                {
                    mouseClickColor = value;
                    OnPropertyChanged(nameof(MouseClickColor));
                }
            }
        }

        private Color mouseDownColor = Colors.GetAccentColor();
        public Color MouseDownColor
        {
            get => mouseDownColor;
            set
            {
                if (mouseDownColor != value)
                {
                    mouseDownColor = value;
                    OnPropertyChanged(nameof(MouseDownColor));
                }
            }
        }

        private Font font = new Font(Constants.DEFAULT_FONT_FAMILY, Constants.DEFAULT_FONT_SIZE, FontStyle.Regular);
        public Font Font
        {
            get => font;
            set
            {
                if (font != value)
                {
                    font = value;
                    OnPropertyChanged(nameof(Font));
                }
            }
        }

        private Color foreColor = Colors.Foreground;
        public Color ForeColor
        {
            get => foreColor;
            set
            {
                if (foreColor != value)
                {
                    foreColor = value;
                    OnPropertyChanged(nameof(ForeColor));
                }
            }
        }

        public List<AcrylicMenuItem> DropDownItems { get; set; } = new List<AcrylicMenuItem>();
        public Action<object, MouseEventArgs> MouseClick { get; set; }
        public Action<object, MouseEventArgs> MouseDown { get; set; }
        public bool CloseAfterClick { get; set; }

        internal AcrylicMenuControl control { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (control != null && control.InvokeRequired)
            {
                control.BeginInvoke(new Action(() =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
                ));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
