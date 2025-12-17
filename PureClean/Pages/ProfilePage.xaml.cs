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
    /// Логика взаимодействия для ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            // Загрузка данных пользователя (заглушка)
            txtUserName.Text = "Иван Иванов";
            txtFullName.Text = "Иван Иванов";
            txtEmail.Text = "ivan@example.com";
            txtPhone.Text = "+7 (999) 123-45-67";
            txtRegistrationDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            txtOrdersCount.Text = "5";
            txtStatus.Text = "Постоянный клиент";
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            // Просто вызываем метод ShowEditForm_Click
            ShowEditForm_Click(sender, e);
        }

        private void ShowEditForm_Click(object sender, RoutedEventArgs e)
        {
            // Заполняем поля текущими данными
            editFirstName.Text = "Иван";
            editLastName.Text = "Иванов";
            editEmail.Text = "ivan@example.com";
            editPhone.Text = "+7 (999) 123-45-67";

            // Показываем форму редактирования
            editForm.Visibility = Visibility.Visible;
            btnShowEdit.Visibility = Visibility.Collapsed;
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем форму редактирования
            editForm.Visibility = Visibility.Collapsed;
            btnShowEdit.Visibility = Visibility.Visible;
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            // Простая валидация
            if (string.IsNullOrEmpty(editFirstName.Text) ||
                string.IsNullOrEmpty(editLastName.Text))
            {
                MessageBox.Show("Заполните имя и фамилию!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Обновляем отображение
            txtFullName.Text = $"{editFirstName.Text} {editLastName.Text}";
            txtUserName.Text = $"{editFirstName.Text} {editLastName.Text}";
            txtEmail.Text = editEmail.Text;
            txtPhone.Text = editPhone.Text;

            MessageBox.Show("Данные сохранены!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Скрываем форму
            editForm.Visibility = Visibility.Collapsed;
            btnShowEdit.Visibility = Visibility.Visible;
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция смены пароля будет реализована позже", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Вы успешно вышли из системы", "Выход",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на страницу входа (раскомментируйте когда будет готова навигация)
                // NavigationService?.Navigate(new LoginPage());
            }
        }
    }
}
