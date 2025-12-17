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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PureClean.Pages
{
    /// <summary>
    /// Логика взаимодействия для CatalogPage.xaml
    /// </summary>
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
                // Пример загрузки данных - адаптируйте под вашу структуру
                LoadServices();
                LoadServiceCategories();

                // Настройка фильтров и сортировки
                SetupFilters();

                // Загружаем начальные данные
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServices()
        {
            // Замените на ваш реальный запрос
            var services = _context.Services.ToList();
            // Привязка данных к UI элементам будет в RefreshData()
        }

        private void LoadServiceCategories()
        {
            try
            {
                // Замените на ваш реальный запрос
                var categories = _context.ServiceCategories.ToList();

                // Очищаем панель категорий
                categoryPanel.Children.Clear();

                // Добавляем CheckBox для каждой категории
                foreach (var category in categories)
                {
                    var checkBox = new CheckBox
                    {
                        Content = category.Name, // Предполагаем, что есть свойство Name
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 5),
                        Tag = category.CategoryID // Сохраняем ID категории в Tag
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
                // Обновление данных с учетом фильтров и поиска
                var filteredData = ApplyFilters();

                // Очистка панели услуг
                servicesPanel.Children.Clear();

                // Добавление отфильтрованных услуг
                foreach (var service in filteredData)
                {
                    // Создание карточки услуги
                    var card = CreateServiceCard(service);
                    servicesPanel.Children.Add(card);
                }

                // Пример подсчета
                if (!filteredData.Any())
                {
                    // Добавление сообщения "Не найдено"
                    var noResultsText = new TextBlock
                    {
                        Text = "Услуги не найдены",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(20)
                    };
                    servicesPanel.Children.Add(noResultsText);
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

                // Применение текстового поиска
                if (!string.IsNullOrEmpty(txtSearch?.Text))
                {
                    var searchText = txtSearch.Text.ToLower();
                    result = result.Where(s =>
                        s.Name.ToLower().Contains(searchText) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchText)));
                }

                // Применение фильтра по цене
                if (int.TryParse(txtMinPrice.Text, out int minPrice))
                {
                    result = result.Where(s => s.BasePrice >= minPrice);
                }

                if (int.TryParse(txtMaxPrice.Text, out int maxPrice))
                {
                    result = result.Where(s => s.BasePrice <= maxPrice);
                }

                // Фильтр "Только дорогие"
                if (chkExpensive.IsChecked == true)
                {
                    result = result.Where(s => s.BasePrice > 1000);
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

                // Применение сортировки
                if (cmbSort?.SelectedIndex > 0)
                {
                    switch (cmbSort.SelectedIndex)
                    {
                        case 1: // По возрастанию цены
                            result = result.OrderBy(s => s.FinalPrice);
                            break;
                        case 2: // По убыванию цены
                            result = result.OrderByDescending(s => s.FinalPrice);
                            break;
                        case 3: // По названию
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
            // Создание карточки услуги
            var border = new Border
            {
                Width = 200,
                Height = 150,
                Background = Brushes.White,
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(10),
                Cursor = Cursors.Hand
            };

            // Эффект тени
            border.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                Opacity = 0.1,
                ShadowDepth = 2,
                Color = Colors.Black
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15)
            };

            // Название услуги
            var nameText = new TextBlock
            {
                Text = service.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                MaxWidth = 170
            };

            // Описание (если есть)
            if (!string.IsNullOrEmpty(service.Description))
            {
                var descriptionText = new TextBlock
                {
                    Text = service.Description.Length > 50
                        ? service.Description.Substring(0, 50) + "..."
                        : service.Description,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10),
                    MaxWidth = 170
                };
                stackPanel.Children.Add(descriptionText);
            }

            // Цена
            var priceText = new TextBlock
            {
                Text = $"{service.FinalPrice} руб.",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.DarkGreen,
                Margin = new Thickness(0, 5, 0, 0)
            };

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(priceText);

            // Обработчик клика по карточке
            border.MouseLeftButtonDown += (s, e) =>
            {
                // Здесь можно добавить логику при клике на услугу
                MessageBox.Show($"Выбрана услуга: {service.Name}\nЦена: {service.FinalPrice} руб.",
                    "Информация об услуге");
            };

            border.Child = stackPanel;
            return border;
        }

        // Обработчики событий
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
            // Добавляем небольшую задержку, чтобы не обновлять при каждом нажатии клавиши
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
            // Сброс текстового поиска
            txtSearch.Text = string.Empty;

            // Сброс ценовых фильтров
            txtMinPrice.Text = "0";
            txtMaxPrice.Text = "5000";

            // Сброс чекбокса "дорогие"
            chkExpensive.IsChecked = false;

            // Сброс сортировки
            cmbSort.SelectedIndex = 0;

            // Сброс категорий
            foreach (CheckBox checkBox in categoryPanel.Children)
            {
                checkBox.IsChecked = false;
            }

            RefreshData();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на страницу добавления
            MessageBox.Show("Добавление новой услуги", "Добавить");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора и навигация на редактирование
            MessageBox.Show("Редактирование услуги", "Редактировать");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика удаления с подтверждением
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
    }
}