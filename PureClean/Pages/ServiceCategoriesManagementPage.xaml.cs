using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class ServiceCategoriesManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<ServiceCategories> _allCategories = new List<ServiceCategories>();

        public ServiceCategoriesManagementPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                // Загружаем данные
                _allCategories = _context.ServiceCategories.ToList();

                // Отладочная информация
                Console.WriteLine($"Загружено категорий: {_allCategories.Count}");

                ApplyFilter();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (_allCategories == null || !_allCategories.Any())
                {
                    dgCategories.ItemsSource = new List<ServiceCategories>();
                    return;
                }

                var filtered = _allCategories.AsEnumerable();

                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(c =>
                        (c.Name != null && c.Name.ToLower().Contains(searchText)) ||
                        (c.Description != null && c.Description.ToLower().Contains(searchText)));
                }

                dgCategories.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            txtTotalCount.Text = _allCategories?.Count.ToString() ?? "0";
        }

        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new AddEditServiceCategoryWindow(null, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Категория успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCategories();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int categoryId = Convert.ToInt32(button.Tag);

                var window = new AddEditServiceCategoryWindow(categoryId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Категория успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCategories();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            txtSearch.Clear();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void dgCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику при выборе категории
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }
    }
}