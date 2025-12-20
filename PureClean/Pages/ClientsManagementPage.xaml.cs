using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class ClientsManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<ClientViewModel> _allClients = new List<ClientViewModel>();

        public ClientsManagementPage()
        {
            InitializeComponent();
            Loaded += ClientsManagementPage_Loaded;
        }

        private void ClientsManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                // Сначала загружаем данные в память, затем преобразуем
                var clients = _context.Clients
                    .Include(c => c.Orders)
                    .ToList()  // Загружаем в память
                    .Select(c => new ClientViewModel
                    {
                        ClientID = c.ClientID,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        Phone = c.Phone,
                        Email = c.Email ?? "Не указан",
                        RegistrationDate = c.RegistrationDate.HasValue
                            ? c.RegistrationDate.Value.ToString("dd.MM.yyyy")
                            : "Не указана",
                        OrdersCount = c.Orders.Count,
                        TotalAmount = c.Orders.Sum(o => o.TotalAmount ?? 0)
                    })
                    .OrderByDescending(c => c.ClientID)  // Сортируем по ID (новые сверху)
                    .ToList();

                _allClients = clients;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allClients == null || !_allClients.Any())
                {
                    clientsGrid.ItemsSource = new List<ClientViewModel>();
                    return;
                }

                var filtered = _allClients.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(c =>
                        (c.LastName + " " + c.FirstName).ToLower().Contains(searchText) ||
                        c.Phone.ToLower().Contains(searchText) ||
                        c.Email.ToLower().Contains(searchText) ||
                        c.ClientID.ToString().Contains(searchText));
                }

                clientsGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allClients == null || !_allClients.Any())
            {
                txtTotalClients.Text = "0";
                txtTotalOrders.Text = "0";
                txtTotalAmount.Text = "0 ₽";
                return;
            }

            txtTotalClients.Text = _allClients.Count.ToString();
            txtTotalOrders.Text = _allClients.Sum(c => c.OrdersCount).ToString();

            decimal totalAmount = _allClients.Sum(c => c.TotalAmount);
            txtTotalAmount.Text = $"{totalAmount:N0} ₽";
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int clientId = Convert.ToInt32(button.Tag);

                // Получаем данные клиента
                var client = _context.Clients
                    .Include(c => c.Orders)
                    .FirstOrDefault(c => c.ClientID == clientId);

                if (client == null)
                {
                    MessageBox.Show("Клиент не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Форматируем дату после загрузки в память
                string registrationDate = client.RegistrationDate.HasValue
                    ? client.RegistrationDate.Value.ToString("dd.MM.yyyy")
                    : "Не указана";

                // Получаем дату последнего заказа
                var lastOrder = client.Orders
                    .OrderByDescending(o => o.AcceptanceDate)
                    .FirstOrDefault();

                string lastOrderDate = lastOrder?.AcceptanceDate?.ToString("dd.MM.yyyy") ?? "Нет заказов";

                // Собираем информацию о клиенте
                string clientInfo = $"📋 Информация о клиенте\n\n" +
                                   $"👤 ФИО: {client.LastName} {client.FirstName}\n" +
                                   $"📞 Телефон: {client.Phone}\n" +
                                   $"📧 Email: {client.Email ?? "Не указан"}\n" +
                                   $"📅 Дата регистрации: {registrationDate}\n\n" +
                                   $"📊 Статистика:\n" +
                                   $"• Всего заказов: {client.Orders.Count}\n" +
                                   $"• Общая сумма заказов: {client.Orders.Sum(o => o.TotalAmount ?? 0):N0} ₽\n" +
                                   $"• Последний заказ: {lastOrderDate}";

                MessageBox.Show(clientInfo, "Детали клиента",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

                int clientId = Convert.ToInt32(button.Tag);

                // Открываем окно редактирования клиента
                var editWindow = new EditClientWindow(clientId, _context);
                editWindow.Owner = Window.GetWindow(this);

                if (editWindow.ShowDialog() == true)
                {
                    // Пересоздаем контекст и перезагружаем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadClients();

                    MessageBox.Show("Данные клиента успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новый контекст для окна редактирования
                var localContext = new Entities();

                // Открываем окно добавления клиента
                var addWindow = new EditClientWindow(0, localContext); // 0 означает новый клиент
                addWindow.Owner = Window.GetWindow(this);
                addWindow.Title = "Добавление нового клиента";

                if (addWindow.ShowDialog() == true)
                {
                    // Закрываем локальный контекст
                    localContext.Dispose();

                    // Пересоздаем основной контекст и перезагружаем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadClients();

                    MessageBox.Show("Новый клиент успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Если диалог отменен, закрываем локальный контекст
                    localContext.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения клиентов
        public class ClientViewModel
        {
            public int ClientID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string RegistrationDate { get; set; }
            public int OrdersCount { get; set; }
            public decimal TotalAmount { get; set; }

            public string FullName => $"{LastName} {FirstName}";
            public string OrdersCountText => $"{OrdersCount}";
            public string TotalAmountText => $"{TotalAmount:N0} ₽";
        }
    }
}