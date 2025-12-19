using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class OrderCreationPage : Page
    {
        private Entities _context = new Entities();
        private int _currentStep = 1;
        private List<CartServiceItem> _cartItems = new List<CartServiceItem>();

        // Модель для товаров в корзине
        public class CartServiceItem
        {
            public int CartItemID { get; set; }
            public int ServiceID { get; set; }
            public string Name { get; set; }
            public string CategoryName { get; set; }
            public decimal? FinalPrice { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => (FinalPrice ?? 0) * Quantity;
        }

        public OrderCreationPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Session.UserID.HasValue)
                {
                    MessageBox.Show("Для оформления заказа необходимо авторизоваться",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService.Navigate(new LoginPage());
                    return;
                }

                LoadCartItems();

                if (_cartItems.Count == 0)
                {
                    MessageBox.Show("Ваша корзина пуста. Добавьте услуги в корзину перед оформлением заказа.",
                        "Корзина пуста", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.Navigate(new CatalogPage());
                    return;
                }

                LoadUserData();
                UpdateStepVisual();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCartItems()
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user == null) return;

                var client = _context.Clients.FirstOrDefault(c =>
                    c.Email == user.Email ||
                    c.Phone == user.Phone);

                if (client == null)
                {
                    MessageBox.Show("Для оформления заказа необходимо заполнить данные клиента",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cart = _context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                if (cart == null) return;

                var cartItems = _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToList();

                _cartItems = cartItems.Select(ci => new CartServiceItem
                {
                    CartItemID = ci.CartItemID,
                    ServiceID = ci.ServiceID,
                    Quantity = ci.Quantity,
                    Name = ci.Services.Name,
                    CategoryName = ci.Services.ServiceCategories.Name,
                    FinalPrice = ci.Services.FinalPrice
                }).ToList();

                dgCartItems.ItemsSource = _cartItems;
                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзины: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                if (user != null)
                {
                    txtFirstName.Text = user.FirstName;
                    txtLastName.Text = user.LastName;
                    txtPhone.Text = user.Phone ?? "";
                    txtEmail.Text = user.Email ?? "";

                    var client = _context.Clients.FirstOrDefault(c =>
                        c.Email == user.Email ||
                        c.Phone == user.Phone);

                    if (client != null)
                    {
                        txtFirstName.Text = client.FirstName;
                        txtLastName.Text = client.LastName;
                        txtPhone.Text = client.Phone;
                        txtEmail.Text = client.Email ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStepVisual()
        {
            step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            btnBack.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            btnNext.Visibility = _currentStep < 3 ? Visibility.Visible : Visibility.Collapsed;
            btnCreateOrder.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            UpdateStepColors();
        }

        private void UpdateStepColors()
        {
            step2Circle.Opacity = _currentStep >= 2 ? 1.0 : 0.5;
            step3Circle.Opacity = _currentStep >= 3 ? 1.0 : 0.3;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepVisual();
            }
            else
            {
                NavigationService.GoBack();
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentStep)
            {
                case 1:
                    if (ValidateStep1())
                    {
                        _currentStep++;
                        PrepareCartStep();
                        UpdateStepVisual();
                    }
                    break;
                case 2:
                    if (ValidateStep2())
                    {
                        _currentStep++;
                        PrepareConfirmationStep();
                        UpdateStepVisual();
                    }
                    break;
            }
        }

        private bool ValidateStep1()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLastName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите телефон", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateStep2()
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Ваша корзина пуста", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void PrepareCartStep()
        {
            try
            {
                dgCartItems.ItemsSource = _cartItems;
                dgCartItems.Items.Refresh();
                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подготовки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int cartItemId = Convert.ToInt32(button.Tag);
                RemoveFromCart(cartItemId);
            }
        }

        private void CalculateTotal()
        {
            decimal total = _cartItems.Sum(item => item.TotalPrice);
            txtTotalAmount.Text = $"{total:N2} ₽";
        }

        private void btnIncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int cartItemId = Convert.ToInt32(button.Tag);
                var item = _cartItems.FirstOrDefault(i => i.CartItemID == cartItemId);
                if (item != null)
                {
                    var cartItem = _context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        cartItem.Quantity++;
                        _context.SaveChanges();

                        item.Quantity = cartItem.Quantity;
                        dgCartItems.Items.Refresh();
                        CalculateTotal();
                    }
                }
            }
        }

        private void btnDecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int cartItemId = Convert.ToInt32(button.Tag);
                var item = _cartItems.FirstOrDefault(i => i.CartItemID == cartItemId);
                if (item != null && item.Quantity > 1)
                {
                    var cartItem = _context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        cartItem.Quantity--;
                        _context.SaveChanges();

                        item.Quantity = cartItem.Quantity;
                        dgCartItems.Items.Refresh();
                        CalculateTotal();
                    }
                }
                else if (item != null && item.Quantity == 1)
                {
                    RemoveFromCart(cartItemId);
                }
            }
        }

        private void RemoveFromCart(int cartItemId)
        {
            try
            {
                var result = MessageBox.Show("Удалить товар из корзины?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var cartItem = _context.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                    if (cartItem != null)
                    {
                        _context.CartItems.Remove(cartItem);
                        _context.SaveChanges();

                        _cartItems.RemoveAll(i => i.CartItemID == cartItemId);
                        dgCartItems.ItemsSource = _cartItems;
                        dgCartItems.Items.Refresh();
                        CalculateTotal();

                        if (_cartItems.Count == 0)
                        {
                            MessageBox.Show("Корзина пуста. Добавьте товары в корзину.",
                                "Корзина пуста", MessageBoxButton.OK, MessageBoxImage.Information);
                            NavigationService.Navigate(new CatalogPage());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrepareConfirmationStep()
        {
            try
            {
                confirmFirstName.Text = txtFirstName.Text;
                confirmLastName.Text = txtLastName.Text;
                confirmPhone.Text = txtPhone.Text;
                confirmEmail.Text = string.IsNullOrWhiteSpace(txtEmail.Text) ? "Не указан" : txtEmail.Text;
                confirmComment.Text = string.IsNullOrWhiteSpace(txtOrderComment.Text) ? "Нет комментария" : txtOrderComment.Text;

                dgSelectedServices.ItemsSource = _cartItems;

                decimal total = _cartItems.Sum(item => item.TotalPrice);
                confirmTotalAmount.Text = $"{total:N2} ₽";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подготовки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = CreateOrUpdateClient();
                if (client == null)
                {
                    MessageBox.Show("Ошибка создания клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_cartItems.Count == 0)
                {
                    MessageBox.Show("Корзина пуста", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CreateOrderFromCart(client.ClientID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заказа: {ex.Message}\n\nПодробности: {ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Clients CreateOrUpdateClient()
        {
            try
            {
                var existingClient = _context.Clients
                    .FirstOrDefault(c => c.Phone == txtPhone.Text.Trim());

                if (existingClient != null)
                {
                    existingClient.FirstName = txtFirstName.Text.Trim();
                    existingClient.LastName = txtLastName.Text.Trim();
                    existingClient.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                    _context.SaveChanges();
                    return existingClient;
                }
                else
                {
                    var newClient = new Clients
                    {
                        FirstName = txtFirstName.Text.Trim(),
                        LastName = txtLastName.Text.Trim(),
                        Phone = txtPhone.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                        RegistrationDate = DateTime.Now
                    };

                    _context.Clients.Add(newClient);
                    _context.SaveChanges();
                    return newClient;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с клиентом: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void CreateOrderFromCart(int clientId)
        {
            try
            {
                using (var context = new Entities())
                {
                    var cart = context.Cart.FirstOrDefault(c => c.ClientID == clientId);
                    if (cart == null)
                    {
                        MessageBox.Show("Корзина не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var cartItems = context.CartItems
                        .Where(ci => ci.CartID == cart.CartID)
                        .ToList();

                    if (cartItems.Count == 0)
                    {
                        MessageBox.Show("Корзина пуста", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    decimal totalAmount = 0;
                    foreach (var cartItem in cartItems)
                    {
                        var service = context.Services.FirstOrDefault(s => s.ServiceID == cartItem.ServiceID);
                        if (service != null)
                        {
                            totalAmount += (service.FinalPrice ?? 0) * cartItem.Quantity;
                        }
                    }

                    var order = new Orders
                    {
                        ClientID = clientId,
                        UserID = Session.UserID.Value,
                        AcceptanceDate = DateTime.Now,
                        PlannedReturnDate = DateTime.Now.AddDays(7),
                        Status = "Принят",
                        TotalAmount = totalAmount,
                        Prepayment = 0,
                        Comment = txtOrderComment.Text?.Trim()
                    };

                    context.Orders.Add(order);
                    context.SaveChanges();

                    foreach (var cartItem in cartItems)
                    {
                        var service = context.Services.FirstOrDefault(s => s.ServiceID == cartItem.ServiceID);
                        if (service != null)
                        {
                            var orderItem = new OrderItems
                            {
                                OrderID = order.OrderID,
                                ServiceID = cartItem.ServiceID,
                                Quantity = cartItem.Quantity,
                                PriceAtOrder = service.FinalPrice ?? 0
                            };

                            context.OrderItems.Add(orderItem);
                        }
                    }

                    context.CartItems.RemoveRange(cartItems);
                    context.Cart.Remove(cart);
                    context.SaveChanges();

                    MessageBox.Show($"Заказ №{order.OrderID} успешно создан!\nСумма: {totalAmount:N2} ₽",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.Navigate(new MyOrdersPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заказа: {ex.Message}\n\nПодробности: {ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}