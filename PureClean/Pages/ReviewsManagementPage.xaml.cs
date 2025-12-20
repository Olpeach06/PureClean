using PureClean.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PureClean.Pages
{
    public partial class ReviewsManagementPage : Page
    {
        private Entities _context = new Entities();
        private List<ReviewViewModel> _allReviews = new List<ReviewViewModel>();

        public ReviewsManagementPage()
        {
            InitializeComponent();
            Loaded += ReviewsManagementPage_Loaded;
        }

        private void ReviewsManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadReviews();
        }

        private void LoadReviews()
        {
            try
            {
                // Загружаем отзывы и связанные данные отдельными запросами
                var reviews = _context.Reviews
                    .ToList()  // Сначала загружаем отзывы
                    .Select(r => new ReviewViewModel
                    {
                        ReviewID = r.ReviewID,
                        OrderID = r.OrderID,
                        ClientName = GetClientName(r.OrderID),
                        Rating = r.Rating,
                        Comment = r.Comment ?? "Без комментария",
                        CreatedDate = r.CreatedDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана",
                        IsHidden = r.Comment != null && (r.Comment.Contains("[СКРЫТО]") ||
                                   ContainsBadWords(r.Comment)),
                        Status = r.Comment != null && (r.Comment.Contains("[СКРЫТО]") ||
                                   ContainsBadWords(r.Comment)) ? "Скрыт" : "Активен",
                        StatusColor = r.Comment != null && (r.Comment.Contains("[СКРЫТО]") ||
                                   ContainsBadWords(r.Comment)) ?
                                   new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green)
                    })
                    .OrderByDescending(r => r.CreatedDate)
                    .ToList();

                _allReviews = reviews;
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отзывов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetClientName(int orderId)
        {
            try
            {
                // Получаем заказ и клиента
                var order = _context.Orders
                    .Include("Clients")  // Используем строку для Include
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order?.Clients != null)
                {
                    return $"{order.Clients.LastName} {order.Clients.FirstName}";
                }
                return "Неизвестный клиент";
            }
            catch
            {
                return "Неизвестный клиент";
            }
        }

        private bool ContainsBadWords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string lowerText = text.ToLower();

            // Список нецензурных слов для проверки (можно расширить)
            string[] badWords = new[]
            {
                "мат1", "мат2", "мат3", // Замените на реальные плохие слова
                "оскорбление", "брань", "черт", "блин"
            };

            foreach (var word in badWords)
            {
                if (lowerText.Contains(word))
                    return true;
            }

            return false;
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allReviews == null || !_allReviews.Any())
                {
                    reviewsGrid.ItemsSource = new List<ReviewViewModel>();
                    return;
                }

                var filtered = _allReviews.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(r =>
                        r.ClientName.ToLower().Contains(searchText) ||
                        r.Comment.ToLower().Contains(searchText) ||
                        r.OrderID.ToString().Contains(searchText) ||
                        r.ReviewID.ToString().Contains(searchText));
                }

                // Фильтр по рейтингу
                var selectedRatingItem = cmbRating.SelectedItem as ComboBoxItem;
                if (selectedRatingItem != null && selectedRatingItem.Tag != null && !string.IsNullOrEmpty(selectedRatingItem.Tag.ToString()))
                {
                    int selectedRating = Convert.ToInt32(selectedRatingItem.Tag);
                    filtered = filtered.Where(r => r.Rating == selectedRating);
                }

                reviewsGrid.ItemsSource = filtered.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_allReviews == null || !_allReviews.Any())
            {
                txtTotalReviews.Text = "0";
                txtAverageRating.Text = "0.0";
                txtHiddenReviews.Text = "0";
                return;
            }

            txtTotalReviews.Text = _allReviews.Count.ToString();

            if (_allReviews.Any(r => r.Rating > 0))
            {
                double averageRating = _allReviews.Average(r => r.Rating);
                txtAverageRating.Text = averageRating.ToString("N1");
            }
            else
            {
                txtAverageRating.Text = "0.0";
            }

            int hiddenCount = _allReviews.Count(r => r.IsHidden);
            txtHiddenReviews.Text = hiddenCount.ToString();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int reviewId = Convert.ToInt32(button.Tag);

                // Получаем данные отзыва
                var review = _context.Reviews.FirstOrDefault(r => r.ReviewID == reviewId);

                if (review == null)
                {
                    MessageBox.Show("Отзыв не найден!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string clientName = GetClientName(review.OrderID);
                string ratingStars = new string('⭐', review.Rating);

                string reviewInfo = $"⭐ Отзыв №{review.ReviewID}\n\n" +
                                   $"📋 Заказ №{review.OrderID}\n" +
                                   $"👤 Клиент: {clientName}\n" +
                                   $"📅 Дата: {review.CreatedDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана"}\n" +
                                   $"⭐ Рейтинг: {ratingStars} ({review.Rating}/5)\n\n" +
                                   $"💬 Комментарий:\n{review.Comment ?? "Без комментария"}\n\n" +
                                   $"{(review.Comment != null && review.Comment.Contains("[СКРЫТО]") ? "⚠️ Этот отзыв был скрыт администратором" : "")}";

                MessageBox.Show(reviewInfo, "Детали отзыва",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null || button.Tag == null) return;

                int reviewId = Convert.ToInt32(button.Tag);

                var window = new EditReviewWindow(reviewId, _context);
                window.Owner = Window.GetWindow(this);

                if (window.ShowDialog() == true)
                {
                    // Обновляем данные
                    _context.Dispose();
                    _context = new Entities();
                    LoadReviews();

                    MessageBox.Show("Отзыв успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования отзыва: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbRating.SelectedIndex = 0;
            ApplyFilters();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbRating_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

        // ViewModel для отображения отзывов
        public class ReviewViewModel
        {
            public int ReviewID { get; set; }
            public int OrderID { get; set; }
            public string ClientName { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public string CreatedDate { get; set; }
            public bool IsHidden { get; set; }
            public string Status { get; set; }
            public SolidColorBrush StatusColor { get; set; }

            public string RatingStars => new string('⭐', Rating);
        }
    }
}