using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class CatalogPage : Page
    {
        private Entities _context = new Entities();
        private DispatcherTimer _priceTimer; // Таймер только для фильтра по цене

        public CatalogPage()
        {
            InitializeComponent();

            // Настройка таймера только для фильтра по цене
            _priceTimer = new DispatcherTimer();
            _priceTimer.Interval = TimeSpan.FromMilliseconds(300);
            _priceTimer.Tick += PriceTimer_Tick;

            InitializePage();
        }

        private void InitializePage()
        {
            try
            {
                LoadServiceCategories();
                UpdateCartCounter();
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServiceCategories()
        {
            try
            {
                var categories = _context.ServiceCategories.ToList();
                categoryPanel.Children.Clear();

                foreach (var category in categories)
                {
                    var checkBox = new CheckBox
                    {
                        Content = category.Name,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 5),
                        Tag = category.CategoryID,
                        IsChecked = true // По умолчанию все выбраны
                    };

                    checkBox.Checked += CategoryCheckBox_Changed;
                    checkBox.Unchecked += CategoryCheckBox_Changed;

                    categoryPanel.Children.Add(checkBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshData()
        {
            try
            {
                var filteredData = ApplyFilters();
                servicesPanel.Children.Clear();

                foreach (var service in filteredData)
                {
                    var card = CreateServiceCard(service);
                    servicesPanel.Children.Add(card);
                }

                noServicesPanel.Visibility = filteredData.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Services> ApplyFilters()
        {
            try
            {
                var allData = _context.Services.ToList();
                var result = allData.AsEnumerable();

                // Текстовый поиск
                if (!string.IsNullOrEmpty(txtSearch?.Text))
                {
                    var searchText = txtSearch.Text.ToLower();
                    result = result.Where(s =>
                        s.Name.ToLower().Contains(searchText) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchText)));
                }

                // Фильтр по цене
                decimal minPrice = 0;
                decimal maxPrice = 5000;

                if (decimal.TryParse(txtMinPrice.Text, out decimal parsedMinPrice))
                {
                    minPrice = parsedMinPrice;
                }

                if (decimal.TryParse(txtMaxPrice.Text, out decimal parsedMaxPrice))
                {
                    maxPrice = parsedMaxPrice;
                }

                result = result.Where(s =>
                    (s.FinalPrice != null ? s.FinalPrice.Value : 0) >= minPrice &&
                    (s.FinalPrice != null ? s.FinalPrice.Value : 0) <= maxPrice);

                // Фильтр "Только дорогие"
                if (chkExpensive.IsChecked == true)
                {
                    result = result.Where(s => (s.FinalPrice != null ? s.FinalPrice.Value : 0) > 1000);
                }

                // Фильтр по категориям
                var selectedCategories = new List<int>();
                foreach (CheckBox checkBox in categoryPanel.Children)
                {
                    if (checkBox.IsChecked == true && checkBox.Tag is int categoryId)
                    {
                        selectedCategories.Add(categoryId);
                    }
                }

                if (selectedCategories.Any())
                {
                    result = result.Where(s => selectedCategories.Contains(s.CategoryID));
                }

                // Сортировка
                if (cmbSort != null && cmbSort.SelectedIndex >= 0)
                {
                    switch (cmbSort.SelectedIndex)
                    {
                        case 0: // По умолчанию
                            result = result.OrderBy(s => s.ServiceID);
                            break;
                        case 1: // По возрастанию цены
                            result = result.OrderBy(s => s.FinalPrice);
                            break;
                        case 2: // По убыванию цены
                            result = result.OrderByDescending(s => s.FinalPrice);
                            break;
                        case 3: // По названию
                            result = result.OrderBy(s => s.Name);
                            break;
                    }
                }
                else
                {
                    result = result.OrderBy(s => s.ServiceID);
                }

                return result.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Services>();
            }
        }

        private Border CreateServiceCard(Services service)
        {
            var border = new Border
            {
                Width = 240,
                Height = 380,
                Background = Brushes.White,
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(10),
                Cursor = Cursors.Hand
            };

            border.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                Opacity = 0.1,
                ShadowDepth = 2,
                Color = Colors.Black
            };

            var mainStackPanel = new StackPanel
            {
                Margin = new Thickness(15)
            };

            // Изображение услуги
            var imageBorder = new Border
            {
                Height = 100,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var iconText = GetCategoryIcon(service.CategoryID);
            var iconBlock = new TextBlock
            {
                Text = iconText,
                FontSize = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            imageBorder.Child = iconBlock;

            // Название
            var nameText = new TextBlock
            {
                Text = service.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5),
                MaxWidth = 200,
                MaxHeight = 45,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Описание
            TextBlock descriptionText = null;
            if (!string.IsNullOrEmpty(service.Description))
            {
                descriptionText = new TextBlock
                {
                    Text = service.Description.Length > 70
                        ? service.Description.Substring(0, 70) + "..."
                        : service.Description,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10),
                    MaxWidth = 200,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
            }

            // Цена
            var priceStackPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (service.OldPrice.HasValue && service.OldPrice.Value > 0 && service.DiscountPercent.HasValue)
            {
                var oldPriceStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var oldPriceText = new TextBlock
                {
                    Text = $"{service.OldPrice.Value:N0} ₽",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    TextDecorations = TextDecorations.Strikethrough,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var discountBadge = new Border
                {
                    Background = Brushes.OrangeRed,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2, 5, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = $"-{service.DiscountPercent.Value}%",
                        FontSize = 10,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    }
                };

                oldPriceStack.Children.Add(oldPriceText);
                oldPriceStack.Children.Add(discountBadge);
                priceStackPanel.Children.Add(oldPriceStack);
            }

            var currentPriceText = new TextBlock
            {
                Text = $"{service.FinalPrice:N0} ₽",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = (service.FinalPrice != null && service.FinalPrice.Value > 1000) ?
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E91E63")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 5)
            };

            if (service.FinalPrice != null && service.FinalPrice.Value > 1000)
            {
                var expensiveBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252")),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "Премиум",
                        FontSize = 11,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    }
                };
                priceStackPanel.Children.Add(expensiveBadge);
            }

            priceStackPanel.Children.Add(currentPriceText);

            // Панель для кнопок
            var buttonsStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Кнопка "Подробнее"
            var detailsButton = new Button
            {
                Content = "Подробнее",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                FontSize = 12,
                Height = 30,
                Width = 90,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Tag = service.ServiceID
            };

            detailsButton.Click += (s, e) =>
            {
                NavigateToServiceDetails(service.ServiceID);
            };

            // Кнопка "В корзину"
            var addToCartButton = new Button
            {
                Content = "В корзину",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Height = 30,
                Width = 90,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = service.ServiceID
            };

            addToCartButton.Click += (s, e) =>
            {
                AddToCart(service.ServiceID, service.Name);
            };

            // Добавляем стили через триггеры
            detailsButton.MouseEnter += (s, e) =>
            {
                detailsButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fdfdc9"));
                detailsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b289c7"));
            };

            detailsButton.MouseLeave += (s, e) =>
            {
                detailsButton.Background = Brushes.Transparent;
                detailsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"));
            };

            addToCartButton.MouseEnter += (s, e) =>
            {
                addToCartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b289c7"));
            };

            addToCartButton.MouseLeave += (s, e) =>
            {
                addToCartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"));
            };

            buttonsStackPanel.Children.Add(detailsButton);
            buttonsStackPanel.Children.Add(addToCartButton);

            // Собираем карточку
            mainStackPanel.Children.Add(imageBorder);
            mainStackPanel.Children.Add(nameText);

            if (descriptionText != null)
                mainStackPanel.Children.Add(descriptionText);

            mainStackPanel.Children.Add(priceStackPanel);
            mainStackPanel.Children.Add(buttonsStackPanel);

            border.Child = mainStackPanel;

            // Обработчик клика по карточке
            border.MouseLeftButtonDown += (s, e) =>
            {
                var source = e.OriginalSource as FrameworkElement;
                if (source != null && !IsChildOfButton(source))
                {
                    NavigateToServiceDetails(service.ServiceID);
                }
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = Brushes.White;
            };

            return border;
        }

        private bool IsChildOfButton(DependencyObject element)
        {
            while (element != null)
            {
                if (element is Button)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private string GetCategoryIcon(int categoryId)
        {
            switch (categoryId)
            {
                case 1:
                    return "🧥"; // Верхняя одежда
                case 2:
                    return "👕"; // Одежда
                case 3:
                    return "🛏️"; // Постельное белье
                case 4:
                    return "🎀"; // Текстиль
                case 5:
                    return "👔"; // Костюмы
                case 6:
                    return "👟"; // Обувь
                default:
                    return "🧺";
            }
        }

        private void NavigateToServiceDetails(int serviceId)
        {
            try
            {
                var serviceDetailsPage = new ServiceDetailsPage(serviceId);
                NavigationService.Navigate(serviceDetailsPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToCart(int serviceId, string serviceName = null)
        {
            try
            {
                if (Session.IsGuest || !Session.IsAuthenticated)
                {
                    ShowGuestWarning();
                    return;
                }

                using (var context = new Entities())
                {
                    var service = context.Services.FirstOrDefault(s => s.ServiceID == serviceId);
                    if (service == null)
                    {
                        MessageBox.Show("Услуга не найдена!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var client = context.Clients.FirstOrDefault(c =>
                        c.Email == user.Email ||
                        (!string.IsNullOrEmpty(user.Phone) && c.Phone == user.Phone));

                    if (client == null)
                    {
                        try
                        {
                            string phone = !string.IsNullOrEmpty(user.Phone) ? user.Phone :
                                $"+7{new Random().Next(100000000, 999999999)}";

                            string email = !string.IsNullOrEmpty(user.Email) ? user.Email :
                                $"{user.Login.Replace(" ", "")}_{Session.UserID}@pureclean.com";

                            client = new Clients
                            {
                                FirstName = user.FirstName ?? "Имя",
                                LastName = user.LastName ?? "Фамилия",
                                Phone = phone,
                                Email = email,
                                RegistrationDate = DateTime.Now
                            };

                            context.Clients.Add(client);
                            context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка создания клиента: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    var cart = context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);

                    if (cart == null)
                    {
                        cart = new Cart
                        {
                            ClientID = client.ClientID,
                            CreatedDate = DateTime.Now,
                            LastUpdated = DateTime.Now
                        };
                        context.Cart.Add(cart);
                        context.SaveChanges();
                    }

                    var existingItem = context.CartItems
                        .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ServiceID == serviceId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                        existingItem.AddedDate = DateTime.Now;
                    }
                    else
                    {
                        var cartItem = new CartItems
                        {
                            CartID = cart.CartID,
                            ServiceID = serviceId,
                            Quantity = 1,
                            AddedDate = DateTime.Now
                        };
                        context.CartItems.Add(cartItem);
                    }

                    cart.LastUpdated = DateTime.Now;
                    context.SaveChanges();

                    Session.CartItemCount = Session.CartItemCount != null ? Session.CartItemCount + 1 : 1;
                    UpdateCartCounter();

                    MessageBox.Show($"Услуга \"{service.Name}\" добавлена в корзину!",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCartCounter()
        {
            try
            {
                if (Session.IsAuthenticated && Session.UserID != null)
                {
                    using (var context = new Entities())
                    {
                        var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                        if (user != null)
                        {
                            var client = context.Clients.FirstOrDefault(c =>
                                c.Email == user.Email ||
                                (!string.IsNullOrEmpty(user.Phone) && c.Phone == user.Phone));

                            if (client != null)
                            {
                                var cart = context.Cart.FirstOrDefault(c => c.ClientID == client.ClientID);
                                if (cart != null)
                                {
                                    var cartItems = context.CartItems.Where(ci => ci.CartID == cart.CartID).ToList();
                                    int cartItemsCount = 0;

                                    foreach (var item in cartItems)
                                    {
                                        cartItemsCount += item.Quantity;
                                    }

                                    txtCartCount.Text = cartItemsCount.ToString();
                                    Session.CartItemCount = cartItemsCount;
                                    return;
                                }
                            }
                        }
                    }
                }

                txtCartCount.Text = "0";
                Session.CartItemCount = 0;
            }
            catch
            {
                txtCartCount.Text = "0";
            }
        }

        private void ShowGuestWarning()
        {
            MessageBox.Show("Чтобы добавлять услуги в корзину, вам необходимо авторизоваться!",
                "Требуется авторизация",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            var result = MessageBox.Show("Хотите перейти на страницу авторизации?",
                "Авторизация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigationService.Navigate(new LoginPage());
            }
        }

        private void UpdateNavigationVisibility()
        {
            if (Session.IsGuest)
            {
                cartBorder.Visibility = Visibility.Collapsed;
                btnMyOrders.Visibility = Visibility.Collapsed;
                btnProfile.Visibility = Visibility.Collapsed;
            }
            else
            {
                cartBorder.Visibility = Visibility.Visible;
                btnMyOrders.Visibility = Visibility.Visible;
                btnProfile.Visibility = Visibility.Visible;
            }
        }

        // Основные исправления здесь:
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializePage();
                UpdateNavigationVisibility();

                if (Session.IsAdmin || Session.IsManager)
                {
                    adminControlsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    adminControlsPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 1. Поиск - обновляется сразу
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshData(); // Без задержки
        }

        // 2. Таймер только для фильтра по цене
        private void PriceTimer_Tick(object sender, EventArgs e)
        {
            _priceTimer.Stop();
            RefreshData();
        }

        // 3. Сортировка - обновляется сразу
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshData(); // Без задержки
        }

        // 4. Фильтр по цене - с задержкой
        private void PriceFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _priceTimer.Stop();
            _priceTimer.Start(); // Только для фильтра по цене
        }

        // 5. Чекбокс "Дорогие услуги" - обновляется сразу
        private void CheckBoxFilter_Changed(object sender, RoutedEventArgs e)
        {
            RefreshData(); // Без задержки
        }

        // 6. Категории - обновляются сразу
        private void CategoryCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RefreshData(); // Без задержки
        }

        private void btnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            txtMinPrice.Text = "0";
            txtMaxPrice.Text = "5000";
            chkExpensive.IsChecked = false;

            if (cmbSort != null)
                cmbSort.SelectedIndex = 0;

            foreach (CheckBox checkBox in categoryPanel.Children)
            {
                checkBox.IsChecked = true;
            }

            RefreshData();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление новой услуги", "Добавить");
        }

        private void MyOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            NavigationService.Navigate(new MyOrdersPage());
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            NavigationService.Navigate(new ProfilePage());
        }

        private void CartIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            NavigationService.Navigate(new CartPage());
        }
    }
}