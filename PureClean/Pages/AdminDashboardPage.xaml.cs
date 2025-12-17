using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminDashboardPage.xaml
    /// </summary>
    public partial class AdminDashboardPage : Page
    {
        private Entities _context = new Entities();

        // Модель для отображения активности
        public class ActivityLog
        {
            public string Time { get; set; }
            public string User { get; set; }
            public string Action { get; set; }
            public string Details { get; set; }
        }

        public AdminDashboardPage()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                // Загрузка статистики
                LoadStatistics();

                // Загрузка активности системы
                LoadActivityLogs();

                UpdateStatus("Данные загружены успешно", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки данных: {ex.Message}", true);
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                // 1. Всего пользователей (Users)
                int totalUsers = _context.Users.Count();
                txtUsersCount.Text = totalUsers.ToString();

                // 2. Всего заказов (Orders)
                int totalOrders = _context.Orders.Count();
                txtOrdersCount.Text = totalOrders.ToString();

                // 3. Общая выручка (TotalAmount из Orders)
                decimal? totalRevenue = _context.Orders
                    .Where(o => o.Status == "Выдан" || o.Status == "Готов") // Только завершенные заказы
                    .Sum(o => o.TotalAmount);

                txtRevenue.Text = totalRevenue?.ToString("N0") + " ₽" ?? "0 ₽";

                // 4. Услуг в каталоге (Services)
                int totalServices = _context.Services.Count();
                txtServicesCount.Text = totalServices.ToString();

                // Дополнительная статистика
                LoadAdditionalStats();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки статистики: {ex.Message}", true);
            }
        }

        private void LoadAdditionalStats()
        {
            try
            {
                // Статистика по статусам заказов
                var ordersByStatus = _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key ?? "Без статуса",
                        Count = g.Count()
                    })
                    .ToList();

                // Активные клиенты (сделавшие заказы за последний месяц)
                DateTime lastMonth = DateTime.Now.AddMonths(-1);
                int activeClients = _context.Orders
                    .Where(o => o.AcceptanceDate >= lastMonth)
                    .Select(o => o.ClientID)
                    .Distinct()
                    .Count();

                // Клиенты без регистрации (только из Orders)
                int totalClientsInOrders = _context.Orders
                    .Select(o => o.ClientID)
                    .Distinct()
                    .Count();

                // Клиенты в таблице Clients
                int totalRegisteredClients = _context.Clients.Count();

                // Средний чек
                decimal? averageOrderValue = _context.Orders
                    .Where(o => o.TotalAmount > 0)
                    .Average(o => (decimal?)o.TotalAmount);

                // Популярные услуги
                var popularServices = _context.OrderItems
                    .GroupBy(oi => oi.ServiceID)
                    .Select(g => new
                    {
                        ServiceID = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                // Можно вывести в дополнительную панель
                UpdateStatus($"Активных клиентов: {activeClients} | Средний чек: {averageOrderValue?.ToString("N0")} ₽", false);
            }
            catch (Exception ex)
            {
                // Подавляем ошибки дополнительной статистики
                Console.WriteLine($"Дополнительная статистика: {ex.Message}");
            }
        }

        private void LoadActivityLogs()
        {
            try
            {
                var activityLogs = new List<ActivityLog>();

                // Получаем последние заказы как активность
                var recentOrders = _context.Orders
                    .OrderByDescending(o => o.AcceptanceDate)
                    .Take(10)
                    .ToList();

                foreach (var order in recentOrders)
                {
                    try
                    {
                        // Получаем имя клиента
                        string clientName = "Клиент";
                        var client = _context.Clients.FirstOrDefault(c => c.ClientID == order.ClientID);
                        if (client != null)
                        {
                            clientName = $"{client.FirstName} {client.LastName}";
                        }

                        // Получаем имя пользователя (сотрудника)
                        string userName = "Сотрудник";
                        var user = _context.Users.FirstOrDefault(u => u.UserID == order.UserID);
                        if (user != null)
                        {
                            userName = $"{user.FirstName} {user.LastName}";
                        }

                        activityLogs.Add(new ActivityLog
                        {
                            Time = order.AcceptanceDate?.ToString("HH:mm") ?? DateTime.Now.ToString("HH:mm"),
                            User = userName,
                            Action = GetOrderAction(order.Status),
                            Details = $"Заказ №{order.OrderID} | Клиент: {clientName} | Сумма: {order.TotalAmount?.ToString("N0")} ₽"
                        });
                    }
                    catch
                    {
                        // Пропускаем ошибки отдельных записей
                    }
                }

                // Добавляем последние регистрации пользователей
                var recentUsers = _context.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .Take(5)
                    .ToList();

                foreach (var user in recentUsers)
                {
                    activityLogs.Add(new ActivityLog
                    {
                        Time = user.RegistrationDate?.ToString("HH:mm") ?? DateTime.Now.ToString("HH:mm"),
                        User = "Система",
                        Action = "Регистрация пользователя",
                        Details = $"{user.FirstName} {user.LastName} ({user.Login})"
                    });
                }

                // Сортируем по времени (новые сверху)
                activityLogs = activityLogs
                    .OrderByDescending(a => DateTime.ParseExact(a.Time, "HH:mm", null))
                    .Take(10)
                    .ToList();

                activityGrid.ItemsSource = activityLogs;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки логов: {ex.Message}", true);

                // Демо-данные при ошибке
                activityGrid.ItemsSource = GenerateDemoActivityLogs();
            }
        }

        private string GetOrderAction(string status)
        {
            switch (status)
            {
                case "Принят":
                    return "Новый заказ";
                case "В работе":
                    return "Заказ в работе";
                case "Готов":
                    return "Заказ готов";
                case "Выдан":
                    return "Заказ выдан";
                case "Отменен":
                    return "Заказ отменен";
                default:
                    return "Обновление заказа";
            }
        }

        private List<ActivityLog> GenerateDemoActivityLogs()
        {
            var logs = new List<ActivityLog>
            {
                new ActivityLog { Time = DateTime.Now.ToString("HH:mm"), User = "Админ", Action = "Вход в систему", Details = "Успешная аутентификация" },
                new ActivityLog { Time = DateTime.Now.AddMinutes(-5).ToString("HH:mm"), User = "Менеджер", Action = "Создание заказа", Details = "Заказ №101 создан" },
                new ActivityLog { Time = DateTime.Now.AddMinutes(-15).ToString("HH:mm"), User = "Клиент", Action = "Регистрация", Details = "Новый клиент: Иванов Иван" },
                new ActivityLog { Time = DateTime.Now.AddMinutes(-30).ToString("HH:mm"), User = "Админ", Action = "Обновление услуги", Details = "Услуга 'Химчистка' обновлена" },
                new ActivityLog { Time = DateTime.Now.AddHours(-1).ToString("HH:mm"), User = "Система", Action = "Обновление данных", Details = "Автоматическое обновление статистики" }
            };

            return logs;
        }

        // Обработчики кнопок
        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Переход к управлению пользователями", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Навигация на страницу пользователей
            NavigationService?.Navigate(new UsersManagementPage());
        }

        

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Выполнить резервное копирование базы данных?",
                    "Резервное копирование",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PerformDatabaseBackup();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка резервного копирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Показать детальные логи
                ShowDetailedLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PerformDatabaseBackup()
        {
            try
            {
                UpdateStatus("Выполняется резервное копирование...", false);

                // В реальном приложении здесь будет SQL команда для бэкапа
                // string backupCommand = $"BACKUP DATABASE PureCleanDB TO DISK = 'C:\\Backups\\PureClean_{DateTime.Now:yyyyMMdd_HHmmss}.bak'";
                // _context.Database.ExecuteSqlCommand(backupCommand);

                // Демо-режим
                string backupFileName = $"PureClean_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

                MessageBox.Show($"Резервная копия создана: {backupFileName}\n(демо-режим)", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateStatus("Резервное копирование завершено", false);

                // Обновляем логи
                LoadActivityLogs();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка резервного копирования: {ex.Message}", true);
                MessageBox.Show($"Не удалось создать резервную копию: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDetailedLogs()
        {
            try
            {
                // Собираем детальную статистику
                var detailedStats = new System.Text.StringBuilder();
                detailedStats.AppendLine("=== ДЕТАЛЬНАЯ СТАТИСТИКА СИСТЕМЫ ===");
                detailedStats.AppendLine($"Дата: {DateTime.Now}");
                detailedStats.AppendLine();

                // Пользователи по ролям
                var usersByRole = _context.Users
                    .GroupBy(u => u.RoleID)
                    .Select(g => new
                    {
                        RoleID = g.Key,
                        Count = g.Count()
                    })
                    .ToList();

                detailedStats.AppendLine("Пользователи по ролям:");
                foreach (var roleGroup in usersByRole)
                {
                    var roleName = _context.Roles
                        .Where(r => r.RoleID == roleGroup.RoleID)
                        .Select(r => r.Name)
                        .FirstOrDefault() ?? "Неизвестная роль";
                    detailedStats.AppendLine($"  {roleName}: {roleGroup.Count}");
                }
                detailedStats.AppendLine();

                // Заказы по статусам
                var ordersByStatus = _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Total = g.Sum(o => o.TotalAmount ?? 0)
                    })
                    .ToList();

                detailedStats.AppendLine("Заказы по статусам:");
                foreach (var statusGroup in ordersByStatus)
                {
                    detailedStats.AppendLine($"  {statusGroup.Status}: {statusGroup.Count} шт. ({statusGroup.Total:N0} ₽)");
                }
                detailedStats.AppendLine();

                // Популярные услуги
                var topServices = _context.OrderItems
                    .GroupBy(oi => oi.ServiceID)
                    .Select(g => new
                    {
                        ServiceID = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                detailedStats.AppendLine("Топ-5 услуг:");
                foreach (var service in topServices)
                {
                    var serviceName = _context.Services
                        .Where(s => s.ServiceID == service.ServiceID)
                        .Select(s => s.Name)
                        .FirstOrDefault() ?? "Неизвестная услуга";
                    detailedStats.AppendLine($"  {serviceName}: {service.Count} раз");
                }

                // Показываем в MessageBox
                MessageBox.Show(detailedStats.ToString(), "Детальные логи системы",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить детальную статистику: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message, bool isError)
        {
            if (statusBorder != null && statusText != null)
            {
                statusBorder.Visibility = Visibility.Visible;
                statusText.Text = message;
                statusText.Foreground = isError ? Brushes.Red : new SolidColorBrush(Color.FromRgb(102, 102, 102));

                // Автоматическое скрытие через 5 секунд
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    statusBorder.Visibility = Visibility.Collapsed;
                };
                timer.Start();
            }
        }

        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                LoadDashboardData();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void OrdersStatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Показать статистику заказов
                ShowOrdersStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowOrdersStatistics()
        {
            try
            {
                // Статистика за текущий месяц
                DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                var monthlyOrders = _context.Orders
                    .Where(o => o.AcceptanceDate >= startOfMonth)
                    .ToList();

                var completedOrders = monthlyOrders
                    .Where(o => o.Status == "Выдан" || o.Status == "Готов")
                    .ToList();

                var revenue = completedOrders.Sum(o => o.TotalAmount ?? 0);
                var avgOrderValue = completedOrders.Any()
                    ? completedOrders.Average(o => o.TotalAmount ?? 0)
                    : 0;

                string stats = $"СТАТИСТИКА ЗА ТЕКУЩИЙ МЕСЯЦ:\n\n" +
                               $"Всего заказов: {monthlyOrders.Count}\n" +
                               $"Завершенных: {completedOrders.Count}\n" +
                               $"Выручка: {revenue:N0} ₽\n" +
                               $"Средний чек: {avgOrderValue:N0} ₽\n\n" +
                               $"Необработанных заказов: {monthlyOrders.Count(o => o.Status == "Принят")}\n" +
                               $"В работе: {monthlyOrders.Count(o => o.Status == "В работе")}";

                MessageBox.Show(stats, "Статистика заказов",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить статистику: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}