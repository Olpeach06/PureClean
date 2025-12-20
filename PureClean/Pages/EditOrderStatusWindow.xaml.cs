using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class EditOrderStatusWindow : Window
    {
        private Entities _context;
        private int _orderId;
        private Orders _order;

        public EditOrderStatusWindow(int orderId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _orderId = orderId;

            LoadOrderData();
        }

        private void LoadOrderData()
        {
            try
            {
                _order = _context.Orders.FirstOrDefault(o => o.OrderID == _orderId);
                if (_order != null)
                {
                    // Отображаем информацию о заказе
                    txtOrderInfo.Text = $"Заказ №{_order.OrderID}";
                    txtClientInfo.Text = $"Клиент: {_order.Clients.FirstName}   {_order.Clients.LastName}";
                    txtCurrentStatus.Text = $"Текущий статус: {_order.Status}";

                    txtComment.Text = _order.Comment;

                    // Устанавливаем текущий статус
                    SetCurrentStatus(_order.Status);
                }
                else
                {
                    ShowError("Заказ не найден");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных заказа: {ex.Message}");
            }
        }

        private void SetCurrentStatus(string status)
        {
            // Сбрасываем все радиокнопки
            rbAccepted.IsChecked = false;
            rbInProgress.IsChecked = false;
            rbReady.IsChecked = false;
            rbCompleted.IsChecked = false;
            rbCancelled.IsChecked = false;

            // Устанавливаем нужную радиокнопку
            switch (status)
            {
                case "Принят":
                    rbAccepted.IsChecked = true;
                    break;
                case "В работе":
                    rbInProgress.IsChecked = true;
                    break;
                case "Готов":
                    rbReady.IsChecked = true;
                    break;
                case "Выдан":
                    rbCompleted.IsChecked = true;
                    break;
                case "Отменен":
                    rbCancelled.IsChecked = true;
                    break;
            }
        }

        private string GetSelectedStatus()
        {
            if (rbAccepted.IsChecked == true) return "Принят";
            if (rbInProgress.IsChecked == true) return "В работе";
            if (rbReady.IsChecked == true) return "Готов";
            if (rbCompleted.IsChecked == true) return "Выдан";
            if (rbCancelled.IsChecked == true) return "Отменен";

            return "";
        }

        private void StatusRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Можно добавить дополнительную логику при выборе статуса
            // Например, показывать предупреждения при определенных переходах
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_order == null)
                {
                    ShowError("Заказ не найден");
                    return;
                }

                // Получаем выбранный статус
                string newStatus = GetSelectedStatus();

                // Сохраняем изменения
                _order.Status = newStatus;
                _order.Comment = txtComment.Text.Trim();

                // Если статус "Выдан", устанавливаем дату фактической выдачи
                if (newStatus == "Выдан" && !_order.ActualReturnDate.HasValue)
                {
                    _order.ActualReturnDate = DateTime.Now;
                }

                // Если статус "Отменен", сбрасываем дату фактической выдачи
                if (newStatus == "Отменен")
                {
                    _order.ActualReturnDate = null;
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            HideError();

            // Проверяем, выбран ли статус
            string selectedStatus = GetSelectedStatus();
            if (string.IsNullOrEmpty(selectedStatus))
            {
                ShowError("Выберите новый статус заказа");
                rbAccepted.Focus();
                return false;
            }

            // Дополнительные проверки можно добавить здесь
            // Например, проверка перехода с "Выдан" на "В работе" может быть запрещена

            return true;
        }

        private void ShowError(string message)
        {
            errorBorder.Visibility = Visibility.Visible;
            txtErrorMessage.Text = message;
        }

        private void HideError()
        {
            errorBorder.Visibility = Visibility.Collapsed;
            txtErrorMessage.Text = "";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на первом элементе
            rbAccepted.Focus();
        }
    }
}