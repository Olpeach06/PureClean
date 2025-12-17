using PureClean.AppData;
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
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = txtLogin.Text;
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль!", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка пользователя в базе данных
                using (var context = new Entities())
                {
                    // ВНИМАНИЕ: В реальном проекте пароли нужно хэшировать!
                    // Здесь для простоты ищем по логину и паролю напрямую
                    var user = context.Users.FirstOrDefault(x =>
                        (x.Login == login || x.Email == login || x.Phone == login) &&
                        x.Password == password); // В вашей БД уже есть хэшированные пароли

                    if (user == null)
                    {
                        MessageBox.Show("Неверный логин или пароль!", "Ошибка авторизации",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!user.IsActive.GetValueOrDefault(true))
                    {
                        MessageBox.Show("Аккаунт заблокирован! Обратитесь к администратору.",
                            "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Сохранение информации о текущем пользователе
                    CurrentUser.UserID = user.UserID;
                    CurrentUser.Login = user.Login;
                    CurrentUser.FullName = $"{user.FirstName} {user.LastName}";
                    CurrentUser.RoleID = user.RoleID;
                    CurrentUser.LoginTime = DateTime.Now;

                    Session.UserID = user.UserID;
                    Session.Login = user.Login; // ИЛИ user.Email - что есть в БД для поиска клиента
                    Session.FullName = $"{user.FirstName} {user.LastName}";
                    Session.RoleID = user.RoleID;
                    Session.LoginTime = DateTime.Now;

                    // Определение роли и перенаправление
                    switch (user.RoleID)
                    {
                        case 1: // Гость
                            MessageBox.Show($"Добро пожаловать, {user.FirstName}!",
                                "Вход выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                            NavigationService.Navigate(new CatalogPage());
                            break;

                        case 2: // Пользователь
                            MessageBox.Show($"Добро пожаловать, {user.FirstName}!",
                                "Вход выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                            NavigationService.Navigate(new CatalogPage());
                            break;

                        case 3: // Менеджер
                            MessageBox.Show($"Добро пожаловать, {user.FirstName}! (Менеджер)",
                                "Вход выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                            NavigationService.Navigate(new ManagerDashboardPage());
                            break;

                        case 4: // Администратор
                            MessageBox.Show($"Добро пожаловать, {user.FirstName}! (Администратор)",
                                "Вход выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                            NavigationService.Navigate(new AdminDashboardPage());
                            break;

                        default:
                            MessageBox.Show("Неизвестная роль пользователя!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}\n\nПроверьте подключение к базе данных.",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавляем этот метод для обработки клика на гиперссылку
        private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }

        // Также можно добавить метод для кнопки регистрации, если она будет
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CatalogPage());
        }
    }
}
