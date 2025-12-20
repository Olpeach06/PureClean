using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class EditSupplyWindow : Window
    {
        private Entities _context;
        private int _supplyId;
        private MaterialSupplies _supply;
        private decimal _originalQuantity;
        private decimal _newQuantity;

        public EditSupplyWindow(int supplyId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _supplyId = supplyId;
            LoadSupplyData();

            // Подписка на события для расчета суммы
            txtQuantity.TextChanged += CalculateTotalAmount;
            txtPrice.TextChanged += CalculateTotalAmount;
        }

        private void LoadSupplyData()
        {
            _supply = _context.MaterialSupplies
                .Include("Materials")
                .Include("Suppliers")
                .FirstOrDefault(s => s.SupplyID == _supplyId);

            if (_supply != null)
            {
                Title = $"Редактирование поставки №{_supply.SupplyID}";

                txtMaterial.Text = _supply.Materials?.Name ?? "Неизвестно";
                txtSupplier.Text = _supply.Suppliers?.Name ?? "Неизвестно";
                txtUnit.Text = _supply.Materials?.Unit ?? "шт.";

                _originalQuantity = _supply.Quantity;
                _newQuantity = _supply.Quantity;

                txtQuantity.Text = _supply.Quantity.ToString("N2");
                txtPrice.Text = _supply.Price.ToString("N2");
                dpSupplyDate.SelectedDate = _supply.SupplyDate;
                txtInvoice.Text = _supply.InvoiceNumber ?? "";

                CalculateTotalAmount(null, null);
            }
        }

        private void CalculateTotalAmount(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(txtQuantity.Text, out decimal quantity) &&
                    decimal.TryParse(txtPrice.Text, out decimal price))
                {
                    decimal total = quantity * price;
                    txtTotalAmount.Text = $"{total:N2} ₽";
                    _newQuantity = quantity;
                }
                else
                {
                    txtTotalAmount.Text = "0 ₽";
                }
            }
            catch
            {
                txtTotalAmount.Text = "0 ₽";
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuantity.Focus();
                    return;
                }

                if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrice.Focus();
                    return;
                }

                if (_supply != null)
                {
                    // Сохраняем старую цену для отката изменений если нужно
                    decimal oldPrice = _supply.Price;
                    decimal oldQuantity = _supply.Quantity;

                    // Обновляем данные поставки
                    _supply.Quantity = quantity;
                    _supply.Price = price;
                    _supply.SupplyDate = dpSupplyDate.SelectedDate;
                    _supply.InvoiceNumber = string.IsNullOrWhiteSpace(txtInvoice.Text) ? null : txtInvoice.Text.Trim();

                    // Корректируем количество материала на складе
                    if (_supply.Materials != null)
                    {
                        // Вычисляем разницу в количестве
                        decimal quantityDifference = quantity - oldQuantity;
                        _supply.Materials.QuantityInStock = (_supply.Materials.QuantityInStock ?? 0) + quantityDifference;
                    }

                    _context.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка",
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