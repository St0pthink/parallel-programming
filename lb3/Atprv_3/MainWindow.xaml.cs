using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using lb3.Task2;
using lb3.Task3;
using lb3.Task1;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Threading;
using System.Net.Http;

namespace lb3
{
    public partial class MainWindow : Window
    {
 
        // поля для задания 1
        private readonly ObservableCollection<string> _files = new();
        private readonly ObservableCollection<string> _resultView = new();
        private readonly TextMapReduceProcessor _processor = new();
        private CancellationTokenSource _cts;


        // поля для задания 2-3

        private readonly OrderProcessor _orderProcessor = new OrderProcessor();
        private readonly LinqPlinqComparer _linqPlinqComparer = new LinqPlinqComparer();

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация Task 1
            FilesListBox.ItemsSource = _files;
            ResultDictionaryListBox.ItemsSource = _resultView;
            LoadFilesButton.Click += LoadFilesButton_Click;

            // Инициализация Task 2
            OpenLogButton.Click += OpenLogButton_Click;
        }

        // Task1 Map and Reduce

        private async void LoadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите .txt файлы",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != true) return;

            _files.Clear();
            foreach (var p in dlg.FileNames) _files.Add(p);

            _resultView.Clear();
            StatusTextBlock1.Text = "Статус: подготовка...";
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // прогресс - обновляем статус
            var progress = new Progress<MapReduceProgress>(p =>
            {
                var pct = p.TotalBytes > 0 ? p.ProcessedBytes * 100.0 / p.TotalBytes : 0;
                StatusTextBlock1.Text = $"Статус: {pct:0.#}%  [{p.CurrentFile}]";
            });

            try
            {
                // запуск асинхронной обработки
                var result = await Task.Run(() =>
                    _processor.ProcessFiles(_files, degreeOfParallelism: Math.Max(1, Environment.ProcessorCount - 1),
                                                 progress: progress, ct: ct));

                // вывод
                foreach (var kv in result.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(1000))
                    _resultView.Add($"{kv.Key} — {kv.Value:n0}");

                StatusTextBlock1.Text = "Статус: готово";
            }
            catch (OperationCanceledException)
            {
                StatusTextBlock1.Text = "Статус: отменено";
            }
            catch (Exception ex)
            {
                StatusTextBlock1.Text = $"Ошибка: {ex.Message}";
            }
        }

        // Task 2 Микросервисы 

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            const string LogFileName = "task2log.txt";

            if (System.IO.File.Exists(LogFileName))
            {
                try
                {
                    // Открытие файла с помощью системного приложения по умолчанию
                    Process.Start(new ProcessStartInfo(LogFileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл лога: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Файл '{LogFileName}' не найден. Запустите обработку заказов.", "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            StartProcessingButton.IsEnabled = false;
            StatusTextBlock2.Text = "Статус: Инициализация данных...";
            StatisticsListBox.Items.Clear();
            var processor = _orderProcessor;

            try
            {
                StatusTextBlock2.Text = "Статус: Запущена обработка заказов (см. log.txt)...";
                // Запуск асинхронной операции в фоновом потоке
                var statistics = await Task.Run(() => processor.ProcessAllOrdersAsync());

                StatusTextBlock2.Text = "Статус: Обработка завершена.";

                StatisticsListBox.Items.Add($"Всего заказов: {statistics.TotalOrders}");
                StatisticsListBox.Items.Add($"Обработано полностью: {statistics.FullyProcessedOrders}");
                StatisticsListBox.Items.Add($"- Не хватило товара: {statistics.InventoryFailureOrders}");
                StatisticsListBox.Items.Add("Логи сохранены в log.txt");
            }
            catch (Exception ex)
            {
                StatusTextBlock2.Text = $"Ошибка: {ex.Message}";
                StatisticsListBox.Items.Add("Произошла ошибка при обработке.");
            }
            finally
            {
                StartProcessingButton.IsEnabled = true;
            }
        }

        // Task 3 LINQ vs PLINQ

        private async void StartComparisonButton_Click(object sender, RoutedEventArgs e)
        {
            StartComparisonButton.IsEnabled = false;
            StatusTextBlock3.Text = "Статус: Генерация данных...";
            ResultsListBox3.Items.Clear();

            // cчитывание и валидация параметров
            if (!int.TryParse(EmployeeCountTextBox.Text, out int employeeCount) || employeeCount <= 0)
            {
                StatusTextBlock3.Text = "Ошибка: Некорректное кол-во сотрудников.";
                StartComparisonButton.IsEnabled = true;
                return;
            }

            if (!int.TryParse(IdThresholdTextBox.Text, out int idThreshold) || idThreshold < 0)
            {
                StatusTextBlock3.Text = "Ошибка: Некорректный порог ID.";
                StartComparisonButton.IsEnabled = true;
                return;
            }

            if (!decimal.TryParse(AmountThresholdTextBox.Text, out decimal amountThreshold) || amountThreshold < 0)
            {
                StatusTextBlock3.Text = "Ошибка: Некорректный порог суммы.";
                StartComparisonButton.IsEnabled = true;
                return;
            }

            try
            {
                var comparer = _linqPlinqComparer;

                // генерация данных
                await Task.Run(() => comparer.GenerateData(employeeCount));

                StatusTextBlock3.Text = "Статус: Выполнение запросов...";

                // выполнение сравнения в фоновом потоке
                await Task.Run(() =>
                {
                    var results = comparer.ExecuteAllComparisons(idThreshold, amountThreshold);
                    Dispatcher.Invoke(() =>
                    {
                        ResultsListBox3.Items.Add($"Сгенерировано: {employeeCount} сотрудников и {comparer.TotalOrders} заказов.");
                        ResultsListBox3.Items.Add("---------------------------------");

                        foreach (var result in results)
                        {
                            ResultsListBox3.Items.Add(result.Title);
                            ResultsListBox3.Items.Add($"  LINQ: {result.LinqTimeMs:F3} мс (Найдено: {result.LinqResultCount})");
                            ResultsListBox3.Items.Add($"  PLINQ: {result.PlinqTimeMs:F3} мс (Найдено: {result.PlinqResultCount})");

                            string comparison;
                            if (result.PlinqTimeMs > 0 && result.LinqTimeMs > result.PlinqTimeMs)
                            {
                                comparison = $"PLINQ быстрее в {result.LinqTimeMs / result.PlinqTimeMs:F2} раз";
                            }
                            else if (result.LinqTimeMs > 0 && result.PlinqTimeMs > result.LinqTimeMs)
                            {
                                comparison = $"LINQ быстрее в {result.PlinqTimeMs / result.LinqTimeMs:F2} раз";
                            }
                            else
                            {
                                comparison = "Время выполнения слишком мало для сравнения или равно.";
                            }

                            ResultsListBox3.Items.Add($"  Сравнение: {comparison}");
                            ResultsListBox3.Items.Add("---");
                        }

                        StatusTextBlock3.Text = "Статус: Сравнение завершено.";
                    });
                });
            }
            catch (Exception ex)
            {
                StatusTextBlock3.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                StartComparisonButton.IsEnabled = true;
            }
        }
    }
}