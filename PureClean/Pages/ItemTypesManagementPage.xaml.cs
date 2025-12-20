using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class ItemTypesManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<ItemTypeViewModel> _allItemTypes = new List<ItemTypeViewModel>();

        public ItemTypesManagementPage()
        {
            InitializeComponent();
            Loaded += ItemTypesManagementPage_Loaded;
        }

        private void ItemTypesManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItemTypes();
        }

        private void LoadItemTypes()
        {
            try
            {
                var itemTypes = _context.ItemTypes
                    .OrderBy(it => it.Name)
                    .ToList()
                    .Select(it => new ItemTypeViewModel
                    {
                        ItemTypeID = it.ItemTypeID,
                        Name = it.Name,
                        Material = it.Material ?? "Не указан",
                        CareInstructions = it.CareInstructions ?? "Не указаны"
                    })
                    .ToList();

                _allItemTypes = itemTypes;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов изделий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allItemTypes == null || !_allItemTypes.Any())
                {
                    itemTypesGrid.ItemsSource = new List<ItemTypeViewModel>();
                    return;
                }

                var filtered = _allItemTypes.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(it =>
                        it.Name.ToLower().Contains(searchText) ||
                        it.Material.ToLower().Contains(searchText) ||
                        it.CareInstructions.ToLower().Contains(searchText) ||
                        it.ItemTypeID.ToString().Contains(searchText));
                }

                itemTypesGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allItemTypes == null || !_allItemTypes.Any())
            {
                txtTotalCount.Text = "0";
                txtMaterialsCount.Text = "0";
                return;
            }

            txtTotalCount.Text = _allItemTypes.Count.ToString();

            // Подсчитываем количество уникальных материалов
            int uniqueMaterials = _allItemTypes
                .Where(it => !string.IsNullOrEmpty(it.Material) && it.Material != "Не указан")
                .Select(it => it.Material.ToLower())
                .Distinct()
                .Count();
            txtMaterialsCount.Text = uniqueMaterials.ToString();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnAddItemType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditItemTypeWindow(0, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadItemTypes();

                    MessageBox.Show("Тип изделия успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления типа изделия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int itemTypeId = Convert.ToInt32(button.Tag);

                var window = new EditItemTypeWindow(itemTypeId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadItemTypes();

                    MessageBox.Show("Тип изделия успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования типа изделия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int itemTypeId = Convert.ToInt32(button.Tag);

                var itemType = _context.ItemTypes.FirstOrDefault(it => it.ItemTypeID == itemTypeId);
                if (itemType == null)
                {
                    MessageBox.Show("Тип изделия не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, используется ли тип изделия в заказах
                bool hasUsage = _context.OrderItemDetails.Any(oid => oid.ItemTypeID == itemTypeId);

                if (hasUsage)
                {
                    MessageBox.Show("Невозможно удалить тип изделия, так как он используется в заказах!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Вы уверены, что хотите удалить тип изделия '{itemType.Name}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.ItemTypes.Remove(itemType);
                    _context.SaveChanges();

                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadItemTypes();

                    MessageBox.Show("Тип изделия успешно удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления типа изделия: {ex.Message}", "Ошибка",
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

        // ViewModel для отображения типов изделий
        public class ItemTypeViewModel
        {
            public int ItemTypeID { get; set; }
            public string Name { get; set; }
            public string Material { get; set; }
            public string CareInstructions { get; set; }
        }
    }
}