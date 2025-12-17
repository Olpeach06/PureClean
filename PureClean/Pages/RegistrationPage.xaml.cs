using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrEmpty(txtFirstName.Text) ||
                    string.IsNullOrEmpty(txtLastName.Text) ||
                    string.IsNullOrEmpty(txtEmail.Text) ||
                    string.IsNullOrEmpty(txtPassword.Password))
                {
                    ShowError("Все обязательные поля должны быть заполнены!");
                    return;
                }

                if (txtPassword.Password != txtConfirmPassword.Password)
                {
                    ShowError("Пароли не совпадают!");
                    return;
                }

                if (txtPassword.Password.Length < 6)
                {
                    ShowError("Пароль должен содержать минимум 6 символов!");
                    return;
                }

                if (!IsValidEmail(txtEmail.Text))
                {
                    ShowError("Введите корректный email адрес!");
                    return;
                }

                // Проверка на существующего пользователя
                using (var context = new Entities())
                {
                    // Проверяем, нет ли уже пользователя с таким email или телефоном
                    bool emailExists = context.Users.Any(u => u.Email == txtEmail.Text);
                    bool phoneExists = !string.IsNullOrEmpty(txtPhone.Text) &&
                                      context.Users.Any(u => u.Phone == txtPhone.Text);

                    if (emailExists)
                    {
                        ShowError("Пользователь с таким email уже существует!");
                        return;
                    }

                    if (phoneExists)
                    {
                        ShowError("Пользователь с таким телефоном уже существует!");
                        return;
                    }

                    // Создаем нового пользователя
                    var newUser = new Users
                    {
                        Login = txtEmail.Text, // Используем email как логин
                        PasswordHash = HashPassword(txtPassword.Password), // Хэшируем пароль
                        Email = txtEmail.Text,
                        Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text,
                        FirstName = txtFirstName.Text.Trim(),
                        LastName = txtLastName.Text.Trim(),
                        RoleID = 2, // Роль "Пользователь" по умолчанию
                        RegistrationDate = DateTime.Now,
                        IsActive = true
                    };

                    // Добавляем пользователя в базу данных
                    context.Users.Add(newUser);
                    context.SaveChanges();

                    MessageBox.Show("Регистрация успешно завершена! Теперь вы можете войти в систему.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переход на страницу входа
                    NavigationService?.Navigate(new LoginPage());
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка регистрации: {ex.Message}");
            }
        }

        // Метод хэширования пароля (упрощенный вариант)
        private string HashPassword(string password)
        {
            // В реальном проекте используйте более надежные методы хэширования
            // Например: BCrypt, PBKDF2, Argon2
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Простая проверка email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Добавляем этот метод для обработки клика на гиперссылку
        private void LoginHyperlink_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу входа
            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginPage());
            }
        }

        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            errorMessage.Visibility = Visibility.Visible;
        }
    }
}
