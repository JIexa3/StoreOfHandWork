using System;
using System.Windows;
using System.Windows.Controls;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Вспомогательный класс для общих методов работы со страницами
    /// </summary>
    public static class PageHelpers
    {
        /// <summary>
        /// Проверяет, что все элементы управления инициализированы
        /// </summary>
        /// <param name="controls">Элементы управления для проверки</param>
        /// <returns>true, если все элементы не null</returns>
        public static bool IsControlInitialized(params object[] controls)
        {
            foreach (var control in controls)
            {
                if (control == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Безопасный способ установки свойства Visibility элемента управления
        /// </summary>
        public static void SafeSetVisibility(FrameworkElement element, Visibility visibility)
        {
            if (element != null)
                element.Visibility = visibility;
        }

        /// <summary>
        /// Безопасный способ получения контрола по имени из Page или Window
        /// </summary>
        public static T? FindControl<T>(FrameworkElement parent, string name) where T : FrameworkElement
        {
            try
            {
                var control = parent.FindName(name);
                return control as T;
            }
            catch
            {
                return null;
            }
        }
    }
}
