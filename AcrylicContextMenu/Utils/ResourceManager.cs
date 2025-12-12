using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AcrylicViews.Utils
{
    internal static class ResourceManager
    {
        private static readonly List<IDisposable> _resources = new List<IDisposable>();
        private static readonly object _lock = new object();

        public static void RegisterResource(IDisposable resource)
        {
            if (resource == null) return;

            lock (_lock)
            {
                _resources.Add(resource);
            }
        }

        public static void UnregisterResource(IDisposable resource)
        {
            if (resource == null) return;

            lock (_lock)
            {
                _resources.Remove(resource);
            }
        }

        public static void DisposeAll()
        {
            lock (_lock)
            {
                foreach (var resource in _resources)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing resource: {ex.Message}");
                    }
                }
                _resources.Clear();
            }
        }

        public static void CleanupUnusedResources()
        {
            lock (_lock)
            {
                _resources.RemoveAll(resource =>
                {
                    try
                    {
                        if (resource is Control control)
                        {
                            return control.IsDisposed;
                        }
                        return false;
                    }
                    catch
                    {
                        return true; // Удаляем ресурс, если не можем проверить его состояние
                    }
                });
            }
        }
    }
} 