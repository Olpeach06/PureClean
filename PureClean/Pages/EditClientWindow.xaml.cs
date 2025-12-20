using PureClean.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PureClean.Pages
{
    public partial class EditClientWindow : Window
    {
        private Entities _context;
        private int _clientId;
        private Clients _client;

        public EditClientWindow(int clientId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _clientId = clientId;
            LoadClientData();
        }

        private void LoadClientData()
        {
            if (_clientId == 0)
            {
                // Новый клиент
                Title = "Добавление нового клиента";
                btnSave.Content = "Добавить";
            }
            else
            {
                // Редактирование существующего клиента
                Title = "Редактирование клиента";
                btnSave.Content = "Сохранить";

                _client = _context.Clients.FirstOrDefault(c => c.ClientID == _clientId);
                if (_client != null)
                {
                    txtLastName.Text = _client.LastName;
                    txtFirstName.Text = _client.FirstName;
                    txtPhone.Text = _client.Phone;
                    txtEmail.Text = _client.Email ?? "";
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email не обязателен

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

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Удаляем все нецифровые символы
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            return cleanPhone.Length >= 10;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("Введите фамилию клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("Введите имя клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPhone.Text))
                {
                    MessageBox.Show("Введите телефон клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPhone.Focus();
                    return;
                }

                if (!IsValidPhone(txtPhone.Text))
                {
                    MessageBox.Show("Введите корректный телефон (минимум 10 цифр)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPhone.Focus();
                    return;
                }

                // Проверка email при наличии
                string email = txtEmail.Text.Trim();
                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                {
                    MessageBox.Show("Введите корректный email адрес", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Проверка уникальности телефона (только для нового клиента)
                if (_clientId == 0)
                {
                    var existingClient = _context.Clients.FirstOrDefault(c => c.Phone == txtPhone.Text.Trim());
                    if (existingClient != null)
                    {
                        MessageBox.Show("Клиент с таким телефоном уже существует!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPhone.Focus();
                        return;
                    }
                }

                if (_clientId == 0)
                {
                    // Добавление нового клиента
                    var newClient = new Clients
                    {
                        LastName = txtLastName.Text.Trim(),
                        FirstName = txtFirstName.Text.Trim(),
                        Phone = txtPhone.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        RegistrationDate = DateTime.Now
                    };

                    _context.Clients.Add(newClient);
                }
                else
                {
                    // Редактирование существующего клиента
                    if (_client != null)
                    {
                        _client.LastName = txtLastName.Text.Trim();
                        _client.FirstName = txtFirstName.Text.Trim();
                        _client.Phone = txtPhone.Text.Trim();
                        _client.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                    }
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}