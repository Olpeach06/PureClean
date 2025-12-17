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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using PureClean.AppData;
namespace PureClean.Pages
{
    public partial class CartPage : Page
    {
        private Entities _context = new Entities();
        private List<CartItemViewModel> _cartItems = new List<CartItemViewModel>();

        public CartPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Session.IsGuest)
                {
                    MessageBox.Show("Для просмотра корзины необходимо авторизоваться!",
                        "Требуется авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService.Navigate(new LoginPage());
                    return;
                }

                LoadCartItems();
                UpdateCartSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзины: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCartItems()
        {
            try
            {
                cartItemsPanel.Children.Clear();
                _cartItems.Clear();

                if (!Session.UserID.HasValue)
                    return;

                // Находим клиента пользователя
                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user == null)
                    return;

                var client = _context.Clients.FirstOrDefault(c =>
                    c.Email == user.Email || c.Phone == user.Phone || c.Email == user.Login);

                if (client == null)
                {
                    ShowEmptyCart();
                    return;
                }

                // Находим корзину клиента
                var cart = _context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                if (cart == null)
                {
                    ShowEmptyCart();
                    return;
                }

                // Загружаем товары из корзины
                var cartItems = _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToList();

                if (!cartItems.Any())
                {
                    ShowEmptyCart();
                    return;
                }

                foreach (var cartItem in cartItems)
                {
                    var service = _context.Services.FirstOrDefault(s => s.ServiceID == cartItem.ServiceID);
                    if (service != null)
                    {
                        var viewModel = new CartItemViewModel
                        {
                            CartItemID = cartItem.CartItemID,
                            ServiceID = service.ServiceID,
                            ServiceName = service.Name,
                            Description = service.Description,
                            Price = service.FinalPrice ?? 0,
                            Quantity = cartItem.Quantity,
                            Total = (service.FinalPrice ?? 0) * cartItem.Quantity
                        };

                        _cartItems.Add(viewModel);

                        // Создаем карточку товара
                        var card = CreateCartItemCard(viewModel);
                        cartItemsPanel.Children.Add(card);
                    }
                }

                ShowCartContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowEmptyCart();
            }
        }

        private UIElement CreateCartItemCard(CartItemViewModel item)
        {
            // Основной контейнер
            var border = new Border
            {
                Style = (Style)FindResource("CartItemCardStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Левая часть: информация о товаре
            var infoStackPanel = new StackPanel();
            Grid.SetColumn(infoStackPanel, 0);

            // Название услуги
            var nameText = new TextBlock
            {
                Text = item.ServiceName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };

            // Описание
            TextBlock descriptionText = null;
            if (!string.IsNullOrEmpty(item.Description))
            {
                descriptionText = new TextBlock
                {
                    Text = item.Description.Length > 100
                        ? item.Description.Substring(0, 100) + "..."
                        : item.Description,
                    FontSize = 13,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
            }

            // Цена и количество
            var priceStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var priceText = new TextBlock
            {
                Text = $"Цена: {FormatPrice(item.Price)} ₽",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 20, 0)
            };

            var quantityText = new TextBlock
            {
                Text = $"Количество: {item.Quantity}",
                FontSize = 14,
                Foreground = Brushes.Black
            };

            priceStackPanel.Children.Add(priceText);
            priceStackPanel.Children.Add(quantityText);

            // Общая стоимость
            var totalText = new TextBlock
            {
                Text = $"Сумма: {FormatPrice(item.Total)} ₽",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Margin = new Thickness(0, 10, 0, 0)
            };

            infoStackPanel.Children.Add(nameText);
            if (descriptionText != null)
                infoStackPanel.Children.Add(descriptionText);
            infoStackPanel.Children.Add(priceStackPanel);
            infoStackPanel.Children.Add(totalText);

            // Средняя часть: управление количеством
            var quantityPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 0)
            };
            Grid.SetColumn(quantityPanel, 1);

            // Кнопка уменьшения
            var decreaseButton = new Button
            {
                Content = "−",
                Width = 30,
                Height = 30,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Foreground = Brushes.White,
                Tag = item.CartItemID,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Создаем стиль для кнопки без использования ControlTemplate
            decreaseButton.MouseEnter += (s, e) =>
            {
                decreaseButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b289c7"));
            };
            decreaseButton.MouseLeave += (s, e) =>
            {
                decreaseButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"));
            };

            decreaseButton.Click += (s, e) =>
            {
                UpdateQuantity(item.CartItemID, -1);
            };

            // Текущее количество
            var quantityDisplay = new TextBlock
            {
                Text = item.Quantity.ToString(),
                Width = 40,
                Height = 30,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };

            // Кнопка увеличения
            var increaseButton = new Button
            {
                Content = "+",
                Width = 30,
                Height = 30,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Foreground = Brushes.White,
                Tag = item.CartItemID,
                Margin = new Thickness(5, 0, 0, 0)
            };

            increaseButton.MouseEnter += (s, e) =>
            {
                increaseButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b289c7"));
            };
            increaseButton.MouseLeave += (s, e) =>
            {
                increaseButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"));
            };

            increaseButton.Click += (s, e) =>
            {
                UpdateQuantity(item.CartItemID, 1);
            };

            quantityPanel.Children.Add(decreaseButton);
            quantityPanel.Children.Add(quantityDisplay);
            quantityPanel.Children.Add(increaseButton);

            // Правая часть: кнопка удаления
            var deleteButton = new Button
            {
                Content = "Удалить",
                Width = 100,
                Height = 35,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)), // #FF6B6B
                Foreground = Brushes.White,
                Tag = item.CartItemID,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            Grid.SetColumn(deleteButton, 2);

            deleteButton.MouseEnter += (s, e) =>
            {
                deleteButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)); // #FF5252
            };
            deleteButton.MouseLeave += (s, e) =>
            {
                deleteButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)); // #FF6B6B
            };

            deleteButton.Click += (s, e) =>
            {
                DeleteCartItem(item.CartItemID);
            };

            grid.Children.Add(infoStackPanel);
            grid.Children.Add(quantityPanel);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            return border;
        }

        private void UpdateQuantity(int cartItemId, int change)
        {
            try
            {
                using (var context = new Entities())
                {
                    var cartItem = context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        var newQuantity = cartItem.Quantity + change;

                        if (newQuantity < 1)
                        {
                            // Если количество стало меньше 1, удаляем товар
                            DeleteCartItem(cartItemId);
                            return;
                        }

                        cartItem.Quantity = newQuantity;
                        cartItem.AddedDate = DateTime.Now;

                        // Обновляем время корзины
                        var cart = context.Cart.FirstOrDefault(c => c.CartID == cartItem.CartID);
                        if (cart != null)
                        {
                            cart.LastUpdated = DateTime.Now;
                        }

                        context.SaveChanges();
                        LoadCartItems();
                        UpdateCartSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления количества: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCartItem(int cartItemId)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот товар из корзины?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new Entities())
                    {
                        var cartItem = context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                        if (cartItem != null)
                        {
                            var cartId = cartItem.CartID;
                            context.CartItems.Remove(cartItem);
                            context.SaveChanges();

                            // Проверяем, есть ли еще товары в корзине
                            var remainingItems = context.CartItems.Count(ci => ci.CartID == cartId);
                            if (remainingItems == 0)
                            {
                                // Удаляем пустую корзину
                                var cart = context.Cart.FirstOrDefault(c => c.CartID == cartId);
                                if (cart != null)
                                {
                                    context.Cart.Remove(cart);
                                    context.SaveChanges();
                                }
                            }
                        }
                    }

                    LoadCartItems();
                    UpdateCartSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления товара: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cartItems.Count == 0)
                    return;

                var result = MessageBox.Show("Вы уверены, что хотите очистить всю корзину?",
                    "Подтверждение очистки",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (!Session.UserID.HasValue)
                        return;

                    var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                        return;

                    var client = _context.Clients.FirstOrDefault(c =>
                        c.Email == user.Email || c.Phone == user.Phone || c.Email == user.Login);

                    if (client != null)
                    {
                        var cart = _context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                        if (cart != null)
                        {
                            using (var context = new Entities())
                            {
                                // Удаляем все товары из корзины
                                var cartItems = context.CartItems.Where(ci => ci.CartID == cart.CartID);
                                context.CartItems.RemoveRange(cartItems);

                                // Удаляем саму корзину
                                var cartToDelete = context.Cart.FirstOrDefault(c => c.CartID == cart.CartID);
                                if (cartToDelete != null)
                                {
                                    context.Cart.Remove(cartToDelete);
                                }

                                context.SaveChanges();
                            }
                        }
                    }

                    LoadCartItems();
                    UpdateCartSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки корзины: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Переход на страницу оформления заказа
            NavigationService.Navigate(new OrderCreationPage());
            MessageBox.Show("Переход на оформление заказа", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CatalogPage());
        }

        private void GoToCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CatalogPage());
        }

        private void UpdateCartSummary()
        {
            int totalItems = _cartItems.Sum(item => item.Quantity);
            decimal totalAmount = _cartItems.Sum(item => item.Total);

            txtItemCount.Text = $"{totalItems} товар{GetPluralEnding(totalItems)}";
            txtTotalItems.Text = totalItems.ToString();
            txtTotalAmount.Text = $"{FormatPrice(totalAmount)} ₽";
        }

        private string GetPluralEnding(int count)
        {
            if (count % 10 == 1 && count % 100 != 11) return "";
            if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) return "а";
            return "ов";
        }

        private string FormatPrice(decimal? price)
        {
            if (!price.HasValue)
                return "0";

            decimal value = price.Value;
            if (value % 1 == 0)
                return value.ToString("N0");
            else
                return value.ToString("N2");
        }

        private void ShowEmptyCart()
        {
            cartItemsScrollViewer.Visibility = Visibility.Collapsed;
            summaryPanel.Visibility = Visibility.Collapsed;
            emptyCartPanel.Visibility = Visibility.Visible;
            txtItemCount.Text = "0 товаров";
        }

        private void ShowCartContent()
        {
            cartItemsScrollViewer.Visibility = Visibility.Visible;
            summaryPanel.Visibility = Visibility.Visible;
            emptyCartPanel.Visibility = Visibility.Collapsed;
        }

        // ViewModel для товаров в корзине
        private class CartItemViewModel
        {
            public int CartItemID { get; set; }
            public int ServiceID { get; set; }
            public string ServiceName { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal Total { get; set; }
        }
    }
}
