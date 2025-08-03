using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using Newtonsoft.Json;
using SpAnalyzerTool.Models;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using Size = System.Windows.Size;


namespace SpAnalyzerTool.Helper
{
    /// <summary>
    /// مسؤول عن تحميل كلمات الاقتراح من ملف JSON وتفعيل نافذة الإكمال التلقائي في AvalonEdit.
    /// </summary>
    public class clsAutoCompleteProvider : IBackgroundRenderer
    {
        private DispatcherTimer? _closeTimer;
        private readonly TextArea _textArea;
        private  ListBox _suggestionList;
        private  Popup? _popup;
        private List<string> _allSuggestions = new();
        public KnownLayer Layer => KnownLayer.Selection;

        public clsAutoCompleteProvider(TextEditor textArea, string jsonPath)
        {
            LoadSuggestionsFromJson(jsonPath);
            
            _textArea = textArea.TextArea;

            InitializeSuggestionUI();
            ApplySyntaxHighlighting(textArea); // تلوين النصوص باستخدام XSHD

            _textArea.TextView.BackgroundRenderers.Add(this);
            _textArea.TextEntered += TextArea_TextEntered;
            _textArea.PreviewKeyDown += TextArea_PreviewKeyDown;
          

            //احداث القائمة المنسدلة
            _suggestionList!.PreviewKeyDown += SuggestionList_PreviewKeyDown;
            _suggestionList.MouseDoubleClick += SuggestionList_MouseDoubleClick;
            _suggestionList.PreviewMouseLeftButtonUp += SuggestionList_MouseClick;

        }

        /// <summary>
        /// تحديث ملف JSON الخاص بالإكمال التلقائي بناءً على الإجراءات والجداول الموجودة في قاعدة البيانات.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static async Task UpdateAutoCompleteJsonAsync(string connection)
        {
            // استخراج القوائم الجديدة من قاعدة البيانات
            var procedures = await clsDatabaseHelper.GetAllStoredProceduresAsync(connection);
            var tables = await clsDatabaseHelper.GetAllTableNamesAsync(connection);

            // قراءة الملف القديم للاحتفاظ بالكلمات المحجوزة القديمة
            var existing = SettingsHelper.Load<SuggestionModel>("SettingesFiles\\autocomplete.json");
            var keywords = existing?.keywords ?? new List<string>(); // قائمة ثابتة لا تتغير



            // إنشاء النموذج الجديد
            var saveInfo = new SuggestionModel
            {
                procedures = procedures,
                tables = tables,
                keywords = keywords // نُعيد استخدامها كما هي
            };

            // حفظ الملف مع الاحتفاظ بالكلمات المحجوزة
            SettingsHelper.Save("SettingesFiles\\autocomplete.json", saveInfo);
        }

        private void InitializeSuggestionUI()
        {
            _suggestionList = new ListBox
            {
                Width = 300,  // زيادة العرض لاستيعاب نصوص أطول
                MaxHeight = 300,
                FontSize = 14,
                Background = Brushes.WhiteSmoke,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                IsTabStop = true,
                Focusable = true,
            };

            _popup = new Popup
            {
                PlacementTarget = _textArea,
                Placement = PlacementMode.RelativePoint,
                StaysOpen = false,
                Child = _suggestionList,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Slide
            };
        }

        private void ResetAutoCloseTimer()
        {
            _closeTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            _closeTimer.Tick -= CloseTimer_Tick;
            _closeTimer.Tick += CloseTimer_Tick;

            _closeTimer.Stop();
            _closeTimer.Start();
        }

        private void LoadSuggestionsFromJson(string path)
        {
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SuggestionModel>(json);
            _allSuggestions = data?.GetAllSuggestions() ?? new List<string>();

        }

        private void InsertSuggestion(string suggestion)
        {
            var caretOffset = _textArea.Caret.Offset;
            var word = GetCurrentWord();
            _textArea.Document.Replace(caretOffset - word.Length, word.Length, suggestion);
            _popup.IsOpen = false;
        }

        private string GetCurrentWord()
        {
            var offset = _textArea.Caret.Offset;
            var document = _textArea.Document;

            if (document == null || offset == 0)
                return "";

            int start = offset - 1;
            while (start > 0 && (char.IsLetterOrDigit(document.GetCharAt(start)) || document.GetCharAt(start) == '_'))
                start--;

            int end = offset;
            while (end < document.TextLength && (char.IsLetterOrDigit(document.GetCharAt(end)) || document.GetCharAt(end) == '_'))
                end++;

            return document.GetText(start, end - start).Trim();
        }

       
        #region Highlighting تلوين النص
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView?.VisualLinesValid != true)
                return;

            SolidColorBrush highlightBrush = new SolidColorBrush(Color.FromArgb(80, 0, 120, 255)); // أزرق شفاف ناعم
            highlightBrush.Freeze();

            foreach (var line in textView.VisualLines)
            {
                if (line.FirstDocumentLine.LineNumber != 1)
                    continue;

                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line.FirstDocumentLine))
                {
                    drawingContext.DrawRectangle(highlightBrush, null, new Rect(rect.Location, new Size(rect.Width, rect.Height)));
                }

                break; 
            }
        }
        
        private void ApplySyntaxHighlighting(TextEditor editor)
        {
            try
            {
                using var stream = new FileStream("SettingesFiles\\SQLSyntax.xshd", FileMode.Open);
                using var reader = new XmlTextReader(stream);

                if (reader == null || reader.NameTable == null)
                {
                    MessageBox.Show("تعذر تحميل ملف التلوين. تأكد من وجود الملف في المسار الصحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                var xshd = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("SQL", new[] { ".sql" }, xshd);
                editor.SyntaxHighlighting = xshd;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تلوين النص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


        private void ShowSuggestions(string word)
        {
            var matches = _allSuggestions
                .Where(k => k.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _suggestionList.ItemsSource = matches;
            if (matches.Count > 0)
            {
                _suggestionList.SelectedIndex = 0;
                _popup.IsOpen = true;
            }
            else
            {
                _popup.IsOpen = false;
            }
        }


        #region Events

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer?.Stop();
            _popup.IsOpen = false;
            _textArea.Focus();
        }

        private void SuggestionList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Tab) && _suggestionList.SelectedItem is string selected)
            {
                InsertSuggestion(selected);
                _popup.IsOpen = false;
                e.Handled = true;

                // إعادة التركيز إلى TextEditor بعد الإدراج
                _textArea.Focus();
            }
        }

        private void SuggestionList_MouseClick(object sender, MouseButtonEventArgs e)
        {
            if (_suggestionList.SelectedItem is string selected)
                InsertSuggestion(selected);
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            var currentWord = GetCurrentWord();

            if (string.IsNullOrWhiteSpace(currentWord) || currentWord.Length < 2)
            {
                _popup!.IsOpen = false;
                return;
            }

            var matches = _allSuggestions
                .Where(x => x.StartsWith(currentWord, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x)
                .ToList();

            if (matches.Count == 0)
            {
                _popup!.IsOpen = false;
                return;
            }

            _suggestionList.ItemsSource = matches;
            _suggestionList.SelectedIndex = 0;

            var position = _textArea.Caret.CalculateCaretRectangle();
            _popup!.HorizontalOffset = position.Right;
            _popup.VerticalOffset = position.Bottom;
            _popup.IsOpen = true;

            ResetAutoCloseTimer();

        }

        private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_suggestionList.SelectedItem is string selected)
                InsertSuggestion(selected);
        }

        private void TextArea_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_popup.IsOpen && e.Key != Key.Back && e.Key != Key.Delete)
                return;

            switch (e.Key)
            {
                case Key.Down:
                    if (_suggestionList.Items.Count > 0)
                    {
                        _suggestionList.Focus();
                        _suggestionList.SelectedIndex = (_suggestionList.SelectedIndex + 1) % _suggestionList.Items.Count;
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    if (_suggestionList.Items.Count > 0)
                    {
                        _suggestionList.Focus();
                        _suggestionList.SelectedIndex = (_suggestionList.SelectedIndex - 1 + _suggestionList.Items.Count) % _suggestionList.Items.Count;
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                case Key.Tab:
                    if (_suggestionList.SelectedItem is string selectedItem)
                    {
                        InsertSuggestion(selectedItem);
                        _popup.IsOpen = false;
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    _popup.IsOpen = false;
                    e.Handled = true;
                    break;

                case Key.Back:
                case Key.Delete:
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        var word = GetCurrentWord();
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            ShowSuggestions(word);
                            _popup.IsOpen = true;
                        }
                        else
                        {
                            _popup.IsOpen = false;
                        }
                    }, DispatcherPriority.Background);
                    break;
            }
        }

        #endregion


    }
}
