using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class MaterialSuppliesPage : Page
    {
        private Entities _context = new Entities();
        private List<SupplyViewModel> _allSupplies = new List<SupplyViewModel>();

        public MaterialSuppliesPage()
        {
            InitializeComponent();
            Loaded += MaterialSuppliesPage_Loaded;
        }

        private void MaterialSuppliesPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSupplies();
        }

        private void LoadSupplies()
        {
            try
            {
                var supplies = _context.MaterialSupplies
                    .Include("Materials")
                    .Include("Suppliers")
                    .OrderByDescending(s => s.SupplyDate)
                    .ToList()
                    .Select(s => new SupplyViewModel
                    {
                        SupplyID = s.SupplyID,
                        Date = s.SupplyDate?.ToString("dd.MM.yyyy") ?? "Не указана",
                        Material = s.Materials?.Name ?? "Неизвестно",
                        Supplier = s.Suppliers?.Name ?? "Неизвестно",
                        Quantity = $"{s.Quantity:N2} {s.Materials?.Unit ?? "шт."}",
                        UnitPrice = $"{s.Price:N2} ₽",
                        TotalPrice = $"{(s.Quantity * s.Price):N2} ₽",
                        Invoice = s.InvoiceNumber ?? "-",
                        RawDate = s.SupplyDate,
                        RawQuantity = s.Quantity,
                        RawPrice = s.Price
                    })
                    .ToList();

                _allSupplies = supplies;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allSupplies == null || !_allSupplies.Any())
                {
                    suppliesGrid.ItemsSource = new List<SupplyViewModel>();
                    return;
                }

                var filtered = _allSupplies.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(s =>
                        s.Material.ToLower().Contains(searchText) ||
                        s.Supplier.ToLower().Contains(searchText) ||
                        s.Invoice.ToLower().Contains(searchText) ||
                        s.SupplyID.ToString().Contains(searchText));
                }

                // Фильтр по дате от
                if (dpFromDate.SelectedDate.HasValue)
                {
                    DateTime fromDate = dpFromDate.SelectedDate.Value;
                    filtered = filtered.Where(s =>
                    {
                        if (s.RawDate.HasValue)
                        {
                            return s.RawDate.Value.Date >= fromDate.Date;
                        }
                        return false;
                    });
                }

                // Фильтр по дате до
                if (dpToDate.SelectedDate.HasValue)
                {
                    DateTime toDate = dpToDate.SelectedDate.Value;
                    filtered = filtered.Where(s =>
                    {
                        if (s.RawDate.HasValue)
                        {
                            return s.RawDate.Value.Date <= toDate.Date;
                        }
                        return false;
                    });
                }

                suppliesGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allSupplies == null || !_allSupplies.Any())
            {
                txtTotalSupplies.Text = "0";
                txtTotalAmount.Text = "0 ₽";
                txtPeriodAmount.Text = "0 ₽";
                return;
            }

            // Общая статистика
            txtTotalSupplies.Text = _allSupplies.Count.ToString();

            decimal totalAmount = _allSupplies.Sum(s => s.RawQuantity * s.RawPrice);
            txtTotalAmount.Text = $"{totalAmount:N2} ₽";

            // Статистика за выбранный период
            var periodSupplies = _allSupplies.AsEnumerable();

            if (dpFromDate.SelectedDate.HasValue)
            {
                DateTime fromDate = dpFromDate.SelectedDate.Value;
                periodSupplies = periodSupplies.Where(s =>
                    s.RawDate.HasValue && s.RawDate.Value.Date >= fromDate.Date);
            }

            if (dpToDate.SelectedDate.HasValue)
            {
                DateTime toDate = dpToDate.SelectedDate.Value;
                periodSupplies = periodSupplies.Where(s =>
                    s.RawDate.HasValue && s.RawDate.Value.Date <= toDate.Date);
            }

            decimal periodAmount = periodSupplies.Sum(s => s.RawQuantity * s.RawPrice);
            txtPeriodAmount.Text = $"{periodAmount:N2} ₽";
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

                int supplyId = Convert.ToInt32(button.Tag);

                // Получаем данные поставки
                var supply = _context.MaterialSupplies
                    .Include("Materials")
                    .Include("Suppliers")
                    .FirstOrDefault(s => s.SupplyID == supplyId);

                if (supply == null)
                {
                    MessageBox.Show("Поставка не найдена!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                decimal totalAmount = supply.Quantity * supply.Price;

                string supplyInfo = $"📦 Информация о поставке №{supply.SupplyID}\n\n" +
                                   $"📅 Дата поставки: {supply.SupplyDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана"}\n" +
                                   $"📋 Материал: {supply.Materials?.Name ?? "Неизвестно"}\n" +
                                   $"🏭 Поставщик: {supply.Suppliers?.Name ?? "Неизвестно"}\n" +
                                   $"👤 Контакт: {supply.Suppliers?.ContactPerson ?? "Не указан"}\n" +
                                   $"📞 Телефон: {supply.Suppliers?.Phone ?? "Не указан"}\n\n" +
                                   $"💰 Детали поставки:\n" +
                                   $"• Количество: {supply.Quantity:N2} {supply.Materials?.Unit ?? "шт."}\n" +
                                   $"• Цена за единицу: {supply.Price:N2} ₽\n" +
                                   $"• Общая сумма: {totalAmount:N2} ₽\n" +
                                   $"• Накладная: {supply.InvoiceNumber ?? "Не указана"}";

                MessageBox.Show(supplyInfo, "Детали поставки",
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

                int supplyId = Convert.ToInt32(button.Tag);

                var window = new EditSupplyWindow(supplyId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadSupplies();

                    MessageBox.Show("Поставка успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования поставки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewSupply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new NewSupplyWindow(_context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadSupplies();

                    MessageBox.Show("Новая поставка успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления поставки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateStatistics();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения поставок
        public class SupplyViewModel
        {
            public int SupplyID { get; set; }
            public string Date { get; set; }
            public string Material { get; set; }
            public string Supplier { get; set; }
            public string Quantity { get; set; }
            public string UnitPrice { get; set; }
            public string TotalPrice { get; set; }
            public string Invoice { get; set; }

            // Для фильтрации и расчетов
            public DateTime? RawDate { get; set; }
            public decimal RawQuantity { get; set; }
            public decimal RawPrice { get; set; }
        }
    }
}