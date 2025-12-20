using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace PureClean.Pages
{
    public partial class EditMaterialWindow : Window
    {
        private Entities _context;
        private int _materialId;
        private Materials _material;

        public EditMaterialWindow(int materialId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _materialId = materialId;
            LoadMaterialData();
        }

        private void LoadMaterialData()
        {
            if (_materialId == 0)
            {
                Title = "Добавление нового материала";
                btnSave.Content = "Добавить";
                cmbUnit.SelectedIndex = 0;
            }
            else
            {
                Title = "Редактирование материала";
                btnSave.Content = "Сохранить";

                _material = _context.Materials.FirstOrDefault(m => m.MaterialID == _materialId);
                if (_material != null)
                {
                    txtName.Text = _material.Name;

                    // Выбираем единицу измерения
                    foreach (ComboBoxItem item in cmbUnit.Items)
                    {
                        if (item.Tag?.ToString() == _material.Unit)
                        {
                            cmbUnit.SelectedItem = item;
                            break;
                        }
                    }

                    txtQuantity.Text = (_material.QuantityInStock ?? 0).ToString("N2");
                    txtMinQuantity.Text = (_material.MinQuantity ?? 10).ToString("N2");
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название материала", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (cmbUnit.SelectedItem == null)
                {
                    MessageBox.Show("Выберите единицу измерения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbUnit.Focus();
                    return;
                }

                if (!decimal.TryParse(txtMinQuantity.Text, out decimal minQuantity) || minQuantity < 0)
                {
                    MessageBox.Show("Введите корректное значение минимального запаса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMinQuantity.Focus();
                    return;
                }

                decimal quantity = 0;
                if (!string.IsNullOrWhiteSpace(txtQuantity.Text))
                {
                    if (!decimal.TryParse(txtQuantity.Text, out quantity) || quantity < 0)
                    {
                        MessageBox.Show("Введите корректное количество", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtQuantity.Focus();
                        return;
                    }
                }

                string unit = ((ComboBoxItem)cmbUnit.SelectedItem).Tag.ToString();

                if (_materialId == 0)
                {
                    // Добавление нового материала
                    var newMaterial = new Materials
                    {
                        Name = txtName.Text.Trim(),
                        Unit = unit,
                        QuantityInStock = quantity,
                        MinQuantity = minQuantity
                    };

                    _context.Materials.Add(newMaterial);
                }
                else
                {
                    // Редактирование существующего материала
                    if (_material != null)
                    {
                        _material.Name = txtName.Text.Trim();
                        _material.Unit = unit;
                        _material.QuantityInStock = quantity;
                        _material.MinQuantity = minQuantity;
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