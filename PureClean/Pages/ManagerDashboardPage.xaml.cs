using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для ManagerDashboardPage.xaml
    /// </summary>
    public partial class ManagerDashboardPage : Page
    {
        private Entities _context = new Entities();

        public ManagerDashboardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Показываем демо-данные при ошибке
                ShowDemoData();
            }
        }

        private void LoadDashboardData()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Статистика новых заказов (за текущий месяц)
            int newOrdersCount = _context.Orders
                .Count(o => o.AcceptanceDate >= startOfMonth && o.AcceptanceDate <= now);

            // Статистика заказов в работе
            int ordersInProgressCount = _context.Orders
                .Count(o => o.Status == "В работе" || o.Status == "Принят");

            // Выручка за месяц (только выданные заказы)
            var revenueQuery = _context.Orders
                .Where(o => o.AcceptanceDate >= startOfMonth &&
                           o.AcceptanceDate <= now &&
                           o.Status == "Выдан")
                .Select(o => o.TotalAmount);

            decimal revenue = 0;
            foreach (var amount in revenueQuery)
            {
                if (amount.HasValue)
                    revenue += amount.Value;
            }

            // Новые клиенты (за текущий месяц)
            int newClientsCount = _context.Clients
                .Count(c => c.RegistrationDate >= startOfMonth && c.RegistrationDate <= now);

            // Обновляем карточки статистики
            UpdateStatCards(newOrdersCount, ordersInProgressCount, revenue, newClientsCount);

            // Загружаем последние заказы
            LoadRecentOrders();
        }

        private void UpdateStatCards(int newOrders, int inProgress, decimal revenue, int newClients)
        {
            // Карточка 1: Новые заказы
            txtNewOrders.Text = newOrders.ToString();

            // Карточка 2: Заказы в работе
            txtOrdersInProgress.Text = inProgress.ToString();

            // Карточка 3: Выручка
            txtRevenue.Text = revenue.ToString("N0") + " ₽";

            // Карточка 4: Новые клиенты
            txtNewClients.Text = newClients.ToString();
        }

        private void LoadRecentOrders()
        {
            try
            {
                // Получаем последние 5 заказов
                var orders = _context.Orders
                    .OrderByDescending(o => o.AcceptanceDate)
                    .Take(5)
                    .ToList();

                var recentOrders = new List<OrderViewModel>();

                foreach (var order in orders)
                {
                    var client = _context.Clients.FirstOrDefault(c => c.ClientID == order.ClientID);
                    string clientName = client != null ? $"{client.FirstName} {client.LastName}" : "Неизвестный клиент";

                    string date = order.AcceptanceDate.HasValue
                        ? order.AcceptanceDate.Value.ToString("dd.MM.yyyy")
                        : "Не указана";

                    string amount = order.TotalAmount.HasValue
                        ? order.TotalAmount.Value.ToString("N0") + " ₽"
                        : "0 ₽";

                    recentOrders.Add(new OrderViewModel
                    {
                        OrderId = order.OrderID,
                        ClientName = clientName,
                        Date = date,
                        Amount = amount,
                        Status = order.Status
                    });
                }

                recentOrdersGrid.ItemsSource = recentOrders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                recentOrdersGrid.ItemsSource = GetDemoOrders();
            }
        }

        private void ShowDemoData()
        {
            UpdateStatCards(10, 5, 25400, 3);
            recentOrdersGrid.ItemsSource = GetDemoOrders();
        }

        private List<OrderViewModel> GetDemoOrders()
        {
            return new List<OrderViewModel>
            {
                new OrderViewModel { OrderId = 1001, ClientName = "Иванов Иван", Date = "18.12.2025", Amount = "2 500 ₽", Status = "Принят" },
                new OrderViewModel { OrderId = 1002, ClientName = "Петрова Анна", Date = "17.12.2025", Amount = "1 800 ₽", Status = "В работе" },
                new OrderViewModel { OrderId = 1003, ClientName = "Сидоров Петр", Date = "16.12.2025", Amount = "3 200 ₽", Status = "Готов" },
                new OrderViewModel { OrderId = 1004, ClientName = "Козлова Ольга", Date = "15.12.2025", Amount = "950 ₽", Status = "Выдан" },
                new OrderViewModel { OrderId = 1005, ClientName = "Васильев Сергей", Date = "14.12.2025", Amount = "4 500 ₽", Status = "Принят" }
            };
        }

        // Существующие методы навигации
        private void GoToServices(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new ServicesManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToOrders(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new OrdersManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToClients(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new ClientsManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToMaterials(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new MaterialsManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Новые методы навигации
        private void GoToServiceCategories(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new ServiceCategoriesManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToItemTypes(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new ItemTypesManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToSuppliers(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new SuppliersManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToReviews(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new ReviewsManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToMaterialSupplies(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new MaterialSuppliesPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToMaterialUsages(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new MaterialUsagesPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void GoToActiveCarts(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        // Используем представление vw_CartTotals
        //        NavigationService.Navigate(new GenericTableViewPage());
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        // Вспомогательный класс для отображения заказов
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string ClientName { get; set; }
            public string Date { get; set; }
            public string Amount { get; set; }
            public string Status { get; set; }
        }

        // Освобождаем контекст при закрытии страницы
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }
    }
}