using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StoreOfHandWork.Pages
{
    /// <summary>
    /// Класс окна для ввода номера карты с маской XXXX XXXX XXXX XXXX
    /// </summary>
    public class CardInputWindow : Window
    {
        private TextBox cardNumberTextBox;
        public string CardNumber { get; private set; } = string.Empty;
        public bool IsConfirmed { get; private set; } = false;
        
        public CardInputWindow(string prompt)
        {
            // Настройка окна
            Title = "Ввод данных карты";
            Width = 400;
            Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            
            // Создание элементов интерфейса
            Grid mainGrid = new Grid { Margin = new Thickness(15) };
            Content = mainGrid;
            
            // Настройка строк сетки
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Текст запроса
            TextBlock promptTextBlock = new TextBlock
            {
                Text = prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(promptTextBlock, 0);
            mainGrid.Children.Add(promptTextBlock);
            
            // Поле ввода с маской
            cardNumberTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                MaxLength = 19
            };
            cardNumberTextBox.PreviewTextInput += CardNumberTextBox_PreviewTextInput;
            cardNumberTextBox.TextChanged += CardNumberTextBox_TextChanged;
            Grid.SetRow(cardNumberTextBox, 1);
            mainGrid.Children.Add(cardNumberTextBox);
            
            // Подсказка о формате
            TextBlock formatHintTextBlock = new TextBlock
            {
                Text = "Формат: XXXX XXXX XXXX XXXX",
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(formatHintTextBlock, 2);
            mainGrid.Children.Add(formatHintTextBlock);
            
            // Кнопки
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonsPanel, 3);
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
            
            // Фокус на поле ввода при загрузке
            Loaded += (s, e) => cardNumberTextBox.Focus();
        }
        
        /// <summary>
        /// Проверка ввода только цифр
        /// </summary>
        private void CardNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }
        
        /// <summary>
        /// Форматирование ввода карты в формате XXXX XXXX XXXX XXXX
        /// </summary>
        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cardNumberTextBox.Text.Length == 0) return;
            
            // Удаляем все пробелы и другие символы кроме цифр
            string text = Regex.Replace(cardNumberTextBox.Text, @"[^\d]", "");
            
            // Форматируем текст, добавляя пробелы после каждой группы из 4 цифр
            string formattedText = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formattedText += " ";
                
                formattedText += text[i];
            }
            
            // Если форматированный текст отличается от текущего, обновляем его
            if (cardNumberTextBox.Text != formattedText)
            {
                int caretPosition = cardNumberTextBox.SelectionStart;
                
                // Если добавлен пробел, корректируем позицию каретки
                if (formattedText.Length > cardNumberTextBox.Text.Length)
                    caretPosition++;
                
                cardNumberTextBox.Text = formattedText;
                cardNumberTextBox.SelectionStart = Math.Min(caretPosition, formattedText.Length);
            }
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки OK
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на корректность введенного номера
            string cleanCardNumber = Regex.Replace(cardNumberTextBox.Text, @"\s+", "");
            
            // Проверяем длину номера карты (обычно 16 цифр)
            if (cleanCardNumber.Length < 16)
            {
                MessageBox.Show("Пожалуйста, введите полный номер карты (16 цифр)", 
                              "Неверный формат", MessageBoxButton.OK, MessageBoxImage.Warning);
                cardNumberTextBox.Focus();
                return;
            }
            
            // Сохраняем номер карты и устанавливаем флаг подтверждения
            CardNumber = cardNumberTextBox.Text;
            IsConfirmed = true;
            
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки Отмена
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
