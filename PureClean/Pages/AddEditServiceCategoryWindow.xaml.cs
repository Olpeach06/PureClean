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
using System.Windows.Shapes;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class AddEditServiceCategoryWindow : Window
    {
        private Entities _context;
        private int? _categoryId;

        public string WindowTitle => _categoryId.HasValue ? "✏️ Редактирование категории" : "➕ Добавление категории";
        public string CategoryIcon => _categoryId.HasValue ? "✏️" : "➕";

        public AddEditServiceCategoryWindow(int? categoryId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _categoryId = categoryId;

            DataContext = this;
            LoadCategoryData();
        }

        private void LoadCategoryData()
        {
            if (_categoryId.HasValue)
            {
                try
                {
                    var category = _context.ServiceCategories.FirstOrDefault(c => c.CategoryID == _categoryId.Value);
                    if (category != null)
                    {
                        txtName.Text = category.Name;
                        txtDescription.Text = category.Description;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки данных категории: {ex.Message}");
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_categoryId.HasValue)
                {
                    // Редактирование существующей категории
                    EditCategory();
                }
                else
                {
                    // Добавление новой категории
                    AddCategory();
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

        private void AddCategory()
        {
            var category = new ServiceCategories
            {
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim()
            };

            _context.ServiceCategories.Add(category);
        }

        private void EditCategory()
        {
            var category = _context.ServiceCategories.FirstOrDefault(c => c.CategoryID == _categoryId.Value);
            if (category == null)
            {
                ShowError("Категория не найдена");
                return;
            }

            category.Name = txtName.Text.Trim();
            category.Description = txtDescription.Text.Trim();
        }

        private bool ValidateInput()
        {
            HideError();

            // Проверка названия
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowError("Название категории обязательно для заполнения");
                txtName.Focus();
                return false;
            }

            if (txtName.Text.Length < 2)
            {
                ShowError("Название категории должно содержать минимум 2 символа");
                txtName.Focus();
                return false;
            }

            // Проверка уникальности названия
            var existingCategory = _context.ServiceCategories
                .Where(c => c.Name == txtName.Text.Trim())
                .Where(c => !_categoryId.HasValue || c.CategoryID != _categoryId.Value)
                .Any();

            if (existingCategory)
            {
                ShowError("Категория с таким названием уже существует");
                txtName.Focus();
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
            if (!_categoryId.HasValue)
            {
                txtName.Focus();
            }
        }
    }
}