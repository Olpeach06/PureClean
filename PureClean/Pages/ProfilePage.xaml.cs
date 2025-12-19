using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PureClean.AppData;
using System.Security.Cryptography;
using System.Text;

namespace PureClean.Pages
{
    public partial class ProfilePage : Page
    {
        private Entities _context = new Entities();

        public ProfilePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Session.IsGuest)
                {
                    MessageBox.Show("Для просмотра профиля необходимо авторизоваться!",
                        "Требуется авторизация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    NavigationService.Navigate(new LoginPage());
                    return;
                }

                LoadUserData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            try
            {
                if (!Session.UserID.HasValue)
                {
                    ShowGuestData();
                    return;
                }

                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user == null)
                {
                    ShowGuestData();
                    return;
                }

                // Обновляем данные НЕМЕДЛЕННО через прямое присваивание
                txtUserName.Text = $"{user.FirstName} {user.LastName}";
                txtFullName.Text = $"{user.FirstName} {user.LastName}";
                txtEmail.Text = user.Email ?? "Не указан";
                txtPhone.Text = user.Phone ?? "Не указан";
                txtRegistrationDate.Text = user.RegistrationDate?.ToString("dd.MM.yyyy") ?? "Не указана";

                // Роль
                string roleName = GetRoleName(user.RoleID);
                txtRole.Text = roleName;
                txtStatus.Text = user.IsActive == true ? "Активен" : "Не активен";

                btnShowEdit.Visibility = Visibility.Visible;
                btnChangePassword.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowGuestData();
            }
        }

        private string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1: return "Гость";
                case 2: return "Пользователь";
                case 3: return "Менеджер";
                case 4: return "Администратор";
                default: return "Пользователь";
            }
        }

        private void ShowGuestData()
        {
            txtUserName.Text = "Гость";
            txtFullName.Text = "Гость";
            txtEmail.Text = "Не указан";
            txtPhone.Text = "Не указан";
            txtRegistrationDate.Text = "Не указана";
            txtRole.Text = "Гость";
            txtStatus.Text = "Не активен";
            btnShowEdit.Visibility = Visibility.Collapsed;
            btnChangePassword.Visibility = Visibility.Collapsed;
        }

        // Метод для кнопки "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnShowEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Session.UserID.HasValue)
                {
                    MessageBox.Show("Для редактирования профиля необходимо авторизоваться!",
                        "Требуется авторизация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user == null) return;

                // Заполняем поля
                editFirstName.Text = user.FirstName ?? "";
                editLastName.Text = user.LastName ?? "";
                editEmail.Text = user.Email ?? "";
                editPhone.Text = user.Phone ?? "";

                // Показываем форму редактирования и скрываем другие
                editForm.Visibility = Visibility.Visible;
                btnShowEdit.Visibility = Visibility.Collapsed;
                btnChangePassword.Visibility = Visibility.Collapsed;
                changePasswordForm.Visibility = Visibility.Collapsed;
                btnLogout.Visibility = Visibility.Collapsed; // Скрываем выход при редактировании
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            editForm.Visibility = Visibility.Collapsed;
            btnShowEdit.Visibility = Visibility.Visible;
            btnChangePassword.Visibility = Visibility.Visible;
            btnLogout.Visibility = Visibility.Visible; // Показываем выход обратно
        }

        private void btnSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrEmpty(editFirstName.Text) || string.IsNullOrEmpty(editLastName.Text))
                {
                    MessageBox.Show("Заполните имя и фамилию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(editEmail.Text))
                {
                    MessageBox.Show("Заполните email!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Session.UserID.HasValue) return;

                using (var context = new Entities())
                {
                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null) return;

                    // Проверяем email на уникальность (кроме текущего пользователя)
                    var existingUserWithEmail = context.Users
                        .FirstOrDefault(u => u.Email == editEmail.Text.Trim() && u.UserID != Session.UserID);

                    if (existingUserWithEmail != null)
                    {
                        MessageBox.Show("Этот email уже используется другим пользователем!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Сохраняем старые значения для сравнения
                    var oldFirstName = user.FirstName;
                    var oldLastName = user.LastName;

                    // Обновляем данные
                    user.FirstName = editFirstName.Text.Trim();
                    user.LastName = editLastName.Text.Trim();
                    user.Email = editEmail.Text.Trim();
                    user.Phone = editPhone.Text?.Trim() ?? "";

                    context.SaveChanges();

                    // НЕМЕДЛЕННО обновляем отображение без перезагрузки из БД
                    txtUserName.Text = $"{user.FirstName} {user.LastName}";
                    txtFullName.Text = $"{user.FirstName} {user.LastName}";
                    txtEmail.Text = user.Email ?? "Не указан";
                    txtPhone.Text = user.Phone ?? "Не указан";

                    MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Скрываем форму
                    editForm.Visibility = Visibility.Collapsed;
                    btnShowEdit.Visibility = Visibility.Visible;
                    btnChangePassword.Visibility = Visibility.Visible;
                    btnLogout.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для хэширования пароля (должен совпадать с методом в LoginPage)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Альтернативный метод: Если пароль хранится как MD5
        private string HashPasswordMD5(string password)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = md5.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Метод для проверки формата пароля в базе данных
        private string GetPasswordHash(string password, string storedPassword)
        {
            // Попробуем определить, как хранится пароль в базе
            // 1. Попробуем SHA256
            string sha256Hash = HashPassword(password);

            // 2. Попробуем MD5
            string md5Hash = HashPasswordMD5(password);

            // 3. Возможно пароль хранится в plain text
            // 4. Возможно пароль хранится как MD5 в hex формате
            var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(bytes);
            string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // Сравним с хранимым паролем
            if (storedPassword == sha256Hash)
                return sha256Hash;
            else if (storedPassword == md5Hash)
                return md5Hash;
            else if (storedPassword == hexHash)
                return hexHash;
            else if (storedPassword == password) // plain text
                return password;
            else
                return sha256Hash; // по умолчанию используем SHA256
        }

        // Показать форму смены пароля
        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Session.UserID.HasValue)
                {
                    MessageBox.Show("Для смены пароля необходимо авторизоваться!",
                        "Требуется авторизация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Очищаем поля
                currentPasswordBox.Clear();
                newPasswordBox.Clear();
                confirmPasswordBox.Clear();

                // Показываем форму смены пароля и скрываем другие
                changePasswordForm.Visibility = Visibility.Visible;
                btnChangePassword.Visibility = Visibility.Collapsed;
                btnShowEdit.Visibility = Visibility.Collapsed;
                editForm.Visibility = Visibility.Collapsed;
                btnLogout.Visibility = Visibility.Collapsed; // Скрываем выход при смене пароля
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Отмена смены пароля
        private void btnCancelPassword_Click(object sender, RoutedEventArgs e)
        {
            changePasswordForm.Visibility = Visibility.Collapsed;
            btnChangePassword.Visibility = Visibility.Visible;
            btnShowEdit.Visibility = Visibility.Visible;
            btnLogout.Visibility = Visibility.Visible; // Показываем выход обратно
        }

        // Сохранение нового пароля (ИСПРАВЛЕННЫЙ МЕТОД)
        private void btnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrEmpty(currentPasswordBox.Password))
                {
                    MessageBox.Show("Введите текущий пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    currentPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(newPasswordBox.Password))
                {
                    MessageBox.Show("Введите новый пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    newPasswordBox.Focus();
                    return;
                }

                if (newPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Новый пароль должен содержать не менее 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    newPasswordBox.Focus();
                    return;
                }

                if (newPasswordBox.Password != confirmPasswordBox.Password)
                {
                    MessageBox.Show("Новые пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    confirmPasswordBox.Focus();
                    return;
                }

                if (!Session.UserID.HasValue)
                {
                    MessageBox.Show("Пользователь не авторизован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var context = new Entities())
                {
                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ДЕБАГ: посмотрим, что хранится в базе
                    string storedPassword = user.Password ?? "";
                    MessageBox.Show($"Пароль в базе: {storedPassword}", "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Проверяем текущий пароль РАЗНЫМИ СПОСОБАМИ
                    string currentPassword = currentPasswordBox.Password;
                    bool passwordCorrect = false;

                    // 1. Проверяем как есть (если пароль в plain text)
                    if (storedPassword == currentPassword)
                    {
                        passwordCorrect = true;
                    }
                    // 2. Проверяем SHA256
                    else if (storedPassword == HashPassword(currentPassword))
                    {
                        passwordCorrect = true;
                    }
                    // 3. Проверяем MD5 в Base64
                    else if (storedPassword == HashPasswordMD5(currentPassword))
                    {
                        passwordCorrect = true;
                    }
                    // 4. Проверяем MD5 в HEX формате
                    else
                    {
                        using (var md5 = MD5.Create())
                        {
                            var bytes = Encoding.UTF8.GetBytes(currentPassword);
                            var hashBytes = md5.ComputeHash(bytes);
                            string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                            if (storedPassword == hexHash)
                            {
                                passwordCorrect = true;
                            }
                        }
                    }

                    if (!passwordCorrect)
                    {
                        MessageBox.Show("Текущий пароль указан неверно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        currentPasswordBox.Clear();
                        currentPasswordBox.Focus();
                        return;
                    }

                    // Обновляем пароль (ИСПОЛЬЗУЙТЕ ТОТ ЖЕ ФОРМАТ, ЧТО И В БАЗЕ)
                    // Если пароль хранился в plain text - сохраняем в plain text
                    // Если пароль хранился в SHA256 - сохраняем SHA256
                    // И т.д.

                    // Определяем формат и сохраняем в том же формате
                    if (storedPassword == currentPassword) // plain text
                    {
                        user.Password = newPasswordBox.Password; // plain text
                    }
                    else if (storedPassword == HashPassword(currentPassword)) // SHA256
                    {
                        user.Password = HashPassword(newPasswordBox.Password); // SHA256
                    }
                    else // по умолчанию используем SHA256
                    {
                        user.Password = HashPassword(newPasswordBox.Password);
                    }

                    context.SaveChanges();

                    MessageBox.Show("Пароль успешно изменен! Пожалуйста, войдите с новым паролем.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Выходим из системы после смены пароля
                    Session.Clear();
                    NavigationService.Navigate(new LoginPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене пароля: {ex.Message}\n\nПодробности: {ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Упрощенный метод смены пароля (если не работает сложный вариант)
        private void btnSavePassword_Simplified_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrEmpty(currentPasswordBox.Password))
                {
                    MessageBox.Show("Введите текущий пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    currentPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(newPasswordBox.Password))
                {
                    MessageBox.Show("Введите новый пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    newPasswordBox.Focus();
                    return;
                }

                if (newPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Новый пароль должен содержать не менее 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    newPasswordBox.Focus();
                    return;
                }

                if (newPasswordBox.Password != confirmPasswordBox.Password)
                {
                    MessageBox.Show("Новые пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    confirmPasswordBox.Focus();
                    return;
                }

                if (!Session.UserID.HasValue) return;

                using (var context = new Entities())
                {
                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ПРОСТОЙ ВАРИАНТ: Пробуем разные форматы паролей
                    string currentPassword = currentPasswordBox.Password;
                    bool passwordCorrect = false;

                    // Вариант 1: пароль хранится как есть (plain text)
                    if (user.Password == currentPassword)
                    {
                        passwordCorrect = true;
                        // Сохраняем новый пароль как есть
                        user.Password = newPasswordBox.Password;
                    }
                    // Вариант 2: пароль в SHA256
                    else if (user.Password == HashPassword(currentPassword))
                    {
                        passwordCorrect = true;
                        // Сохраняем новый пароль в SHA256
                        user.Password = HashPassword(newPasswordBox.Password);
                    }
                    // Вариант 3: пароль в MD5
                    else
                    {
                        // Проверяем MD5
                        using (var md5 = MD5.Create())
                        {
                            var bytes = Encoding.UTF8.GetBytes(currentPassword);
                            var hash = md5.ComputeHash(bytes);
                            string hashString = Convert.ToBase64String(hash);

                            if (user.Password == hashString)
                            {
                                passwordCorrect = true;
                                // Сохраняем новый пароль в MD5
                                var newBytes = Encoding.UTF8.GetBytes(newPasswordBox.Password);
                                var newHash = md5.ComputeHash(newBytes);
                                user.Password = Convert.ToBase64String(newHash);
                            }
                        }
                    }

                    if (!passwordCorrect)
                    {
                        MessageBox.Show("Текущий пароль указан неверно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        currentPasswordBox.Clear();
                        currentPasswordBox.Focus();
                        return;
                    }

                    context.SaveChanges();

                    MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Скрываем форму и очищаем поля
                    changePasswordForm.Visibility = Visibility.Collapsed;
                    btnChangePassword.Visibility = Visibility.Visible;
                    btnShowEdit.Visibility = Visibility.Visible;
                    btnLogout.Visibility = Visibility.Visible;

                    currentPasswordBox.Clear();
                    newPasswordBox.Clear();
                    confirmPasswordBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Session.Clear();
                MessageBox.Show("Вы успешно вышли!", "Выход", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new LoginPage());
            }
        }
    }
}