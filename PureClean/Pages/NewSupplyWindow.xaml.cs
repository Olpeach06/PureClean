using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class NewSupplyWindow : Window
    {
        private Entities _context;

        public NewSupplyWindow(Entities context)
        {
            InitializeComponent();
            _context = context;
            LoadMaterialsAndSuppliers();

            // Подписка на события для расчета суммы
            txtQuantity.TextChanged += CalculateTotalAmount;
            txtPrice.TextChanged += CalculateTotalAmount;
        }

        private void LoadMaterialsAndSuppliers()
        {
            try
            {
                // Загружаем материалы
                var materials = _context.Materials
                    .OrderBy(m => m.Name)
                    .ToList();
                cmbMaterial.ItemsSource = materials;

                // Загружаем поставщиков
                var suppliers = _context.Suppliers
                    .OrderBy(s => s.Name)
                    .ToList();
                cmbSupplier.ItemsSource = suppliers;

                if (materials.Any())
                    cmbMaterial.SelectedIndex = 0;

                if (suppliers.Any())
                    cmbSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMaterial = cmbMaterial.SelectedItem as Materials;
            if (selectedMaterial != null)
            {
                txtUnit.Text = selectedMaterial.Unit;
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
                if (cmbMaterial.SelectedItem == null)
                {
                    MessageBox.Show("Выберите материал", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbMaterial.Focus();
                    return;
                }

                if (cmbSupplier.SelectedItem == null)
                {
                    MessageBox.Show("Выберите поставщика", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbSupplier.Focus();
                    return;
                }

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

                var material = cmbMaterial.SelectedItem as Materials;
                var supplier = cmbSupplier.SelectedItem as Suppliers;
                DateTime supplyDate = dpSupplyDate.SelectedDate ?? DateTime.Now;
                string invoiceNumber = string.IsNullOrWhiteSpace(txtInvoice.Text) ? null : txtInvoice.Text.Trim();

                // Создаем новую поставку
                var newSupply = new MaterialSupplies
                {
                    MaterialID = material.MaterialID,
                    SupplierID = supplier.SupplierID,
                    Quantity = quantity,
                    Price = price,
                    SupplyDate = supplyDate,
                    InvoiceNumber = invoiceNumber
                };

                _context.MaterialSupplies.Add(newSupply);

                // Обновляем количество материала на складе
                material.QuantityInStock = (material.QuantityInStock ?? 0) + quantity;

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения поставки: {ex.Message}", "Ошибка",
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