using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace AcrylicViews.Utils
{
    public enum AnimationType {
        /// <summary>
        /// Медленно → быстро
        /// </summary>
        EaseOutQuad,
        /// <summary>
        /// Быстро → медленно
        /// </summary>
        EaseInQuad,
        /// <summary>
        /// Плавно с обеих сторон
        /// </summary>
        EaseInOut,
        /// <summary>
        /// Ещё более плавная кривая
        /// </summary>
        EaseOutCubic,
        /// <summary>
        /// Без анимации
        /// </summary>
        None
    }

    public class MenuAnimator
    {
        public int Duration { get; set; } = 20; // мс

        public int Steps { get; set; } = 10;
        
        public AnimationType Type { get; set; } =  AnimationType.EaseOutCubic;

        private int _targetHeight;

        private Point _targetLocation;

        public void PrepareForShow(Form form)
        {
            _targetHeight = form.Height;
            _targetLocation = form.Location;

            // Начальное состояние — внизу, невидимо
            form.Opacity = 0;
            form.Height = 0;
            form.Location = new Point(_targetLocation.X, _targetLocation.Y + _targetHeight);
        }

        internal async Task ShowAnimated(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            if (Type == AnimationType.None)
            {
                form.Opacity = 0;
                _targetHeight = form.Height;
                _targetLocation = form.Location;
                form.Show();
                form.Height = _targetHeight;
                form.Location = _targetLocation;
                await Task.Delay(50);
                form.Opacity = 1;
                return;
            }

            try
            {
                PrepareForShow(form);
                form.Show();

                await Animate(0, 1, value =>
                {
                    int h = (int)(_targetHeight * value);
                    form.Height = h;
                    form.Opacity = value;
                    form.Location = new Point(_targetLocation.X, _targetLocation.Y + _targetHeight - h);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DropDownManager.Anim] ❌ Неизвестное исключение: {ex}");
            }
        }

        internal async Task HideAnimated(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            if (Type == AnimationType.None)
            {
                // Без анимации — просто скрываем
                form.Hide();
                return;
            }

            try
            {
                await Animate(1, 0, value =>
                {
                    int h = (int)(_targetHeight * value);
                    form.Height = h;
                    form.Opacity = value;
                    form.Location = new Point(_targetLocation.X, _targetLocation.Y + _targetHeight - h);
                });

                form.Hide();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MenuAnimator.HideAnimated] ❌ {ex}");
            }
        }

        private async Task Animate(double from, double to, Action<double> apply)
        {
            for (int i = 0; i <= Steps; i++)
            {
                double t = i / (double)Steps;
                double eased = EasingFunction(t);
                double value = from + (to - from) * eased;

                apply(value);

                if (Duration > 0 && Steps > 0)
                    await Task.Delay(Duration / Steps);
            }
        }

        private double EasingFunction(double t)
        {
            switch (Type)
            {
                case AnimationType.EaseOutQuad:
                    return 1 - (1 - t) * (1 - t);

                case AnimationType.EaseInQuad:
                    return t * t;

                case AnimationType.EaseInOut:
                    return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;

                case AnimationType.EaseOutCubic:
                    return 1 - Math.Pow(1 - t, 3);

                case AnimationType.None:
                    return 1;

                default:
                    return t;
            }
        }
    }

}
