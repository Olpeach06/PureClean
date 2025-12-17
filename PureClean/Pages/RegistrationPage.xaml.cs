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
    public partial class RegistrationPage : Page
    {
        // Переменные для хранения видимости паролей
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

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

                    // Создаем нового пользователя (пароль сохраняется как есть)
                    var newUser = new Users
                    {
                        Login = txtEmail.Text, // Используем email как логин
                        Password = txtPassword.Password, // Сохраняем пароль в открытом виде
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

        // Метод для показа/скрытия пароля
        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(txtPassword, btnShowPassword, ref _isPasswordVisible);
        }

        private void ShowConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(txtConfirmPassword, btnShowConfirmPassword, ref _isConfirmPasswordVisible);
        }

        private void TogglePasswordVisibility(PasswordBox passwordBoxControl, Button toggleButton, ref bool isVisible)
        {
            if (!isVisible)
            {
                // Создаем временный TextBox для отображения пароля
                var textBox = new TextBox
                {
                    Text = passwordBoxControl.Password,
                    FontSize = passwordBoxControl.FontSize,
                    Padding = passwordBoxControl.Padding,
                    BorderThickness = passwordBoxControl.BorderThickness,
                    BorderBrush = passwordBoxControl.BorderBrush,
                    Background = passwordBoxControl.Background
                };

                // Заменяем PasswordBox на TextBox
                var parent = passwordBoxControl.Parent as Grid;
                if (parent != null)
                {
                    var column = Grid.GetColumn(passwordBoxControl);
                    var row = Grid.GetRow(passwordBoxControl);

                    parent.Children.Remove(passwordBoxControl);
                    Grid.SetColumn(textBox, column);
                    Grid.SetRow(textBox, row);
                    parent.Children.Add(textBox);

                    // Сохраняем ссылку на PasswordBox в Tag TextBox
                    textBox.Tag = passwordBoxControl;

                    // Меняем иконку кнопки
                    toggleButton.Content = "🙈";
                    isVisible = true;

                    // Фокус на TextBox
                    textBox.Focus();
                }
            }
            else
            {
                // Восстанавливаем PasswordBox
                var parent = toggleButton.Parent as Grid;
                if (parent != null)
                {
                    // Ищем TextBox в той же колонке
                    TextBox textBoxToRemove = null;
                    foreach (var child in parent.Children)
                    {
                        if (child is TextBox textBox && Grid.GetColumn(textBox) == Grid.GetColumn(toggleButton) - 1)
                        {
                            textBoxToRemove = textBox;
                            break;
                        }
                    }

                    if (textBoxToRemove != null)
                    {
                        var passwordBox = textBoxToRemove.Tag as PasswordBox;
                        if (passwordBox != null)
                        {
                            passwordBox.Password = textBoxToRemove.Text;

                            var column = Grid.GetColumn(textBoxToRemove);
                            var row = Grid.GetRow(textBoxToRemove);

                            parent.Children.Remove(textBoxToRemove);
                            Grid.SetColumn(passwordBox, column);
                            Grid.SetRow(passwordBox, row);
                            parent.Children.Add(passwordBox);

                            // Меняем иконку кнопки обратно
                            toggleButton.Content = "👁";
                            isVisible = false;

                            // Фокус на PasswordBox
                            passwordBox.Focus();
                        }
                    }
                }
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