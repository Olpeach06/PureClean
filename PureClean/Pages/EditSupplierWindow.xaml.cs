using PureClean.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace PureClean.Pages
{
    public partial class EditSupplierWindow : Window
    {
        private Entities _context;
        private int _supplierId;
        private Suppliers _supplier;

        public EditSupplierWindow(int supplierId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _supplierId = supplierId;
            LoadSupplierData();
        }

        private void LoadSupplierData()
        {
            if (_supplierId == 0)
            {
                Title = "Добавление нового поставщика";
                btnSave.Content = "Добавить";
            }
            else
            {
                Title = "Редактирование поставщика";
                btnSave.Content = "Сохранить";

                _supplier = _context.Suppliers.FirstOrDefault(s => s.SupplierID == _supplierId);
                if (_supplier != null)
                {
                    txtName.Text = _supplier.Name;
                    txtContactPerson.Text = _supplier.ContactPerson ?? "";
                    txtPhone.Text = _supplier.Phone ?? "";
                    txtEmail.Text = _supplier.Email ?? "";
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название компании поставщика", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
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

                if (_supplierId == 0)
                {
                    // Добавление нового поставщика
                    var newSupplier = new Suppliers
                    {
                        Name = txtName.Text.Trim(),
                        ContactPerson = string.IsNullOrWhiteSpace(txtContactPerson.Text) ? null : txtContactPerson.Text.Trim(),
                        Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(email) ? null : email
                    };

                    _context.Suppliers.Add(newSupplier);
                }
                else
                {
                    // Редактирование существующего поставщика
                    if (_supplier != null)
                    {
                        _supplier.Name = txtName.Text.Trim();
                        _supplier.ContactPerson = string.IsNullOrWhiteSpace(txtContactPerson.Text) ? null : txtContactPerson.Text.Trim();
                        _supplier.Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim();
                        _supplier.Email = string.IsNullOrWhiteSpace(email) ? null : email;
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