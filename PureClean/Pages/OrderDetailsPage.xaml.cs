using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class OrderDetailsPage : Page
    {
        private Entities _context = new Entities();
        private int _orderId;

        public OrderDetailsPage()
        {
            InitializeComponent();
            // Обработка загрузки без orderId
            LoadDemoData();
        }

        public OrderDetailsPage(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем данные при загрузке страницы
            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            try
            {
                // Проверяем, есть ли orderId
                if (_orderId == 0)
                {
                    // Если перешли без ID, показываем демо-данные
                    LoadDemoData();
                    return;
                }

                var order = _context.Orders.FirstOrDefault(o => o.OrderID == _orderId);
                if (order == null)
                {
                    MessageBox.Show("Заказ не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadDemoData();
                    return;
                }

                // Загружаем реальные данные из БД
                LoadRealOrderData(order);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadDemoData();
            }
        }

        private void LoadRealOrderData(Orders order)
        {
            try
            {
                // Номер заказа и статус
                txtOrderNumber.Text = $"Заказ №{order.OrderID}";

                // Обновляем статус
                UpdateStatusDisplay(order.Status);

                // Даты
                txtAcceptanceDate.Text = order.AcceptanceDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана";
                txtPlannedDate.Text = order.PlannedReturnDate.ToString("dd.MM.yyyy HH:mm");
                txtActualDate.Text = order.ActualReturnDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не выдано";

                // Клиент
                txtClientName.Text = $"{order.Clients.FirstName} {order.Clients.LastName}";
                txtClientPhone.Text = order.Clients.Phone;
                txtClientEmail.Text = order.Clients.Email ?? "Не указан";

                // Сотрудник
                txtEmployeeName.Text = $"{order.Users.FirstName} {order.Users.LastName}";

                // Финансовая информация
                decimal totalAmount = order.TotalAmount ?? 0;
                decimal prepayment = order.Prepayment ?? 0;
                decimal remainder = totalAmount - prepayment;

                txtTotalAmount.Text = $"{totalAmount:N0} ₽";
                txtPrepayment.Text = $"{prepayment:N0} ₽";
                txtRemainder.Text = $"{remainder:N0} ₽";

                // Комментарий
                txtComment.Text = !string.IsNullOrEmpty(order.Comment) ? order.Comment : "Комментарий отсутствует";

                // Загрузка услуг в заказе
                LoadOrderItems(order.OrderID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки данных заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusDisplay(string status)
        {
            txtStatus.Text = status;

            switch (status)
            {
                case "Выдан":
                    statusBorder.Background = System.Windows.Media.Brushes.LightGreen;
                    statusBorder.BorderBrush = System.Windows.Media.Brushes.DarkGreen;
                    txtStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                    break;
                case "Готов":
                    statusBorder.Background = System.Windows.Media.Brushes.LightGoldenrodYellow;
                    statusBorder.BorderBrush = System.Windows.Media.Brushes.DarkOrange;
                    txtStatus.Foreground = System.Windows.Media.Brushes.DarkOrange;
                    break;
                case "В работе":
                    statusBorder.Background = System.Windows.Media.Brushes.LightBlue;
                    statusBorder.BorderBrush = System.Windows.Media.Brushes.Blue;
                    txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
                    break;
                case "Принят":
                    statusBorder.Background = System.Windows.Media.Brushes.Lavender;
                    statusBorder.BorderBrush = System.Windows.Media.Brushes.DarkViolet;
                    txtStatus.Foreground = System.Windows.Media.Brushes.DarkViolet;
                    break;
                case "Отменен":
                    statusBorder.Background = System.Windows.Media.Brushes.LightCoral;
                    statusBorder.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                    txtStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                    break;
            }
        }

        private void LoadOrderItems(int orderId)
        {
            try
            {
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderID == orderId)
                    .Select(oi => new OrderItemViewModel
                    {
                        Service = oi.Services.Name,
                        Quantity = oi.Quantity,
                        Price = oi.PriceAtOrder,
                        Total = oi.ItemTotal ?? oi.PriceAtOrder * oi.Quantity,
                        Comment = oi.Comment
                    })
                    .ToList();

                orderItemsGrid.ItemsSource = orderItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDemoData()
        {
            // Демо-данные для отображения при ошибке
            txtOrderNumber.Text = "Заказ № (не выбран)";
            UpdateStatusDisplay("Принят");

            txtClientName.Text = "Не выбран";
            txtClientPhone.Text = "Не указан";
            txtClientEmail.Text = "Не указан";
            txtEmployeeName.Text = "Не указан";

            txtAcceptanceDate.Text = "Не указана";
            txtPlannedDate.Text = "Не указана";
            txtActualDate.Text = "Не выдано";

            txtTotalAmount.Text = "0 ₽";
            txtPrepayment.Text = "0 ₽";
            txtRemainder.Text = "0 ₽";
            txtComment.Text = "Выберите заказ для просмотра деталей";

            // Очищаем список услуг
            orderItemsGrid.ItemsSource = null;
        }

        private void btnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            // Обработчик кнопки "Изменить статус"
            try
            {
                if (_orderId == 0)
                {
                    MessageBox.Show("Выберите заказ для изменения статуса!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var window = new EditOrderStatusWindow(_orderId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Статус заказа успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrderDetails(); // Перезагружаем данные
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // Логика печати
            MessageBox.Show("Функция печати находится в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения услуг в заказе
        public class OrderItemViewModel
        {
            public string Service { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total { get; set; }
            public string Comment { get; set; }
        }
    }
}