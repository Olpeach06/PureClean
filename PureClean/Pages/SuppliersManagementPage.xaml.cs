using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class SuppliersManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<SupplierViewModel> _allSuppliers = new List<SupplierViewModel>();

        public SuppliersManagementPage()
        {
            InitializeComponent();
            Loaded += SuppliersManagementPage_Loaded;
        }

        private void SuppliersManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                var suppliers = _context.Suppliers
                    .Include(s => s.MaterialSupplies)
                    .OrderBy(s => s.Name)
                    .ToList()
                    .Select(s => new SupplierViewModel
                    {
                        SupplierID = s.SupplierID,
                        Name = s.Name,
                        ContactPerson = s.ContactPerson ?? "Не указано",
                        Phone = s.Phone ?? "Не указан",
                        Email = s.Email ?? "Не указан",
                        SuppliesCount = s.MaterialSupplies.Count
                    })
                    .ToList();

                _allSuppliers = suppliers;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allSuppliers == null || !_allSuppliers.Any())
                {
                    suppliersGrid.ItemsSource = new List<SupplierViewModel>();
                    return;
                }

                var filtered = _allSuppliers.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(s =>
                        s.Name.ToLower().Contains(searchText) ||
                        s.ContactPerson.ToLower().Contains(searchText) ||
                        s.Phone.ToLower().Contains(searchText) ||
                        s.Email.ToLower().Contains(searchText) ||
                        s.SupplierID.ToString().Contains(searchText));
                }

                suppliersGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allSuppliers == null || !_allSuppliers.Any())
            {
                txtTotalSuppliers.Text = "0";
                txtTotalSupplies.Text = "0";
                txtActiveSuppliers.Text = "0";
                return;
            }

            txtTotalSuppliers.Text = _allSuppliers.Count.ToString();
            txtTotalSupplies.Text = _allSuppliers.Sum(s => s.SuppliesCount).ToString();

            // Активные поставщики - те, у кого были поставки за последние 3 месяца
            int activeSuppliers = _allSuppliers.Count(s => s.SuppliesCount > 0);
            txtActiveSuppliers.Text = activeSuppliers.ToString();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int supplierId = Convert.ToInt32(button.Tag);

                // Получаем данные поставщика
                var supplier = _context.Suppliers
                    .Include(s => s.MaterialSupplies)
                    .Include("MaterialSupplies.Materials")
                    .FirstOrDefault(s => s.SupplierID == supplierId);

                if (supplier == null)
                {
                    MessageBox.Show("Поставщик не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Собираем информацию о поставщике
                string supplierInfo = $"🏭 Информация о поставщике\n\n" +
                                   $"📋 Название: {supplier.Name}\n" +
                                   $"👤 Контактное лицо: {supplier.ContactPerson ?? "Не указано"}\n" +
                                   $"📞 Телефон: {supplier.Phone ?? "Не указан"}\n" +
                                   $"📧 Email: {supplier.Email ?? "Не указан"}\n\n" +
                                   $"📊 Статистика:\n" +
                                   $"• Всего поставок: {supplier.MaterialSupplies.Count}\n" +
                                   $"• Последняя поставка: {(supplier.MaterialSupplies.OrderByDescending(ms => ms.SupplyDate).FirstOrDefault()?.SupplyDate?.ToString("dd.MM.yyyy") ?? "Нет поставок")}";

                MessageBox.Show(supplierInfo, "Детали поставщика",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

                int supplierId = Convert.ToInt32(button.Tag);

                var window = new EditSupplierWindow(supplierId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadSuppliers();

                    MessageBox.Show("Данные поставщика успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования поставщика: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditSupplierWindow(0, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadSuppliers();

                    MessageBox.Show("Новый поставщик успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления поставщика: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения поставщиков
        public class SupplierViewModel
        {
            public int SupplierID { get; set; }
            public string Name { get; set; }
            public string ContactPerson { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public int SuppliesCount { get; set; }
        }
    }
}