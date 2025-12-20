using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class MaterialUsagesPage : Page
    {
        private Entities _context = new Entities();
        private List<MaterialUsageViewModel> _allUsages = new List<MaterialUsageViewModel>();

        public MaterialUsagesPage()
        {
            InitializeComponent();
            Loaded += MaterialUsagesPage_Loaded;
        }

        private void MaterialUsagesPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMaterialUsages();
        }

        private void LoadMaterialUsages()
        {
            try
            {
                var usages = _context.MaterialUsages
                    .ToList()
                    .Select(mu => new MaterialUsageViewModel
                    {
                        MaterialUsageID = mu.MaterialUsageID,
                        UsageDate = mu.UsageDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана",
                        RawDate = mu.UsageDate,
                        QuantityUsed = mu.QuantityUsed,
                        Unit = GetMaterialUnit(mu.MaterialID),
                        MaterialName = GetMaterialName(mu.MaterialID),
                        OrderID = GetOrderID(mu.OrderItemID),
                        ServiceName = GetServiceName(mu.OrderItemID),
                        RawQuantity = mu.QuantityUsed,
                        MaterialID = mu.MaterialID,
                        OrderItemID = mu.OrderItemID
                    })
                    .OrderByDescending(mu => mu.RawDate)
                    .ToList();

                _allUsages = usages;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки учета материалов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetMaterialName(int materialId)
        {
            try
            {
                var material = _context.Materials.FirstOrDefault(m => m.MaterialID == materialId);
                return material?.Name ?? "Неизвестный материал";
            }
            catch
            {
                return "Неизвестный материал";
            }
        }

        private string GetMaterialUnit(int materialId)
        {
            try
            {
                var material = _context.Materials.FirstOrDefault(m => m.MaterialID == materialId);
                return material?.Unit ?? "шт.";
            }
            catch
            {
                return "шт.";
            }
        }

        private int GetOrderID(int orderItemId)
        {
            try
            {
                var orderItem = _context.OrderItems.FirstOrDefault(oi => oi.OrderItemID == orderItemId);
                return orderItem?.OrderID ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetServiceName(int orderItemId)
        {
            try
            {
                var orderItem = _context.OrderItems
                    .Include("Services")
                    .FirstOrDefault(oi => oi.OrderItemID == orderItemId);

                return orderItem?.Services?.Name ?? "Неизвестная услуга";
            }
            catch
            {
                return "Неизвестная услуга";
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allUsages == null || !_allUsages.Any())
                {
                    usagesGrid.ItemsSource = new List<MaterialUsageViewModel>();
                    return;
                }

                var filtered = _allUsages.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(mu =>
                        mu.MaterialName.ToLower().Contains(searchText) ||
                        mu.ServiceName.ToLower().Contains(searchText) ||
                        mu.OrderID.ToString().Contains(searchText) ||
                        mu.MaterialUsageID.ToString().Contains(searchText));
                }

                // Фильтр по дате от
                if (dpFromDate.SelectedDate.HasValue)
                {
                    DateTime fromDate = dpFromDate.SelectedDate.Value;
                    filtered = filtered.Where(mu =>
                    {
                        if (mu.RawDate.HasValue)
                        {
                            return mu.RawDate.Value.Date >= fromDate.Date;
                        }
                        return false;
                    });
                }

                // Фильтр по дате до
                if (dpToDate.SelectedDate.HasValue)
                {
                    DateTime toDate = dpToDate.SelectedDate.Value;
                    filtered = filtered.Where(mu =>
                    {
                        if (mu.RawDate.HasValue)
                        {
                            return mu.RawDate.Value.Date <= toDate.Date;
                        }
                        return false;
                    });
                }

                usagesGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allUsages == null || !_allUsages.Any())
            {
                txtTotalUsages.Text = "0";
                txtTotalMaterials.Text = "0";
                txtAverageUsage.Text = "0";
                return;
            }

            // Общая статистика
            txtTotalUsages.Text = _allUsages.Count.ToString();

            // Общее количество использованных материалов
            decimal totalQuantity = _allUsages.Sum(mu => mu.RawQuantity);
            txtTotalMaterials.Text = $"{totalQuantity:N2}";

            // Средний расход на одну запись
            if (_allUsages.Any())
            {
                decimal averageUsage = _allUsages.Average(mu => mu.RawQuantity);
                txtAverageUsage.Text = $"{averageUsage:N2}";
            }
            else
            {
                txtAverageUsage.Text = "0";
            }
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

                int usageId = Convert.ToInt32(button.Tag);

                // Получаем данные расхода материала
                var usage = _context.MaterialUsages.FirstOrDefault(mu => mu.MaterialUsageID == usageId);

                if (usage == null)
                {
                    MessageBox.Show("Запись расхода не найдена!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string materialName = GetMaterialName(usage.MaterialID);
                string materialUnit = GetMaterialUnit(usage.MaterialID);
                string serviceName = GetServiceName(usage.OrderItemID);
                int orderId = GetOrderID(usage.OrderItemID);

                string usageInfo = $"🔧 Расход материала №{usage.MaterialUsageID}\n\n" +
                                   $"📅 Дата расхода: {usage.UsageDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана"}\n" +
                                   $"📋 Заказ №{orderId}\n" +
                                   $"🔨 Услуга: {serviceName}\n" +
                                   $"📦 Материал: {materialName}\n" +
                                   $"📏 Количество: {usage.QuantityUsed:N2} {materialUnit}";

                MessageBox.Show(usageInfo, "Детали расхода материала",
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

                int usageId = Convert.ToInt32(button.Tag);

                var window = new EditMaterialUsageWindow(usageId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadMaterialUsages();

                    MessageBox.Show("Расход материала успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования расхода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewUsage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new NewMaterialUsageWindow(_context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadMaterialUsages();

                    MessageBox.Show("Новый расход материала успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления расхода: {ex.Message}", "Ошибка",
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

        // ViewModel для отображения расхода материалов
        public class MaterialUsageViewModel
        {
            public int MaterialUsageID { get; set; }
            public string UsageDate { get; set; }
            public DateTime? RawDate { get; set; }
            public string MaterialName { get; set; }
            public string ServiceName { get; set; }
            public int OrderID { get; set; }
            public decimal QuantityUsed { get; set; }
            public decimal RawQuantity { get; set; }
            public string Unit { get; set; }

            // Для редактирования
            public int MaterialID { get; set; }
            public int OrderItemID { get; set; }

            public string QuantityUsedText => $"{QuantityUsed:N2} {Unit}";
        }
    }
}