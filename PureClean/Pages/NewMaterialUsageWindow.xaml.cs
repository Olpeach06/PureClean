using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PureClean.Pages
{
    public partial class NewMaterialUsageWindow : Window
    {
        private Entities _context;

        public class OrderItemDisplay
        {
            public int OrderItemID { get; set; }
            public int OrderID { get; set; }
            public string ServiceName { get; set; }
            public string DisplayName => $"Заказ №{OrderID}: {ServiceName}";
        }

        public NewMaterialUsageWindow(Entities context)
        {
            InitializeComponent();
            _context = context;
            LoadOrderItemsAndMaterials();

            // Установка начального состояния кнопки
            btnSave.IsEnabled = false;
        }

        private void LoadOrderItemsAndMaterials()
        {
            try
            {
                // Загружаем элементы заказов (только активные заказы)
                var orderItems = _context.OrderItems
                    .ToList()
                    .Select(oi => new
                    {
                        oi.OrderItemID,
                        oi.OrderID,
                        oi.ServiceID
                    })
                    .ToList();

                var orderItemDisplays = new List<OrderItemDisplay>();

                foreach (var oi in orderItems)
                {
                    // Проверяем статус заказа
                    var order = _context.Orders.FirstOrDefault(o => o.OrderID == oi.OrderID);
                    if (order != null && order.Status != "Отменен" && order.Status != "Выдан")
                    {
                        var service = _context.Services.FirstOrDefault(s => s.ServiceID == oi.ServiceID);
                        orderItemDisplays.Add(new OrderItemDisplay
                        {
                            OrderItemID = oi.OrderItemID,
                            OrderID = oi.OrderID,
                            ServiceName = service?.Name ?? "Неизвестная услуга"
                        });
                    }
                }

                cmbOrderItem.ItemsSource = orderItemDisplays.OrderBy(oi => oi.OrderID).ToList();

                // Загружаем материалы (только те, что есть на складе)
                var materials = _context.Materials
                    .Where(m => (m.QuantityInStock ?? 0) > 0)
                    .OrderBy(m => m.Name)
                    .ToList();

                cmbMaterial.ItemsSource = materials;

                if (orderItemDisplays.Any())
                    cmbOrderItem.SelectedIndex = 0;

                if (materials.Any())
                    cmbMaterial.SelectedIndex = 0;

                // Активируем кнопку если есть данные
                btnSave.IsEnabled = orderItemDisplays.Any() && materials.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMaterial = cmbMaterial.SelectedItem as Materials;
            if (selectedMaterial != null)
            {
                txtUnit.Text = selectedMaterial.Unit;
                UpdateStockInfo(selectedMaterial);
            }
        }

        private void UpdateStockInfo(Materials material)
        {
            if (material != null)
            {
                decimal stock = material.QuantityInStock ?? 0;
                txtStockInfo.Text = $"На складе: {stock:N2} {material.Unit}";

                // Показываем/скрываем блок с информацией и меняем цвет
                if (stockInfoBorder != null)
                {
                    if (stock < (material.MinQuantity ?? 10))
                    {
                        // Критически мало
                        stockInfoBorder.Visibility = Visibility.Visible;
                        stockInfoBorder.Background = new SolidColorBrush(Colors.LightCoral);
                    }
                    else if (stock < (material.MinQuantity ?? 10) * 2)
                    {
                        // Мало осталось
                        stockInfoBorder.Visibility = Visibility.Visible;
                        stockInfoBorder.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                    }
                    else if (stock > 0)
                    {
                        // Достаточно
                        stockInfoBorder.Visibility = Visibility.Visible;
                        stockInfoBorder.Background = new SolidColorBrush(Colors.LightGreen);
                    }
                    else
                    {
                        // Нет на складе (не должно быть, так как фильтруем)
                        stockInfoBorder.Visibility = Visibility.Visible;
                        stockInfoBorder.Background = new SolidColorBrush(Colors.LightGray);
                    }
                }

                // Проверяем введенное количество
                CheckQuantityAgainstStock(material);
            }
        }

        private void CheckQuantityAgainstStock(Materials material)
        {
            if (material != null && !string.IsNullOrEmpty(txtQuantity.Text))
            {
                if (decimal.TryParse(txtQuantity.Text, out decimal quantity))
                {
                    decimal stock = material.QuantityInStock ?? 0;

                    if (quantity > stock)
                    {
                        txtStockInfo.Text = $"⚠️ Недостаточно! Доступно: {stock:N2} {material.Unit}";
                        if (stockInfoBorder != null)
                            stockInfoBorder.Background = new SolidColorBrush(Colors.LightCoral);
                        btnSave.IsEnabled = false;
                    }
                    else if (quantity <= 0)
                    {
                        txtStockInfo.Text = $"Введите количество больше 0";
                        if (stockInfoBorder != null)
                            stockInfoBorder.Background = new SolidColorBrush(Colors.LightGray);
                        btnSave.IsEnabled = false;
                    }
                    else
                    {
                        txtStockInfo.Text = $"Доступно: {stock:N2} {material.Unit} (останется: {stock - quantity:N2})";
                        if (stockInfoBorder != null)
                        {
                            if (stock - quantity < (material.MinQuantity ?? 10))
                                stockInfoBorder.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                            else
                                stockInfoBorder.Background = new SolidColorBrush(Colors.LightGreen);
                        }
                        btnSave.IsEnabled = true;
                    }
                }
            }
        }

        private void cmbOrderItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить дополнительную логику при выборе заказа
            // Например, подгружать рекомендуемые материалы для данной услуги
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedMaterial = cmbMaterial.SelectedItem as Materials;
            if (selectedMaterial != null)
            {
                CheckQuantityAgainstStock(selectedMaterial);
            }
            else
            {
                btnSave.IsEnabled = false;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (cmbOrderItem.SelectedItem == null)
                {
                    MessageBox.Show("Выберите заказ и услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbOrderItem.Focus();
                    return;
                }

                if (cmbMaterial.SelectedItem == null)
                {
                    MessageBox.Show("Выберите материал", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbMaterial.Focus();
                    return;
                }

                if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuantity.Focus();
                    return;
                }

                var orderItem = (OrderItemDisplay)cmbOrderItem.SelectedItem;
                var material = cmbMaterial.SelectedItem as Materials;
                DateTime usageDate = dpUsageDate.SelectedDate ?? DateTime.Now;

                // Проверяем достаточно ли материала на складе
                decimal stock = material.QuantityInStock ?? 0;
                if (quantity > stock)
                {
                    MessageBox.Show($"Недостаточно материала на складе! Доступно: {stock:N2} {material.Unit}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, не существует ли уже записи расхода для этого OrderItemID и MaterialID
                bool existingUsage = _context.MaterialUsages.Any(mu =>
                    mu.OrderItemID == orderItem.OrderItemID && mu.MaterialID == material.MaterialID);

                if (existingUsage)
                {
                    var result = MessageBox.Show($"Для этой услуги уже есть запись расхода данного материала. Хотите добавить дополнительный расход?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // Создаем новую запись расхода
                var newUsage = new MaterialUsages
                {
                    OrderItemID = orderItem.OrderItemID,
                    MaterialID = material.MaterialID,
                    QuantityUsed = quantity,
                    UsageDate = usageDate
                };

                _context.MaterialUsages.Add(newUsage);

                // Уменьшаем количество материала на складе
                material.QuantityInStock = stock - quantity;

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения расхода: {ex.Message}", "Ошибка",
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