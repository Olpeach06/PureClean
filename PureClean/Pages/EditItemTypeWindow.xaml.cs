using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace PureClean.Pages
{
    public partial class EditItemTypeWindow : Window
    {
        private Entities _context;
        private int _itemTypeId;
        private ItemTypes _itemType;

        public EditItemTypeWindow(int itemTypeId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _itemTypeId = itemTypeId;
            LoadItemTypeData();
        }

        private void LoadItemTypeData()
        {
            if (_itemTypeId == 0)
            {
                Title = "Добавление нового типа изделия";
                btnSave.Content = "Добавить";
            }
            else
            {
                Title = "Редактирование типа изделия";
                btnSave.Content = "Сохранить";

                _itemType = _context.ItemTypes.FirstOrDefault(it => it.ItemTypeID == _itemTypeId);
                if (_itemType != null)
                {
                    txtName.Text = _itemType.Name;
                    txtMaterial.Text = _itemType.Material ?? "";
                    txtCareInstructions.Text = _itemType.CareInstructions ?? "";
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
                    MessageBox.Show("Введите название типа изделия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (_itemTypeId == 0)
                {
                    // Добавление нового типа изделия
                    var newItemType = new ItemTypes
                    {
                        Name = txtName.Text.Trim(),
                        Material = string.IsNullOrWhiteSpace(txtMaterial.Text) ? null : txtMaterial.Text.Trim(),
                        CareInstructions = string.IsNullOrWhiteSpace(txtCareInstructions.Text) ? null : txtCareInstructions.Text.Trim()
                    };

                    _context.ItemTypes.Add(newItemType);
                }
                else
                {
                    // Редактирование существующего типа изделия
                    if (_itemType != null)
                    {
                        _itemType.Name = txtName.Text.Trim();
                        _itemType.Material = string.IsNullOrWhiteSpace(txtMaterial.Text) ? null : txtMaterial.Text.Trim();
                        _itemType.CareInstructions = string.IsNullOrWhiteSpace(txtCareInstructions.Text) ? null : txtCareInstructions.Text.Trim();
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