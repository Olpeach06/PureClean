using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class CartPage : Page, INotifyPropertyChanged
    {
        private Entities _context = new Entities();
        private List<CartItemViewModel> _cartItems = new List<CartItemViewModel>();

        public CartPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Session.IsGuest)
                {
                    ShowAuthRequired();
                    return;
                }

                LoadCartItems();
                StartSpinnerAnimation();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки корзины", ex.Message);
            }
        }

        private void StartSpinnerAnimation()
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };

            if (spinnerRotate != null)
            {
                spinnerRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        private void LoadCartItems()
        {
            try
            {
                ShowLoading();
                _cartItems.Clear();

                if (!Session.UserID.HasValue)
                    return;

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

                var cart = _context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                if (cart == null)
                {
                    ShowEmptyCart();
                    return;
                }

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
                            ImageUrl = service.ImagePath
                        };

                        _cartItems.Add(viewModel);
                    }
                }

                cartItemsControl.ItemsSource = null;
                cartItemsControl.ItemsSource = _cartItems;
                ShowCartContent();
                UpdateCartSummary();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки товаров", ex.Message);
                ShowEmptyCart();
            }
            finally
            {
                HideLoading();
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int cartItemId)
            {
                UpdateQuantity(cartItemId, 1);
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int cartItemId)
            {
                UpdateQuantity(cartItemId, -1);
            }
        }

        private void UpdateQuantity(int cartItemId, int change)
        {
            try
            {
                ShowLoading();

                using (var context = new Entities())
                {
                    var cartItem = context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        var newQuantity = cartItem.Quantity + change;

                        if (newQuantity < 1)
                        {
                            DeleteCartItem(cartItemId);
                            return;
                        }

                        cartItem.Quantity = newQuantity;
                        cartItem.AddedDate = DateTime.Now;

                        var cart = context.Cart.FirstOrDefault(c => c.CartID == cartItem.CartID);
                        if (cart != null)
                        {
                            cart.LastUpdated = DateTime.Now;
                        }

                        context.SaveChanges();

                        // Находим и обновляем ViewModel
                        var item = _cartItems.FirstOrDefault(i => i.CartItemID == cartItemId);
                        if (item != null)
                        {
                            item.Quantity = newQuantity;
                            item.NotifyTotalChanged(); // Используем публичный метод
                        }

                        UpdateCartSummary();
                        AnimateQuantityChange(cartItemId, change > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обновления количества", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }

        private void AnimateQuantityChange(int cartItemId, bool isIncrease)
        {
            var item = _cartItems.FirstOrDefault(i => i.CartItemID == cartItemId);
            if (item != null)
            {
                var container = cartItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container != null)
                {
                    var animation = new DoubleAnimation
                    {
                        From = isIncrease ? 1.1 : 0.9,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };

                    container.BeginAnimation(OpacityProperty, animation);
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int cartItemId)
            {
                DeleteCartItem(cartItemId);
            }
        }

        private void DeleteCartItem(int cartItemId)
        {
            try
            {
                var result = MessageBox.Show("Удалить товар из корзины?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                ShowLoading();

                using (var context = new Entities())
                {
                    var cartItem = context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        var cartId = cartItem.CartID;
                        context.CartItems.Remove(cartItem);
                        context.SaveChanges();

                        // Удаляем из ViewModel
                        var item = _cartItems.FirstOrDefault(i => i.CartItemID == cartItemId);
                        if (item != null)
                        {
                            _cartItems.Remove(item);
                            cartItemsControl.ItemsSource = null;
                            cartItemsControl.ItemsSource = _cartItems;
                        }

                        // Проверяем пустую корзину
                        var remainingItems = context.CartItems.Count(ci => ci.CartID == cartId);
                        if (remainingItems == 0)
                        {
                            var cart = context.Cart.FirstOrDefault(c => c.CartID == cartId);
                            if (cart != null)
                            {
                                context.Cart.Remove(cart);
                                context.SaveChanges();
                            }
                            ShowEmptyCart();
                        }
                        else
                        {
                            UpdateCartSummary();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка удаления товара", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cartItems.Count == 0) return;

                var result = MessageBox.Show("Очистить всю корзину?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                ShowLoading();

                if (!Session.UserID.HasValue) return;

                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user == null) return;

                var client = _context.Clients.FirstOrDefault(c =>
                    c.Email == user.Email || c.Phone == user.Phone || c.Email == user.Login);

                if (client != null)
                {
                    var cart = _context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                    if (cart != null)
                    {
                        using (var context = new Entities())
                        {
                            var cartItems = context.CartItems.Where(ci => ci.CartID == cart.CartID);
                            context.CartItems.RemoveRange(cartItems);

                            var cartToDelete = context.Cart.FirstOrDefault(c => c.CartID == cart.CartID);
                            if (cartToDelete != null)
                            {
                                context.Cart.Remove(cartToDelete);
                            }

                            context.SaveChanges();
                        }
                    }
                }

                _cartItems.Clear();
                cartItemsControl.ItemsSource = null;
                ShowEmptyCart();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка очистки корзины", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                ShowMessage("Корзина пуста!", "Добавьте товары для оформления заказа");
                return;
            }

            // Анимация нажатия кнопки
            var button = sender as Button;
            if (button != null)
            {
                var animation = new DoubleAnimation
                {
                    From = 1,
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(100),
                    AutoReverse = true
                };

                button.BeginAnimation(OpacityProperty, animation);
            }

            // Переход на оформление заказа
            NavigationService.Navigate(new OrderCreationPage());
        }

        private void UpdateCartSummary()
        {
            try
            {
                int totalItems = _cartItems.Sum(item => item.Quantity);
                decimal totalAmount = _cartItems.Sum(item => item.Total);

                txtItemCount.Text = totalItems.ToString();
                txtTotalAmount.Text = $"{totalAmount:N2} ₽";
                txtCartStatus.Text = $"{totalItems} товар{GetPluralEnding(totalItems)} · {totalAmount:N2} ₽";
                

                // Показываем/скрываем сообщение о доставке
                deliveryMessage.Visibility = totalAmount >= 3000 ? Visibility.Visible : Visibility.Collapsed;

                // Включаем/выключаем кнопку оформления
                btnCheckout.IsEnabled = totalItems > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления итогов: {ex.Message}");
            }
        }

        private string GetPluralEnding(int count)
        {
            if (count % 10 == 1 && count % 100 != 11) return "";
            if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) return "а";
            return "ов";
        }

        private void ShowLoading()
        {
            loadingPanel.Visibility = Visibility.Visible;
        }

        private void HideLoading()
        {
            loadingPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyCart()
        {
            emptyCartPanel.Visibility = Visibility.Visible;
            cartScrollViewer.Visibility = Visibility.Collapsed;
        }

        private void ShowCartContent()
        {
            emptyCartPanel.Visibility = Visibility.Collapsed;
            cartScrollViewer.Visibility = Visibility.Visible;
        }

        private void ShowAuthRequired()
        {
            MessageBox.Show("Для доступа к корзине необходимо войти в систему",
                "Требуется авторизация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            NavigationService.Navigate(new LoginPage());
        }

        private void ShowError(string title, string message)
        {
            MessageBox.Show($"{message}", title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new CatalogPage());
        private void GoToCatalogButton_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new CatalogPage());
    }

    // ViewModel для товаров в корзине - исправленная версия
    public class CartItemViewModel : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _price;

        public int CartItemID { get; set; }
        public int ServiceID { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public decimal Total => Price * Quantity;

        // Публичный метод для уведомления об изменении Total
        public void NotifyTotalChanged()
        {
            OnPropertyChanged(nameof(Total));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}