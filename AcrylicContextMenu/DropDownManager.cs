using AcrylicViews.Model;
using AcrylicViews.Utils;
using AcrylicViews.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

internal class DropDownManager
{
    public List<AcrylicMenuItem> DropDownItems { get; set; } = new List<AcrylicMenuItem>();
    public bool DropDownVisible { get; private set; } = false;
    public Action OnDropDownShown { get; set; }
    public Action OnDropDownHidden { get; set; }

    private ContextMenuView dropDownMenu;
    private readonly ContextMenuView parentView;
    private readonly Control ownerControl;
    private Timer hoverTimer;
    private CancellationTokenSource hoverCts;

    public DropDownManager(ContextMenuView parentView, Control ownerControl)
    {
        this.parentView = parentView;
        this.ownerControl = ownerControl;
        Debug.WriteLine("[DropDownManager] Инициализирован для " + ownerControl?.Name);
    }

    public async Task Show()
    {
        Debug.WriteLine("[DropDownManager] Show() вызван");

        if (dropDownMenu != null && dropDownMenu.Visible)
        {
            Debug.WriteLine("[DropDownManager] Уже показан — выход");
            return;
        }

        if (DropDownItems == null || DropDownItems.Count == 0)
        {
            Debug.WriteLine("[DropDownManager] Нет DropDownItems — выход");
            return;
        }

        CancelHoverTask();
        hoverCts = new CancellationTokenSource();

        try
        {
            Debug.WriteLine("[DropDownManager] Задержка перед открытием меню...");
            await Task.Delay(Constants.HOVER_DELAY_MS, hoverCts.Token);
            Debug.WriteLine("[DropDownManager] Задержка завершена");
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[DropDownManager] Ожидание отменено (hoverCts)");
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка в Task.Delay: " + ex);
            return;
        }

        if (hoverCts == null || hoverCts.IsCancellationRequested)
        {
            Debug.WriteLine("[DropDownManager] CTS отменён — выход");
            return;
        }

        if (ownerControl == null || ownerControl.IsDisposed)
        {
            Debug.WriteLine("[DropDownManager] Контрол уничтожен — выход");
            return;
        }

        if (!ownerControl.IsHandleCreated)
        {
            Debug.WriteLine("[DropDownManager] Handle не создан — выход");
            return;
        }

        try
        {
            var localPoint = ownerControl.Parent.PointToClient(Cursor.Position);
            if (!ownerControl.Bounds.Contains(localPoint))
            {
                Debug.WriteLine("[DropDownManager] Курсор вне ownerControl — выход");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при проверке позиции курсора: " + ex);
            return;
        }

        try
        {
            Debug.WriteLine("[DropDownManager] Создание меню...");
            CreateDropDownMenu();
            Debug.WriteLine("[DropDownManager] dropDownMenu создан: " + dropDownMenu?.Name);

            dropDownMenu.Show();
            Debug.WriteLine("[DropDownManager] dropDownMenu.Show() выполнен");
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine("[DropDownManager] ObjectDisposedException в Show(): " + ex.ObjectName);
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при показе меню: " + ex);
            return;
        }

        DropDownVisible = true;
        Debug.WriteLine("[DropDownManager] DropDownVisible = true");

        if (hoverTimer == null)
        {
            InitializeHoverTimer();
        }

        try
        {
            hoverTimer?.Start();
            Debug.WriteLine("[DropDownManager] hoverTimer запущен");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при запуске hoverTimer: " + ex);
        }

        OnDropDownShown?.Invoke();
        Debug.WriteLine("[DropDownManager] OnDropDownShown вызван");
    }

    public void Hide()
    {
        Debug.WriteLine("[DropDownManager] Hide() вызван");

        if (dropDownMenu == null)
        {
            Debug.WriteLine("[DropDownManager] dropDownMenu == null — выход");
            return;
        }

        hoverTimer?.Stop();

        try
        {
            dropDownMenu.Hide();
            Debug.WriteLine("[DropDownManager] dropDownMenu.Hide() выполнен");

            dropDownMenu.Dispose();
            Debug.WriteLine("[DropDownManager] dropDownMenu.Dispose() выполнен");
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine("[DropDownManager] ObjectDisposedException в Hide(): " + ex.ObjectName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при Hide(): " + ex);
        }

        dropDownMenu = null;

        DropDownVisible = false;
        Debug.WriteLine("[DropDownManager] DropDownVisible = false");

        try
        {
            OnDropDownHidden?.Invoke();
            Debug.WriteLine("[DropDownManager] OnDropDownHidden вызван");

            parentView?.Activate();
            Debug.WriteLine("[DropDownManager] parentView.Activate() выполнен");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка в OnDropDownHidden или Activate: " + ex);
        }
    }

    private void CreateDropDownMenu()
    {
        Debug.WriteLine("[DropDownManager] CreateDropDownMenu() вызван");

        try
        {
            dropDownMenu?.Dispose();
            Debug.WriteLine("[DropDownManager] Старое меню удалено");

            dropDownMenu = parentView.Clone();
            dropDownMenu.DropDownButtonHeight = ownerControl.Height;
            dropDownMenu.DropDownLocation = ownerControl.Location;
            Debug.WriteLine("[DropDownManager] Новое меню клонировано");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при создании dropDownMenu: " + ex);
            return;
        }

        try
        {
            foreach (var item in DropDownItems)
            {
                var control = dropDownMenu.AddItem(item);
                item.control = control;
                Debug.WriteLine($"[DropDownManager] Добавлен элемент: {item.Text}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка при добавлении элементов: " + ex);
        }
    }

    private void InitializeHoverTimer()
    {
        Debug.WriteLine("[DropDownManager] Инициализация hoverTimer");
        hoverTimer = new Timer { Interval = Constants.HOVER_DELAY_MS };
        hoverTimer.Tick += HoverTimer_Tick;
    }

    public void CancelHoverTask()
    {
        Debug.WriteLine("[DropDownManager] CancelHoverTask() вызван");

        if (hoverCts != null && !hoverCts.IsCancellationRequested)
        {
            try
            {
                hoverCts.Cancel();
                hoverCts.Dispose();
                Debug.WriteLine("[DropDownManager] hoverCts отменён и удалён");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DropDownManager] Ошибка при отмене hoverCts: " + ex);
            }
        }

        hoverCts = null;
    }

    private void HoverTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            if (ownerControl == null || ownerControl.IsDisposed)
            {
                Debug.WriteLine("[DropDownManager] HoverTimer_Tick: ownerControl уничтожен");
                hoverTimer?.Stop();
                return;
            }

            Point cursorPos = Cursor.Position;
            bool overButton = ownerControl.Bounds.Contains(ownerControl.Parent.PointToClient(cursorPos));
            bool overDropDown = dropDownMenu?.Bounds.Contains(cursorPos) == true;

            if (!overButton && !overDropDown)
            {
                Debug.WriteLine("[DropDownManager] Курсор вне меню — закрываем");
                hoverTimer?.Stop();
                Hide();
            }
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine("[DropDownManager] ObjectDisposedException в HoverTimer_Tick: " + ex.ObjectName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DropDownManager] Ошибка в HoverTimer_Tick: " + ex);
        }
    }
}
