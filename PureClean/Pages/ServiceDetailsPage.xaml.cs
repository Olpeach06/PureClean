using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PureClean.AppData;

namespace PureClean.Pages
{
    public partial class ServiceDetailsPage : Page
    {
        private Entities _context = new Entities();
        private int _serviceId;
        private Services _service;

        // Конструктор для перехода с параметром
        public ServiceDetailsPage(int serviceId)
        {
            InitializeComponent();
            _serviceId = serviceId;
            Loaded += Page_Loaded;
        }

        // Конструктор по умолчанию (для дизайнера)
        public ServiceDetailsPage() : this(0)
        {
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_serviceId == 0)
                {
                    // Если ID не передан, берем первую услугу
                    _service = _context.Services.FirstOrDefault();
                    if (_service == null)
                    {
                        MessageBox.Show("Услуги не найдены", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        NavigationService.GoBack();
                        return;
                    }
                    _serviceId = _service.ServiceID;
                }
                else
                {
                    _service = _context.Services.FirstOrDefault(s => s.ServiceID == _serviceId);
                }

                if (_service == null)
                {
                    MessageBox.Show("Услуга не найдена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService.GoBack();
                    return;
                }

                LoadServiceData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServiceData()
        {
            try
            {
                // Основная информация
                serviceNameText.Text = _service.Name;

                // Категория
                var category = _context.ServiceCategories
                    .FirstOrDefault(c => c.CategoryID == _service.CategoryID);
                categoryText.Text = category != null ? category.Name : "Без категории";

                // Описание
                descriptionText.Text = !string.IsNullOrEmpty(_service.Description)
                    ? _service.Description
                    : "Описание отсутствует";

                // Цены и скидка
                if (_service.OldPrice.HasValue && _service.OldPrice.Value > 0 && _service.DiscountPercent.HasValue)
                {
                    oldPriceText.Text = $"{_service.OldPrice.Value:N0} ₽";
                    discountText.Text = $"-{_service.DiscountPercent.Value}%";
                    discountBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    oldPriceText.Text = "";
                    discountBorder.Visibility = Visibility.Collapsed;
                }

                currentPriceText.Text = $"{_service.FinalPrice:N0} ₽";

                // Характеристики
                executionTimeText.Text = $"{_service.ExecutionTimeHours} часов";
                categoryDetailText.Text = category != null ? category.Name : "Не указана";

                // Статистика
                ordersCountText.Text = GetOrdersCount(_serviceId).ToString();
                ratingText.Text = GetServiceRating(_serviceId);
                addedDateText.Text = _service.ServiceID.ToString();

                // Инструкции по уходу
                careInstructionsText.Text = GetCareInstructions(_service.CategoryID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetOrdersCount(int serviceId)
        {
            try
            {
                return _context.OrderItems
                    .Count(oi => oi.ServiceID == serviceId);
            }
            catch
            {
                return 0;
            }
        }

        private string GetServiceRating(int serviceId)
        {
            try
            {
                // Здесь можно реализовать расчет рейтинга
                // Пока вернем фиктивное значение
                return "4.8 ★";
            }
            catch
            {
                return "Нет оценок";
            }
        }

        private string GetCareInstructions(int? categoryId)
        {
            if (categoryId == null)
                return "• Инструкции по уходу отсутствуют";

            try
            {
                // Здесь можно получить инструкции из таблицы ItemTypes
                // Пока вернем общие инструкции
                return "• Только профессиональная химчистка\n" +
                       "• Избегать попадания воды\n" +
                       "• Хранить в чехле в сухом месте\n" +
                       "• Не гладить при высокой температуре";
            }
            catch
            {
                return "• Инструкции по уходу отсутствуют";
            }
        }

        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            AddToCartFromDetails();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void AddToCartFromDetails()
        {
            try
            {
                if (Session.IsGuest)
                {
                    ShowGuestWarning();
                    return;
                }

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
                    // Находим текущего пользователя
                    var user = context.Users.FirstOrDefault(u => u.UserID == Session.UserID);
                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Ищем клиента по email или phone
                    var client = context.Clients.FirstOrDefault(c =>
                        c.Email == user.Email ||
                        (!string.IsNullOrEmpty(user.Phone) && c.Phone == user.Phone));

                    // Если клиент не найден, создаем нового
                    if (client == null)
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

                    // Находим или создаем корзину
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

                    // Проверяем, есть ли уже в корзине
                    var existingItem = context.CartItems
                        .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ServiceID == _serviceId);

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
                            ServiceID = _serviceId,
                            Quantity = 1,
                            AddedDate = DateTime.Now
                        };
                        context.CartItems.Add(cartItem);
                    }

                    cart.LastUpdated = DateTime.Now;
                    context.SaveChanges();

                    // Обновляем счетчик в сессии
                    Session.CartItemCount = Session.CartItemCount != null ? Session.CartItemCount + 1 : 1;

                    MessageBox.Show($"Услуга \"{_service.Name}\" добавлена в корзину!",
                        "Успешно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в корзину: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}