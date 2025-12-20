using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;

namespace PureClean.Pages
{
    public partial class AddEditServiceWindow : Window
    {
        private Entities _context;
        private int? _serviceId;

        public string WindowTitle => _serviceId.HasValue ? "✏️ Редактирование услуги" : "➕ Добавление услуги";
        public string ServiceIcon => _serviceId.HasValue ? "✏️" : "➕";

        public AddEditServiceWindow(int? serviceId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _serviceId = serviceId;

            DataContext = this;
            LoadServiceData();
            LoadCategories();

            // Подписка на изменение цены и скидки
            txtBasePrice.TextChanged += PriceChanged;
            txtDiscount.TextChanged += PriceChanged;
            txtOldPrice.TextChanged += PriceChanged;
        }

        private void LoadServiceData()
        {
            if (_serviceId.HasValue)
            {
                try
                {
                    var service = _context.Services.FirstOrDefault(s => s.ServiceID == _serviceId.Value);
                    if (service != null)
                    {
                        txtName.Text = service.Name;
                        txtDescription.Text = service.Description;
                        txtBasePrice.Text = service.BasePrice.ToString("0.00");
                        txtDiscount.Text = service.DiscountPercent?.ToString() ?? "";
                        txtOldPrice.Text = service.OldPrice?.ToString("0.00") ?? "";
                        txtExecutionTime.Text = service.ExecutionTimeHours.ToString();
                        txtImagePath.Text = service.ImagePath;

                        if (service.DiscountPercent.HasValue && service.DiscountPercent > 0)
                        {
                            oldPriceSection.Visibility = Visibility.Visible;
                        }

                        cmbCategory.SelectedValue = service.CategoryID;
                        UpdateFinalPrice();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки данных услуги: {ex.Message}");
                }
            }
        }

        private void LoadCategories()
        {
            try
            {
                cmbCategory.ItemsSource = _context.ServiceCategories
                    .OrderBy(c => c.Name)
                    .ToList();

                if (cmbCategory.Items.Count > 0)
                {
                    cmbCategory.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void PriceChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFinalPrice();

            // Показываем поле старой цены, если указана скидка
            if (!string.IsNullOrWhiteSpace(txtDiscount.Text) &&
                int.TryParse(txtDiscount.Text, out int discount) &&
                discount > 0)
            {
                oldPriceSection.Visibility = Visibility.Visible;
            }
            else
            {
                oldPriceSection.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateFinalPrice()
        {
            try
            {
                decimal basePrice = 0;
                int discountPercent = 0;

                if (decimal.TryParse(txtBasePrice.Text, out decimal parsedBasePrice))
                {
                    basePrice = parsedBasePrice;
                }

                if (int.TryParse(txtDiscount.Text, out int parsedDiscount))
                {
                    discountPercent = parsedDiscount;
                }

                decimal finalPrice = basePrice;
                if (discountPercent > 0 && discountPercent <= 100)
                {
                    finalPrice = basePrice * (1 - discountPercent / 100m);
                }

                txtFinalPrice.Text = $"{finalPrice:0.00} ₽";
            }
            catch
            {
                txtFinalPrice.Text = "0.00 ₽";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_serviceId.HasValue)
                {
                    // Редактирование существующей услуги
                    EditService();
                }
                else
                {
                    // Добавление новой услуги
                    AddService();
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

        private void AddService()
        {
            var service = new Services
            {
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                CategoryID = (int)cmbCategory.SelectedValue,
                BasePrice = decimal.Parse(txtBasePrice.Text),
                ExecutionTimeHours = int.Parse(txtExecutionTime.Text),
                ImagePath = txtImagePath.Text.Trim()
            };

            if (!string.IsNullOrWhiteSpace(txtDiscount.Text) && int.TryParse(txtDiscount.Text, out int discount))
            {
                service.DiscountPercent = discount;
            }

            if (!string.IsNullOrWhiteSpace(txtOldPrice.Text) && decimal.TryParse(txtOldPrice.Text, out decimal oldPrice))
            {
                service.OldPrice = oldPrice;
            }

            _context.Services.Add(service);
        }

        private void EditService()
        {
            var service = _context.Services.FirstOrDefault(s => s.ServiceID == _serviceId.Value);
            if (service == null)
            {
                ShowError("Услуга не найдена");
                return;
            }

            service.Name = txtName.Text.Trim();
            service.Description = txtDescription.Text.Trim();
            service.CategoryID = (int)cmbCategory.SelectedValue;
            service.BasePrice = decimal.Parse(txtBasePrice.Text);
            service.ExecutionTimeHours = int.Parse(txtExecutionTime.Text);
            service.ImagePath = txtImagePath.Text.Trim();

            if (!string.IsNullOrWhiteSpace(txtDiscount.Text) && int.TryParse(txtDiscount.Text, out int discount))
            {
                service.DiscountPercent = discount;
            }
            else
            {
                service.DiscountPercent = null;
            }

            if (!string.IsNullOrWhiteSpace(txtOldPrice.Text) && decimal.TryParse(txtOldPrice.Text, out decimal oldPrice))
            {
                service.OldPrice = oldPrice;
            }
            else
            {
                service.OldPrice = null;
            }
        }

        private bool ValidateInput()
        {
            HideError();

            // Проверка названия
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowError("Название услуги обязательно для заполнения");
                txtName.Focus();
                return false;
            }

            // Проверка категории
            if (cmbCategory.SelectedValue == null)
            {
                ShowError("Выберите категорию услуги");
                cmbCategory.Focus();
                return false;
            }

            // Проверка базовой цены
            if (!decimal.TryParse(txtBasePrice.Text, out decimal basePrice) || basePrice < 0)
            {
                ShowError("Введите корректную базовую цену (положительное число)");
                txtBasePrice.Focus();
                return false;
            }

            // Проверка скидки
            if (!string.IsNullOrWhiteSpace(txtDiscount.Text))
            {
                if (!int.TryParse(txtDiscount.Text, out int discount) || discount < 0 || discount > 100)
                {
                    ShowError("Скидка должна быть числом от 0 до 100%");
                    txtDiscount.Focus();
                    return false;
                }
            }

            // Проверка старой цены
            if (!string.IsNullOrWhiteSpace(txtOldPrice.Text))
            {
                if (!decimal.TryParse(txtOldPrice.Text, out decimal oldPrice) || oldPrice < 0)
                {
                    ShowError("Введите корректную старую цену (положительное число)");
                    txtOldPrice.Focus();
                    return false;
                }
            }

            // Проверка времени выполнения
            if (!int.TryParse(txtExecutionTime.Text, out int executionTime) || executionTime <= 0)
            {
                ShowError("Введите корректное время выполнения (целое число больше 0)");
                txtExecutionTime.Focus();
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
            if (!_serviceId.HasValue)
            {
                txtName.Focus();
            }
        }
    }
}