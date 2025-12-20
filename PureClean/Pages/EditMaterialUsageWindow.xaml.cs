using PureClean.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PureClean.Pages
{
    public partial class EditMaterialUsageWindow : Window
    {
        private Entities _context;
        private int _usageId;
        private MaterialUsages _usage;
        private Materials _material;
        private decimal _originalQuantity;
        private decimal _newQuantity;
        private decimal _materialStock;

        public EditMaterialUsageWindow(int usageId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _usageId = usageId;
            LoadUsageData();

            // Подписка на изменение количества
            txtQuantity.TextChanged += Quantity_TextChanged;
        }

        private void LoadUsageData()
        {
            _usage = _context.MaterialUsages.FirstOrDefault(mu => mu.MaterialUsageID == _usageId);

            if (_usage != null)
            {
                Title = $"Редактирование расхода №{_usage.MaterialUsageID}";

                // Получаем информацию о материале
                _material = _context.Materials.FirstOrDefault(m => m.MaterialID == _usage.MaterialID);

                if (_material != null)
                {
                    txtUnit.Text = _material.Unit;
                    _materialStock = _material.QuantityInStock ?? 0;
                }

                // Получаем информацию о заказе и услуге
                var orderItem = _context.OrderItems
                    .Include("Services")
                    .Include("Orders")
                    .FirstOrDefault(oi => oi.OrderItemID == _usage.OrderItemID);

                string orderInfo = "Неизвестный заказ";
                string materialInfo = _material?.Name ?? "Неизвестный материал";

                if (orderItem != null)
                {
                    orderInfo = $"Заказ №{orderItem.OrderID}, Услуга: {orderItem.Services?.Name ?? "Неизвестно"}";
                }

                // Устанавливаем DataContext для привязки
                DataContext = new
                {
                    OrderInfo = orderInfo,
                    MaterialInfo = materialInfo
                };

                _originalQuantity = _usage.QuantityUsed;
                _newQuantity = _originalQuantity;

                txtQuantity.Text = _originalQuantity.ToString("N2");
                dpUsageDate.SelectedDate = _usage.UsageDate;

                UpdateStockInfo();
            }
        }

        private void UpdateStockInfo()
        {
            if (_material != null)
            {
                decimal availableStock = _materialStock + _originalQuantity; // Текущий запас + то, что мы вернем
                txtStockInfo.Text = $"Доступно для использования: {availableStock:N2} {_material.Unit}";
                txtOriginalQuantity.Text = $"Исходное количество: {_originalQuantity:N2} {_material.Unit}";

                // Показываем предупреждение если мало
                if (availableStock < (_material.MinQuantity ?? 10))
                {
                    stockInfoBorder.Background = System.Windows.Media.Brushes.LightCoral;
                }
                else if (availableStock < (_material.MinQuantity ?? 10) * 2)
                {
                    stockInfoBorder.Background = System.Windows.Media.Brushes.LightGoldenrodYellow;
                }
                else
                {
                    stockInfoBorder.Background = System.Windows.Media.Brushes.LightGreen;
                }
            }
        }

        private void Quantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtQuantity.Text, out decimal quantity))
            {
                _newQuantity = quantity;

                // Проверяем, достаточно ли материала
                if (_material != null)
                {
                    decimal availableStock = _materialStock + _originalQuantity;

                    if (quantity > availableStock)
                    {
                        txtStockInfo.Text = $"⚠️ Недостаточно! Доступно: {availableStock:N2} {_material.Unit}";
                        stockInfoBorder.Background = System.Windows.Media.Brushes.LightCoral;
                        btnSave.IsEnabled = false;
                    }
                    else
                    {
                        decimal difference = quantity - _originalQuantity;
                        if (difference > 0)
                        {
                            txtStockInfo.Text = $"После изменения: -{difference:N2} {_material.Unit} к запасу";
                        }
                        else if (difference < 0)
                        {
                            txtStockInfo.Text = $"После изменения: +{Math.Abs(difference):N2} {_material.Unit} к запасу";
                        }
                        else
                        {
                            txtStockInfo.Text = $"Без изменений запаса";
                        }

                        btnSave.IsEnabled = true;
                    }
                }
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
                if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuantity.Focus();
                    return;
                }

                if (_usage != null && _material != null)
                {
                    // Проверяем, достаточно ли материала
                    decimal availableStock = _materialStock + _originalQuantity;
                    if (quantity > availableStock)
                    {
                        MessageBox.Show($"Недостаточно материала на складе! Доступно: {availableStock:N2} {_material.Unit}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Сохраняем старую информацию для отката если нужно
                    decimal oldQuantity = _usage.QuantityUsed;

                    // Обновляем данные расхода
                    _usage.QuantityUsed = quantity;
                    _usage.UsageDate = dpUsageDate.SelectedDate;

                    // Корректируем количество материала на складе
                    // Возвращаем старое количество и вычитаем новое
                    _material.QuantityInStock = _materialStock + oldQuantity - quantity;

                    _context.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка",
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