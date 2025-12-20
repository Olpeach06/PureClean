using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PureClean.Pages
{
    public partial class MaterialsManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<MaterialViewModel> _allMaterials = new List<MaterialViewModel>();
        private List<SupplyViewModel> _allSupplies = new List<SupplyViewModel>();

        public MaterialsManagementPage()
        {
            InitializeComponent();
            Loaded += MaterialsManagementPage_Loaded;
        }

        private void MaterialsManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMaterials();
            LoadSupplies();
        }

        private void LoadMaterials()
        {
            try
            {
                var materials = _context.Materials
                    .ToList()
                    .Select(m => new MaterialViewModel
                    {
                        MaterialID = m.MaterialID,
                        Name = m.Name,
                        Unit = m.Unit,
                        QuantityInStock = m.QuantityInStock ?? 0,
                        MinQuantity = m.MinQuantity ?? 10,
                        Status = GetStockStatus(m.QuantityInStock ?? 0, m.MinQuantity ?? 10),
                        StatusColor = GetStatusColor(m.QuantityInStock ?? 0, m.MinQuantity ?? 10)
                    })
                    .OrderBy(m => m.Status) // Сначала те, что требуют заказа
                    .ThenBy(m => m.Name)
                    .ToList();

                _allMaterials = materials;
                materialsGrid.ItemsSource = _allMaterials;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                        Price = $"{s.Price:N2} ₽",
                        Invoice = s.InvoiceNumber ?? "-"
                    })
                    .ToList();

                _allSupplies = supplies;
                suppliesGrid.ItemsSource = _allSupplies;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStockStatus(decimal quantity, decimal minQuantity)
        {
            if (quantity <= 0) return "Нет на складе";
            if (quantity < minQuantity) return "Требуется заказ";
            if (quantity < minQuantity * 2) return "Мало осталось";
            return "Достаточно";
        }

        private SolidColorBrush GetStatusColor(decimal quantity, decimal minQuantity)
        {
            if (quantity <= 0) return new SolidColorBrush(Colors.Red);
            if (quantity < minQuantity) return new SolidColorBrush(Colors.Orange);
            if (quantity < minQuantity * 2) return new SolidColorBrush(Colors.YellowGreen);
            return new SolidColorBrush(Colors.Green);
        }

        private void UpdateSummary()
        {
            if (_allMaterials == null || !_allMaterials.Any())
            {
                txtTotalMaterials.Text = "0";
                txtNeedOrderCount.Text = "0";
                return;
            }

            txtTotalMaterials.Text = _allMaterials.Count.ToString();
            int needOrderCount = _allMaterials.Count(m =>
                m.QuantityInStock < m.MinQuantity || m.Status == "Требуется заказ" || m.Status == "Нет на складе");
            txtNeedOrderCount.Text = needOrderCount.ToString();
        }

        // Обработчик кнопки "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnAddMaterial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditMaterialWindow(0, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadMaterials();

                    MessageBox.Show("Материал успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления материала: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditMaterial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int materialId = Convert.ToInt32(button.Tag);

                var window = new EditMaterialWindow(materialId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadMaterials();

                    MessageBox.Show("Материал успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования материала: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int materialId = Convert.ToInt32(button.Tag);

                var material = _context.Materials.FirstOrDefault(m => m.MaterialID == materialId);
                if (material == null)
                {
                    MessageBox.Show("Материал не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, есть ли связанные записи
                bool hasSupplies = _context.MaterialSupplies.Any(s => s.MaterialID == materialId);
                bool hasUsages = _context.MaterialUsages.Any(u => u.MaterialID == materialId);

                if (hasSupplies || hasUsages)
                {
                    MessageBox.Show("Невозможно удалить материал, так как он используется в системе!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Вы уверены, что хотите удалить материал '{material.Name}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Materials.Remove(material);
                    _context.SaveChanges();

                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadMaterials();

                    MessageBox.Show("Материал успешно удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления материала: {ex.Message}", "Ошибка",
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
                    LoadMaterials();
                    LoadSupplies();

                    MessageBox.Show("Поставка успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления поставки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для материалов
        public class MaterialViewModel
        {
            public int MaterialID { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public decimal QuantityInStock { get; set; }
            public decimal MinQuantity { get; set; }
            public string Status { get; set; }
            public SolidColorBrush StatusColor { get; set; }

            public string InStock => $"{QuantityInStock:N2} {Unit}";
            public string MinQuantityText => $"{MinQuantity:N2} {Unit}";
        }

        // ViewModel для поставок
        public class SupplyViewModel
        {
            public int SupplyID { get; set; }
            public string Date { get; set; }
            public string Material { get; set; }
            public string Supplier { get; set; }
            public string Quantity { get; set; }
            public string Price { get; set; }
            public string Invoice { get; set; }
        }
    }
}