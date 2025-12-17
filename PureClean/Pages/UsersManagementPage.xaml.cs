using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для UsersManagementPage.xaml
    /// </summary>
    public partial class UsersManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<UserViewModel> _allUsers = new List<UserViewModel>();
        private int _currentPage = 1;
        private const int PageSize = 10;

        // ViewModel для отображения пользователей
        public class UserViewModel
        {
            public int UserID { get; set; }
            public string Login { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Role { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public string Status { get; set; }
            public Brush StatusColor { get; set; }
            public bool IsActive { get; set; }
            public int RoleID { get; set; }
        }

        public UsersManagementPage()
        {
            InitializeComponent();
            LoadUsers();
            LoadRoleFilter();
        }

        private void LoadUsers()
        {
            try
            {
                // Загружаем пользователей из БД
                var users = _context.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .ToList();

                _allUsers = users.Select(u => new UserViewModel
                {
                    UserID = u.UserID,
                    Login = u.Login,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Phone = u.Phone ?? "", // Заменяем null на пустую строку
                    RoleID = u.RoleID,
                    Role = GetRoleName(u.RoleID),
                    RegistrationDate = u.RegistrationDate,
                    IsActive = u.IsActive ?? true,
                    Status = (u.IsActive ?? true) ? "Активен" : "Неактивен",
                    StatusColor = (u.IsActive ?? true) ?
                        new SolidColorBrush(Color.FromRgb(76, 175, 80)) : // Зеленый
                        new SolidColorBrush(Color.FromRgb(244, 67, 54))   // Красный
                }).ToList();

                ApplyFilters();
                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRoleName(int roleId)
        {
            try
            {
                var role = _context.Roles.FirstOrDefault(r => r.RoleID == roleId);
                return role?.Name ?? "Неизвестная роль";
            }
            catch
            {
                return "Неизвестная роль";
            }
        }

        private void LoadRoleFilter()
        {
            try
            {
                // Очищаем комбобокс
                cmbRoleFilter.Items.Clear();

                // Добавляем "Все роли"
                cmbRoleFilter.Items.Add(new ComboBoxItem { Content = "Все роли", Tag = 0 });

                // Загружаем роли из БД
                var roles = _context.Roles.ToList();
                foreach (var role in roles)
                {
                    cmbRoleFilter.Items.Add(new ComboBoxItem
                    {
                        Content = role.Name,
                        Tag = role.RoleID
                    });
                }

                cmbRoleFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredUsers = _allUsers.AsEnumerable();

                // Поиск по тексту
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    var searchText = txtSearch.Text.ToLower();
                    filteredUsers = filteredUsers.Where(u =>
                        (u.Login != null && u.Login.ToLower().Contains(searchText)) ||
                        (u.FullName != null && u.FullName.ToLower().Contains(searchText)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchText)) ||
                        (u.Phone != null && u.Phone.ToLower().Contains(searchText)));
                }

                // Фильтр по роли
                if (cmbRoleFilter.SelectedItem is ComboBoxItem selectedRole &&
                    selectedRole.Tag is int roleId && roleId > 0)
                {
                    filteredUsers = filteredUsers.Where(u => u.RoleID == roleId);
                }

                // Применение пагинации
                var totalItems = filteredUsers.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

                if (_currentPage > totalPages && totalPages > 0)
                    _currentPage = totalPages;
                else if (_currentPage < 1)
                    _currentPage = 1;

                var pagedUsers = filteredUsers
                    .Skip((_currentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                usersGrid.ItemsSource = pagedUsers;
                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            var totalItems = _allUsers.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            txtPageInfo.Text = $"Страница {_currentPage} из {totalPages}";
            txtTotalUsers.Text = $"Всего пользователей: {totalItems}";

            // Обновляем состояние кнопок
            btnPrevPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < totalPages;
        }

        // Обработчики событий
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilters();
        }

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _currentPage = 1;
                ApplyFilters();
            }
        }

        private void CmbRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilters();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyFilters();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            var totalItems = _allUsers.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            if (_currentPage < totalPages)
            {
                _currentPage++;
                ApplyFilters();
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addUserWindow = new AddEditUserWindow(null, _context);
                addUserWindow.Owner = Window.GetWindow(this);

                if (addUserWindow.ShowDialog() == true)
                {
                    // Перезагружаем пользователей после добавления
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is UserViewModel user)
                {
                    var editUserWindow = new AddEditUserWindow(user.UserID, _context);
                    editUserWindow.Owner = Window.GetWindow(this);

                    if (editUserWindow.ShowDialog() == true)
                    {
                        // Перезагружаем пользователей после редактирования
                        LoadUsers();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is UserViewModel user)
                {
                    var passwordWindow = new ChangePasswordWindow(user.UserID, _context);
                    passwordWindow.Owner = Window.GetWindow(this);

                    if (passwordWindow.ShowDialog() == true)
                    {
                        MessageBox.Show("Пароль успешно изменен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is UserViewModel user)
                {
                    // Проверяем, есть ли у пользователя заказы
                    bool hasOrders = CheckIfUserHasOrders(user.UserID);

                    if (hasOrders)
                    {
                        // У пользователя есть заказы - предлагаем деактивацию
                        var result = MessageBox.Show(
                            $"У пользователя {user.FullName} ({user.Login}) есть созданные заказы.\n\n" +
                            "Физическое удаление невозможно.\n\n" +
                            "Хотите деактивировать пользователя вместо удаления?",
                            "Невозможно удалить",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            DeactivateUser(user.UserID);
                        }
                    }
                    else
                    {
                        // У пользователя нет заказов - можно удалить
                        var result = MessageBox.Show(
                            $"Вы уверены, что хотите удалить пользователя {user.FullName} ({user.Login})?\n\n" +
                            "Это действие нельзя отменить!",
                            "Подтверждение удаления",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            DeleteUser(user.UserID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckIfUserHasOrders(int userId)
        {
            try
            {
                // Проверяем, есть ли заказы у пользователя
                return _context.Orders.Any(o => o.UserID == userId);
            }
            catch
            {
                // При ошибке предполагаем, что заказы есть (для безопасности)
                return true;
            }
        }

        private void DeleteUser(int userId)
        {
            try
            {
                var userToDelete = _context.Users.FirstOrDefault(u => u.UserID == userId);
                if (userToDelete != null)
                {
                    // Удаляем пользователя
                    _context.Users.Remove(userToDelete);
                    _context.SaveChanges();

                    MessageBox.Show("Пользователь успешно удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadUsers(); // Обновляем список
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}\n\n" +
                               "Попробуйте деактивировать пользователя вместо удаления.",
                               "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeactivateUser(int userId)
        {
            try
            {
                var userToDeactivate = _context.Users.FirstOrDefault(u => u.UserID == userId);
                if (userToDeactivate != null)
                {
                    userToDeactivate.IsActive = false;
                    _context.SaveChanges();

                    MessageBox.Show("Пользователь успешно деактивирован", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadUsers(); // Обновляем список
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка деактивации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is UserViewModel user)
                {
                    var newStatus = !user.IsActive;
                    var statusText = newStatus ? "активировать" : "деактивировать";

                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите {statusText} пользователя {user.FullName}?",
                        $"Подтверждение {(newStatus ? "активации" : "деактивации")}",
                        MessageBoxButton.YesNo,
                        newStatus ? MessageBoxImage.Question : MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        var userToUpdate = _context.Users.FirstOrDefault(u => u.UserID == user.UserID);
                        if (userToUpdate != null)
                        {
                            userToUpdate.IsActive = newStatus;
                            _context.SaveChanges();

                            MessageBox.Show($"Пользователь успешно {statusText}ован", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadUsers();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения статуса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        // Экспорт в Excel (демо)
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Экспорт в Excel (демо-режим)\n\n" +
                               "В реальном приложении здесь будет экспорт данных в Excel файл.",
                               "Экспорт данных",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}