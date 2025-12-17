using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class CatalogPage : Page
    {
        private Entities _context = new Entities();

        public CatalogPage()
        {
            InitializeComponent();
            InitializePage();

            // Добавьте обработчики событий для фильтров по цене
            txtMinPrice.TextChanged += PriceFilter_TextChanged;
            txtMaxPrice.TextChanged += PriceFilter_TextChanged;
            chkExpensive.Checked += CheckBoxFilter_Changed;
            chkExpensive.Unchecked += CheckBoxFilter_Changed;
            btnResetFilters.Click += btnResetFilters_Click;
        }

        private void InitializePage()
        {
            try
            {
                LoadServiceCategories();
                SetupFilters();
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
                        Tag = category.CategoryID
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

        private void SetupFilters()
        {
            // Уже настроено в XAML
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

                if (!filteredData.Any())
                {
                    noServicesPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    noServicesPanel.Visibility = Visibility.Collapsed;
                }
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
                if (int.TryParse(txtMinPrice.Text, out int minPrice))
                {
                    result = result.Where(s => s.FinalPrice >= minPrice);
                }

                if (int.TryParse(txtMaxPrice.Text, out int maxPrice))
                {
                    result = result.Where(s => s.FinalPrice <= maxPrice);
                }

                // Фильтр "Только дорогие"
                if (chkExpensive.IsChecked == true)
                {
                    result = result.Where(s => s.FinalPrice > 1000);
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
                if (cmbSort?.SelectedIndex > 0)
                {
                    switch (cmbSort.SelectedIndex)
                    {
                        case 1:
                            result = result.OrderBy(s => s.FinalPrice);
                            break;
                        case 2:
                            result = result.OrderByDescending(s => s.FinalPrice);
                            break;
                        case 3:
                            result = result.OrderBy(s => s.Name);
                            break;
                        default:
                            result = result.OrderBy(s => s.ServiceID);
                            break;
                    }
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

        private UIElement CreateServiceCard(Services service)
        {
            var border = new Border
            {
                Width = 220,
                Height = 330,
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

            // Изображение
            var imageBorder = new Border
            {
                Height = 100,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Название
            var nameText = new TextBlock
            {
                Text = service.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5),
                MaxWidth = 190,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            // Описание
            TextBlock descriptionText = null;
            if (!string.IsNullOrEmpty(service.Description))
            {
                descriptionText = new TextBlock
                {
                    Text = service.Description.Length > 60
                        ? service.Description.Substring(0, 60) + "..."
                        : service.Description,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10),
                    MaxWidth = 190,
                    Height = 40
                };
            }

            // Цена
            var priceStackPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            if (service.OldPrice.HasValue && service.OldPrice > 0 && service.DiscountPercent.HasValue)
            {
                var oldPriceText = new TextBlock
                {
                    Text = $"{service.OldPrice.Value} руб.",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    TextDecorations = TextDecorations.Strikethrough,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                priceStackPanel.Children.Add(oldPriceText);
            }

            var currentPriceText = new TextBlock
            {
                Text = $"{service.FinalPrice} руб.",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = service.FinalPrice > 1000 ? Brushes.Red : Brushes.DarkGreen
            };
            priceStackPanel.Children.Add(currentPriceText);

            // Кнопка "В корзину" - ПРОСТОЙ ВАРИАНТ
            var addToCartButton = new Button
            {
                Content = "В корзину",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Height = 35,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 5, 0, 0),
                Cursor = Cursors.Hand,
                Tag = service.ServiceID
            };

            // ПРОСТОЙ ОБРАБОТЧИК С ТРИГГЕРОМ В КОДЕ
            addToCartButton.MouseEnter += (s, e) =>
            {
                addToCartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b289c7"));
            };

            addToCartButton.MouseLeave += (s, e) =>
            {
                addToCartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c69fd5"));
            };

            addToCartButton.Click += (s, e) =>
            {
                AddToCart(service.ServiceID, service.Name);
            };

            // Собираем карточку
            mainStackPanel.Children.Add(imageBorder);
            mainStackPanel.Children.Add(nameText);

            if (descriptionText != null)
                mainStackPanel.Children.Add(descriptionText);

            mainStackPanel.Children.Add(priceStackPanel);
            mainStackPanel.Children.Add(addToCartButton);

            border.Child = mainStackPanel;
            return border;
        }

        // ИСПРАВЛЕННЫЙ МЕТОД ДОБАВЛЕНИЯ В КОРЗИНУ
        private void AddToCart(int serviceId, string serviceName = null)
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                if (Session.IsGuest)
                {
                    ShowGuestWarning();
                    return;
                }

                // Проверяем, авторизован ли пользователь
                if (!Session.IsAuthenticated)
                {
                    MessageBox.Show("Пожалуйста, войдите в систему для добавления услуг в корзину.",
                        "Требуется авторизация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                using (var context = new Entities())
                {
                    // Проверяем, существует ли услуга
                    var service = context.Services.FirstOrDefault(s => s.ServiceID == serviceId);
                    if (service == null)
                    {
                        MessageBox.Show("Услуга не найдена!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Находим ClientID пользователя - УПРОЩЕННЫЙ ВАРИАНТ
                    // Предполагаем, что в таблице Users есть связь с Clients
                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Сначала пытаемся найти клиента по email или phone
                    var client = context.Clients.FirstOrDefault(c =>
                        c.Email == user.Email ||
                        c.Phone == user.Phone ||
                        (c.Email == user.Login) ||
                        (c.Phone == user.Login));

                    // Если клиент не найден, создаем нового клиента - УПРОЩЕННЫЙ ВАРИАНТ
                    if (client == null)
                    {
                        try
                        {
                            client = new Clients
                            {
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                Phone = user.Phone ?? string.Empty,
                                Email = user.Email ?? user.Login,
                                RegistrationDate = DateTime.Now
                            };
                            context.Clients.Add(client);
                            context.SaveChanges(); // Сохраняем сначала клиента

                            MessageBox.Show($"Создан новый клиент: {client.FirstName} {client.LastName}",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка создания клиента: {ex.Message}\n\nInner: {ex.InnerException?.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    int clientId = client.ClientID;

                    // Находим или создаем корзину
                    var cart = context.Cart.FirstOrDefault(c => c.ClientID == clientId);

                    if (cart == null)
                    {
                        cart = new Cart
                        {
                            ClientID = clientId,
                            CreatedDate = DateTime.Now,
                            LastUpdated = DateTime.Now
                        };
                        context.Cart.Add(cart);
                        try
                        {
                            context.SaveChanges(); // Сохраняем корзину
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка создания корзины: {ex.Message}\n\nInner: {ex.InnerException?.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Проверяем, есть ли уже в корзине
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

                    try
                    {
                        context.SaveChanges(); // Сохраняем изменения
                        

                        MessageBox.Show($"Услуга \"{service.Name}\" добавлена в корзину!",
                            "Успешно",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        // Ловим ошибки валидации сущностей
                        var errorMessages = new List<string>();
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                errorMessages.Add($"Свойство: {validationError.PropertyName} Ошибка: {validationError.ErrorMessage}");
                            }
                        }
                        var fullErrorMessage = string.Join("\n", errorMessages);
                        MessageBox.Show($"Ошибка валидации при добавлении в корзину:\n{fullErrorMessage}",
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateException updateEx)
                    {
                        // Ловим ошибки обновления БД
                        var innerExceptionMessage = updateEx.InnerException?.Message ?? "Нет дополнительной информации";
                        MessageBox.Show($"Ошибка обновления базы данных:\n{updateEx.Message}\n\nВнутренняя ошибка:\n{innerExceptionMessage}",
                            "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Выводим полную информацию об ошибке
                var errorMessage = $"Ошибка при добавлении в корзину:\n{ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nВнутренняя ошибка:\n{ex.InnerException.Message}";

                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $"\n\nДетали:\n{ex.InnerException.InnerException.Message}";
                    }
                }

                MessageBox.Show(errorMessage,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        // ИСПРАВЛЕННЫЙ МЕТОД ДЛЯ ОБНОВЛЕНИЯ СЧЕТЧИКА КОРЗИНЫ
        

        // Метод для обновления видимости элементов навигации
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

        // Обработчики событий
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshData();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshData();
        }

        private void PriceFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                RefreshData();
            };
            timer.Start();
        }

        private void CheckBoxFilter_Changed(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void CategoryCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void btnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            txtMinPrice.Text = "0";
            txtMaxPrice.Text = "5000";
            chkExpensive.IsChecked = false;
            cmbSort.SelectedIndex = 0;

            foreach (CheckBox checkBox in categoryPanel.Children)
            {
                checkBox.IsChecked = false;
            }

            RefreshData();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление новой услуги", "Добавить");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Редактирование услуги", "Редактировать");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную услугу?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Услуга удалена", "Удаление");
                RefreshData();
            }
        }

        private void MyOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            // TODO: Переход на страницу моих заказов
            NavigationService.Navigate(new MyOrdersPage());
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            // TODO: Переход на страницу профиля
            NavigationService.Navigate(new ProfilePage());
        }

        private void CartIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Session.IsGuest)
            {
                ShowGuestWarning();
                return;
            }

            // TODO: Переход на страницу корзины
            NavigationService.Navigate(new CartPage());
        }
    }
}