using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Класс окна для ввода текстовых данных
    /// </summary>
    public class TextInputWindow : Window
    {
        private TextBox inputTextBox;
        public string InputText { get; private set; } = string.Empty;
        public bool IsConfirmed { get; private set; } = false;
        
        public TextInputWindow(string prompt)
        {
            // Настройка окна
            Title = "Ввод данных";
            Width = 350;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            
            // Создание элементов интерфейса
            Grid mainGrid = new Grid { Margin = new Thickness(15) };
            Content = mainGrid;
            
            // Настройка строк сетки
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Текст запроса
            TextBlock promptTextBlock = new TextBlock
            {
                Text = prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(promptTextBlock, 0);
            mainGrid.Children.Add(promptTextBlock);
            
            // Поле ввода
            inputTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(inputTextBox, 1);
            mainGrid.Children.Add(inputTextBox);
            
            // Кнопки
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonsPanel, 2);
            mainGrid.Children.Add(buttonsPanel);
            
            Button cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                IsCancel = true
            };
            cancelButton.Click += CancelButton_Click;
            buttonsPanel.Children.Add(cancelButton);
            
            Button okButton = new Button
            {
                Content = "OK",
                Width = 100,
                IsDefault = true
            };
            okButton.Click += OkButton_Click;
            buttonsPanel.Children.Add(okButton);
            
            // Изменение заголовка окна, если передан промпт
            if (!string.IsNullOrEmpty(prompt))
            {
                Title = "Ввод данных: " + (prompt.Length > 30 ? prompt.Substring(0, 30) + "..." : prompt);
            }
            
            // Фокус на поле ввода при загрузке
            Loaded += (s, e) => inputTextBox.Focus();
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = inputTextBox.Text;
            IsConfirmed = true;
            
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
