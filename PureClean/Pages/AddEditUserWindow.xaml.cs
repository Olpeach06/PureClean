using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddEditUserWindow.xaml
    /// </summary>
    public partial class AddEditUserWindow : Window
    {
        private Entities _context;
        private int? _userId;

        public string WindowTitle => _userId.HasValue ? "✏️ Редактирование пользователя" : "➕ Добавление пользователя";
        public string UserIcon => _userId.HasValue ? "✏️" : "➕";

        public AddEditUserWindow(int? userId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _userId = userId;

            DataContext = this;
            LoadUserData();
            LoadRoles();

            // Показываем поле пароля только при добавлении
            passwordSection.Visibility = _userId.HasValue ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadUserData()
        {
            if (_userId.HasValue)
            {
                try
                {
                    var user = _context.Users.FirstOrDefault(u => u.UserID == _userId.Value);
                    if (user != null)
                    {
                        txtLogin.Text = user.Login;
                        txtEmail.Text = user.Email;
                        txtPhone.Text = user.Phone;
                        txtFirstName.Text = user.FirstName;
                        txtLastName.Text = user.LastName;
                        cmbRole.SelectedValue = user.RoleID;
                        chkIsActive.IsChecked = user.IsActive ?? true;

                        // Для нового пользователя показываем поле пароля
                        if (!_userId.HasValue)
                        {
                            txtPassword.Text = "default123";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки данных пользователя: {ex.Message}");
                }
            }
        }

        private void LoadRoles()
        {
            try
            {
                cmbRole.ItemsSource = _context.Roles
                    .OrderBy(r => r.Name)
                    .ToList();

                if (cmbRole.Items.Count > 0)
                {
                    cmbRole.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_userId.HasValue)
                {
                    // Редактирование существующего пользователя
                    EditUser();
                }
                else
                {
                    // Добавление нового пользователя
                    AddUser();
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void AddUser()
        {
            var user = new Users
            {
                Login = txtLogin.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Phone = txtPhone.Text?.Trim() ?? "",
                FirstName = txtFirstName.Text.Trim(),
                LastName = txtLastName.Text.Trim(),
                RoleID = (int)cmbRole.SelectedValue,
                IsActive = chkIsActive.IsChecked ?? true,
                RegistrationDate = DateTime.Now,
                Password = string.IsNullOrWhiteSpace(txtPassword.Text) ? "default123" : txtPassword.Text.Trim()
            };

            _context.Users.Add(user);
        }

        private void EditUser()
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == _userId.Value);
            if (user == null)
            {
                ShowError("Пользователь не найден");
                return;
            }

            user.Login = txtLogin.Text.Trim();
            user.Email = txtEmail.Text.Trim();
            user.Phone = txtPhone.Text?.Trim() ?? "";
            user.FirstName = txtFirstName.Text.Trim();
            user.LastName = txtLastName.Text.Trim();
            user.RoleID = (int)cmbRole.SelectedValue;
            user.IsActive = chkIsActive.IsChecked ?? true;
        }

        private bool ValidateInput()
        {
            HideError();

            // Проверка логина
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                ShowError("Логин обязателен для заполнения");
                txtLogin.Focus();
                return false;
            }

            if (txtLogin.Text.Length < 3)
            {
                ShowError("Логин должен содержать минимум 3 символа");
                txtLogin.Focus();
                return false;
            }

            // Проверка уникальности логина
            var existingLogin = _context.Users
                .Where(u => u.Login == txtLogin.Text.Trim())
                .Where(u => !_userId.HasValue || u.UserID != _userId.Value)
                .Any();

            if (existingLogin)
            {
                ShowError("Пользователь с таким логином уже существует");
                txtLogin.Focus();
                return false;
            }

            // Проверка email
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                ShowError("Email обязателен для заполнения");
                txtEmail.Focus();
                return false;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                ShowError("Введите корректный email адрес");
                txtEmail.Focus();
                return false;
            }

            // Проверка уникальности email
            var existingEmail = _context.Users
                .Where(u => u.Email == txtEmail.Text.Trim())
                .Where(u => !_userId.HasValue || u.UserID != _userId.Value)
                .Any();

            if (existingEmail)
            {
                ShowError("Пользователь с таким email уже существует");
                txtEmail.Focus();
                return false;
            }

            // Проверка имени и фамилии
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                ShowError("Имя обязательно для заполнения");
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                ShowError("Фамилия обязательна для заполнения");
                txtLastName.Focus();
                return false;
            }

            // Проверка роли
            if (cmbRole.SelectedValue == null)
            {
                ShowError("Выберите роль пользователя");
                cmbRole.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
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
            if (!_userId.HasValue)
            {
                txtLogin.Focus();
            }
        }
    }
}