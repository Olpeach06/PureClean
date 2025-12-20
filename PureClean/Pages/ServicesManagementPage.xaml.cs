using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class ServicesManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<ServiceViewModel> _allServices = new List<ServiceViewModel>();

        public ServicesManagementPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadServices();
        }

        private void LoadServices()
        {
            try
            {
                var services = _context.Services
                    .Select(s => new ServiceViewModel
                    {
                        ServiceID = s.ServiceID,
                        Name = s.Name,
                        Description = s.Description,
                        CategoryID = s.CategoryID,
                        CategoryName = s.ServiceCategories.Name,
                        BasePrice = s.BasePrice,
                        OldPrice = s.OldPrice,
                        DiscountPercent = s.DiscountPercent,
                        FinalPrice = s.FinalPrice,
                        ExecutionTimeHours = s.ExecutionTimeHours,
                        ImagePath = s.ImagePath
                    })
                    .ToList();

                _allServices = services;
                ApplyFilter();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (_allServices == null || !_allServices.Any())
                {
                    servicesGrid.ItemsSource = new List<ServiceViewModel>();
                    return;
                }

                var filtered = _allServices.AsEnumerable();

                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(s =>
                        (s.Name != null && s.Name.ToLower().Contains(searchText)) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchText)) ||
                        (s.CategoryName != null && s.CategoryName.ToLower().Contains(searchText)));
                }

                servicesGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allServices == null)
            {
                txtTotalCount.Text = "0";
                txtExpensiveCount.Text = "0";
                txtDiscountCount.Text = "0";
                return;
            }

            txtTotalCount.Text = _allServices.Count.ToString();

            int expensiveCount = _allServices.Count(s => s.FinalPrice > 1000);
            txtExpensiveCount.Text = expensiveCount.ToString();

            int discountCount = _allServices.Count(s => s.DiscountPercent > 0);
            txtDiscountCount.Text = discountCount.ToString();
        }

        private void btnAddService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new AddEditServiceWindow(null, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Услуга успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadServices();
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

                int serviceId = Convert.ToInt32(button.Tag);

                var window = new AddEditServiceWindow(serviceId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show("Услуга успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadServices();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int serviceId = Convert.ToInt32(button.Tag);

                // Переход на страницу деталей услуги
                NavigationService.Navigate(new ServiceDetailsPage(serviceId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadServices();
            txtSearch.Clear();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void servicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику при выборе услуги
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения услуг
        public class ServiceViewModel
        {
            public int ServiceID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int CategoryID { get; set; }
            public string CategoryName { get; set; }
            public decimal BasePrice { get; set; }
            public decimal? OldPrice { get; set; }
            public int? DiscountPercent { get; set; }
            public decimal? FinalPrice { get; set; }
            public int ExecutionTimeHours { get; set; }
            public string ImagePath { get; set; }
        }
    }
}