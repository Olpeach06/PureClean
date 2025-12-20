using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class OrdersManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<OrderViewModel> _allOrders = new List<OrderViewModel>();

        public OrdersManagementPage()
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
                // Сначала загружаем данные в память, затем преобразуем
                var orders = _context.Orders
                    .OrderByDescending(o => o.AcceptanceDate)
                    .ToList()  // Загружаем в память
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderID,
                        ClientName = o.Clients.FirstName + " " + o.Clients.LastName,
                        ClientPhone = o.Clients.Phone,
                        EmployeeName = o.Users.FirstName + " " + o.Users.LastName,
                        AcceptanceDate = o.AcceptanceDate.HasValue ? o.AcceptanceDate.Value.ToString("dd.MM.yyyy") : "",
                        PlannedDate = o.PlannedReturnDate.ToString("dd.MM.yyyy"),
                        ActualDate = o.ActualReturnDate.HasValue ? o.ActualReturnDate.Value.ToString("dd.MM.yyyy") : "",
                        Status = o.Status,
                        TotalAmount = o.TotalAmount ?? 0,
                        Prepayment = o.Prepayment ?? 0,
                        Comment = o.Comment
                    })
                    .ToList();

                _allOrders = orders;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allOrders == null || !_allOrders.Any())
                {
                    ordersGrid.ItemsSource = new List<OrderViewModel>();
                    return;
                }

                var filtered = _allOrders.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(o =>
                        o.ClientName.ToLower().Contains(searchText) ||
                        o.ClientPhone.ToLower().Contains(searchText) ||
                        o.EmployeeName.ToLower().Contains(searchText) ||
                        o.OrderId.ToString().Contains(searchText));
                }

                // Фильтр по статусу
                var selectedStatusItem = cmbStatus.SelectedItem as ComboBoxItem;
                if (selectedStatusItem != null && selectedStatusItem.Tag != null && !string.IsNullOrEmpty(selectedStatusItem.Tag.ToString()))
                {
                    string selectedStatus = selectedStatusItem.Tag.ToString();
                    filtered = filtered.Where(o => o.Status == selectedStatus);
                }

                // Фильтр по дате от
                if (dpFromDate.SelectedDate.HasValue)
                {
                    DateTime fromDate = dpFromDate.SelectedDate.Value;
                    filtered = filtered.Where(o =>
                    {
                        if (DateTime.TryParse(o.AcceptanceDate, out DateTime orderDate))
                        {
                            return orderDate.Date >= fromDate.Date;
                        }
                        return false;
                    });
                }

                // Фильтр по дате до
                if (dpToDate.SelectedDate.HasValue)
                {
                    DateTime toDate = dpToDate.SelectedDate.Value;
                    filtered = filtered.Where(o =>
                    {
                        if (DateTime.TryParse(o.AcceptanceDate, out DateTime orderDate))
                        {
                            return orderDate.Date <= toDate.Date;
                        }
                        return false;
                    });
                }

                ordersGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allOrders == null)
            {
                txtTotalCount.Text = "0";
                txtTotalAmount.Text = "0 ₽";
                txtInProgressCount.Text = "0";
                txtReadyCount.Text = "0";
                return;
            }

            txtTotalCount.Text = _allOrders.Count.ToString();

            decimal totalAmount = _allOrders.Sum(o => o.TotalAmount);
            txtTotalAmount.Text = $"{totalAmount:N0} ₽";

            int inProgressCount = _allOrders.Count(o => o.Status == "В работе" || o.Status == "Принят");
            txtInProgressCount.Text = inProgressCount.ToString();

            int readyCount = _allOrders.Count(o => o.Status == "Готов" || o.Status == "Выдан");
            txtReadyCount.Text = readyCount.ToString();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int orderId = Convert.ToInt32(button.Tag);

                // Переход на страницу деталей заказа
                NavigationService.Navigate(new OrderDetailsPage(orderId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int orderId = Convert.ToInt32(button.Tag);

                // Используем новое окно для редактирования статуса
                var window = new EditOrderStatusWindow(orderId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Статус заказа успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаляем старый метод ChangeOrderStatus или заменяем им

        

        private void btnApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbStatus.SelectedIndex = 0;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            ApplyFilters();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ordersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику при выборе заказа
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения заказов
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string ClientName { get; set; }
            public string ClientPhone { get; set; }
            public string EmployeeName { get; set; }
            public string AcceptanceDate { get; set; }
            public string PlannedDate { get; set; }
            public string ActualDate { get; set; }
            public string Status { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal Prepayment { get; set; }
            public string Comment { get; set; }

            public string Amount => $"{TotalAmount:N0} ₽";
        }
    }
}