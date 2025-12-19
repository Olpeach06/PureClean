using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class MyOrdersPage : Page
    {
        private Entities _context = new Entities();
        private List<Orders> _orders = new List<Orders>();

        public MyOrdersPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                if (!Session.UserID.HasValue)
                {
                    MessageBox.Show("Для просмотра заказов необходимо авторизоваться",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService.Navigate(new LoginPage());
                    return;
                }

                _orders = _context.Orders
                    .Where(o => o.UserID == Session.UserID)
                    .OrderByDescending(o => o.AcceptanceDate)
                    .ToList();

                DisplayOrders(_orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayOrders(List<Orders> orders)
        {
            ordersPanel.Children.Clear();

            if (orders.Count == 0)
            {
                noOrdersPanel.Visibility = Visibility.Visible;
                txtOrdersCount.Text = "0 заказов";
                return;
            }

            noOrdersPanel.Visibility = Visibility.Collapsed;
            txtOrdersCount.Text = $"{orders.Count} заказ(ов)";

            foreach (var order in orders)
            {
                var orderCard = CreateOrderCard(order);
                ordersPanel.Children.Add(orderCard);
            }
        }

        private Border CreateOrderCard(Orders order)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0e0e0")),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // Левый блок: информация о заказе
            var leftStack = new StackPanel
            {
                Margin = new Thickness(15)
            };

            var orderNumber = new TextBlock
            {
                Text = $"Заказ №{order.OrderID}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            };

            var dateText = new TextBlock
            {
                Text = $"Дата: {order.AcceptanceDate:dd.MM.yyyy HH:mm}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var clientInfo = new TextBlock
            {
                Text = $"Клиент: {order.Clients?.FirstName} {order.Clients?.LastName}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                Margin = new Thickness(0, 2, 0, 0)
            };

            var commentText = new TextBlock
            {
                Text = $"Комментарий: {(string.IsNullOrEmpty(order.Comment) ? "нет" : order.Comment)}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            leftStack.Children.Add(orderNumber);
            leftStack.Children.Add(dateText);
            leftStack.Children.Add(clientInfo);
            leftStack.Children.Add(commentText);

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            // Центральный блок: статус
            var statusBorder = new Border
            {
                Background = GetStatusColor(order.Status),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 15, 10, 15),
                VerticalAlignment = VerticalAlignment.Center
            };

            var statusText = new TextBlock
            {
                Text = order.Status,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 10, 10, 10)
            };

            statusBorder.Child = statusText;
            Grid.SetColumn(statusBorder, 1);
            grid.Children.Add(statusBorder);

            // Правый блок: сумма заказа (без кнопки)
            var rightStack = new StackPanel
            {
                Margin = new Thickness(0, 15, 15, 15),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var amountText = new TextBlock
            {
                Text = $"Сумма: {order.TotalAmount:N2} ₽",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"))
            };

            rightStack.Children.Add(amountText);
            Grid.SetColumn(rightStack, 2);
            grid.Children.Add(rightStack);

            border.Child = grid;

            return border;
        }

        private Brush GetStatusColor(string status)
        {
            switch (status.ToLower())
            {
                case "принят":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                case "в работе":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                case "готов":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                case "выдан":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0"));
                case "отменен":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                default:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575"));
            }
        }

        // Метод для кнопки "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Логика фильтрации по статусу
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            DisplayOrders(_orders);
        }

        private void ApplyFilters()
        {
            var filteredOrders = _orders.AsQueryable();

            // Фильтр по статусу
            if (cmbStatusFilter.SelectedItem is ComboBoxItem selectedStatus && selectedStatus.Tag.ToString() != "all")
            {
                filteredOrders = filteredOrders.Where(o => o.Status == selectedStatus.Tag.ToString());
            }

            DisplayOrders(filteredOrders.ToList());
        }

        private void GoToCatalog_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CatalogPage());
        }
    }
}