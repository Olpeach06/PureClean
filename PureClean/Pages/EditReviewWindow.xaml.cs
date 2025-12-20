using PureClean.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace PureClean.Pages
{
    public partial class EditReviewWindow : Window
    {
        private Entities _context;
        private int _reviewId;
        private Reviews _review;
        private string _originalComment;
        private string _clientName;

        public EditReviewWindow(int reviewId, Entities context)
        {
            InitializeComponent();
            _context = context;
            _reviewId = reviewId;
            LoadReviewData();
        }

        private void LoadReviewData()
        {
            _review = _context.Reviews.FirstOrDefault(r => r.ReviewID == _reviewId);

            if (_review != null)
            {
                Title = $"Редактирование отзыва №{_review.ReviewID}";

                // Получаем информацию о клиенте
                var order = _context.Orders
                    .Include("Clients")  // Используем строку для Include
                    .FirstOrDefault(o => o.OrderID == _review.OrderID);

                _clientName = order?.Clients != null ?
                    $"{order.Clients.LastName} {order.Clients.FirstName}" :
                    "Неизвестный клиент";

                // Устанавливаем DataContext для привязки
                DataContext = new
                {
                    OrderInfo = $"Заказ №{_review.OrderID}, Клиент: {_clientName}",
                    Rating = _review.Rating,
                    RatingStars = new string('⭐', _review.Rating)
                };

                _originalComment = _review.Comment ?? "";
                txtComment.Text = _originalComment;

                // Проверяем, скрыт ли отзыв
                if (_originalComment.Contains("[СКРЫТО]"))
                {
                    chkHideReview.IsChecked = true;
                    // Убираем маркер из текста для редактирования
                    txtComment.Text = _originalComment.Replace("[СКРЫТО] ", "").Replace("[СКРЫТО]", "");
                }

                // Проверяем наличие плохих слов
                if (ContainsBadWords(_originalComment))
                {
                    chkCensorBadWords.IsChecked = true;
                }
            }
        }

        private bool ContainsBadWords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string lowerText = text.ToLower();

            // Список нецензурных слов для проверки
            string[] badWords = new[]
            {
                "мат1", "мат2", "мат3", // Замените на реальные плохие слова
                "оскорбление", "брань", "ругательство", "черт", "блин", "черт возьми"
            };

            foreach (var word in badWords)
            {
                if (lowerText.Contains(word))
                    return true;
            }

            return false;
        }

        private string CensorBadWords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = text;

            // Список нецензурных слов для цензуры
            string[] badWords = new[]
            {
                "мат1", "мат2", "мат3", // Замените на реальные плохие слова
                "оскорбление", "брань", "ругательство", "черт", "блин", "черт возьми"
            };

            foreach (var word in badWords)
            {
                // Заменяем каждое плохое слово на звездочки
                string pattern = @"\b" + Regex.Escape(word) + @"\b";
                result = Regex.Replace(result, pattern, new string('*', word.Length), RegexOptions.IgnoreCase);
            }

            return result;
        }

        private void chkHideReview_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtComment.Text))
            {
                txtComment.Text = "[СКРЫТО] " + txtComment.Text.Replace("[СКРЫТО] ", "").Replace("[СКРЫТО]", "");
            }
        }

        private void chkHideReview_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtComment.Text))
            {
                txtComment.Text = txtComment.Text.Replace("[СКРЫТО] ", "").Replace("[СКРЫТО]", "");
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_review != null)
                {
                    string newComment = txtComment.Text.Trim();

                    // Проверяем, изменился ли комментарий
                    string cleanOriginal = _originalComment.Replace("[СКРЫТО] ", "").Replace("[СКРЫТО]", "").Trim();

                    if (newComment != cleanOriginal)
                    {
                        // Применяем цензуру если нужно
                        if (chkCensorBadWords.IsChecked == true && ContainsBadWords(newComment))
                        {
                            newComment = CensorBadWords(newComment);
                        }

                        // Добавляем маркер скрытия если нужно
                        if (chkHideReview.IsChecked == true && !newComment.Contains("[СКРЫТО]"))
                        {
                            newComment = "[СКРЫТО] " + newComment;
                        }

                        _review.Comment = string.IsNullOrWhiteSpace(newComment) ? null : newComment;

                        _context.SaveChanges();
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Комментарий не был изменен.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
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