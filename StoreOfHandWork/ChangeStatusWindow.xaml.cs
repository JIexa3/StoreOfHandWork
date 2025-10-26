using System;
using System.Collections.Generic;
using System.Windows;
using StoreOfHandWork.Models;

namespace StoreOfHandWork
{
    public partial class ChangeStatusWindow : Window
    {
        public OrderStatus SelectedStatus { get; private set; }

        public ChangeStatusWindow(OrderStatus currentStatus)
        {
            InitializeComponent();

            var statuses = new List<StatusItem>();
            foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
            {
                statuses.Add(new StatusItem { Status = status, DisplayName = GetStatusDisplayName(status) });
            }

            StatusComboBox.ItemsSource = statuses;
            StatusComboBox.DisplayMemberPath = "DisplayName";
            StatusComboBox.SelectedValuePath = "Status";
            StatusComboBox.SelectedValue = currentStatus;
        }

        private string GetStatusDisplayName(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Новый:
                    return "Новый";
                case OrderStatus.ВОбработке:
                    return "В обработке";
                case OrderStatus.Отправлен:
                    return "Отправлен";
                case OrderStatus.Доставлен:
                    return "Доставлен";
                case OrderStatus.Отменен:
                    return "Отменен";
                default:
                    return status.ToString();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (StatusComboBox.SelectedValue is OrderStatus status)
            {
                SelectedStatus = status;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    public class StatusItem
    {
        public OrderStatus Status { get; set; }
        public string DisplayName { get; set; }
    }
}
