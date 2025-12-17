using System;
using System.Linq;
using System.Windows;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        private Entities _context;
        private int _userId;

        public ChangePasswordWindow(int userId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _userId = userId;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                var user = _context.Users.FirstOrDefault(u => u.UserID == _userId);
                if (user == null)
                {
                    ShowError("Пользователь не найден");
                    return;
                }

                // В реальном приложении пароль должен хешироваться!
                user.Password = txtNewPassword.Password;
                _context.SaveChanges();

                MessageBox.Show("Пароль успешно изменен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения пароля: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            HideError();

            // Проверка нового пароля
            if (string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                ShowError("Введите новый пароль");
                txtNewPassword.Focus();
                return false;
            }

            if (txtNewPassword.Password.Length < 6)
            {
                ShowError("Пароль должен содержать минимум 6 символов");
                txtNewPassword.Focus();
                return false;
            }

            // Проверка подтверждения пароля
            if (string.IsNullOrWhiteSpace(txtConfirmPassword.Password))
            {
                ShowError("Подтвердите пароль");
                txtConfirmPassword.Focus();
                return false;
            }

            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                ShowError("Пароли не совпадают");
                txtConfirmPassword.Focus();
                return false;
            }

            // Проверка на слишком простой пароль (дополнительная валидация)
            if (IsPasswordTooSimple(txtNewPassword.Password))
            {
                ShowError("Пароль слишком простой. Используйте буквы, цифры и специальные символы");
                txtNewPassword.Focus();
                return false;
            }

            return true;
        }

        private bool IsPasswordTooSimple(string password)
        {
            // Простая проверка на сложность пароля
            // В реальном приложении можно добавить более сложную логику

            // Проверка на пароль из одинаковых символов
            if (password.Distinct().Count() == 1)
                return true;

            // Проверка на последовательные символы (123456, abcdef)
            if (IsSequential(password))
                return true;

            return false;
        }

        private bool IsSequential(string str)
        {
            for (int i = 1; i < str.Length; i++)
            {
                if (str[i] != str[i - 1] + 1)
                    return false;
            }
            return true;
        }

        private void ShowError(string message)
        {
            errorBorder.Visibility = Visibility.Visible;
            txtErrorMessage.Text = message;
        }

        private void HideError()
        {
            errorBorder.Visibility = Visibility.Collapsed;
            txtErrorMessage.Text = "";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtNewPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnSave_Click(sender, e);
            }
        }
    }
}