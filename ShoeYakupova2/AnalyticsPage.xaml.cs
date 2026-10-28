using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace ShoeYakupova2
{
    public partial class AnalyticsPage : Page
    {
        private Client _currentUser;
        private int UserRoles;

        public AnalyticsPage(Client user, int userRoles)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = userRoles;

            // Ставлю текущий месяц по умолчанию
            DateTime today = DateTime.Today;
            DateTime firstDay = new DateTime(today.Year, today.Month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

            StartDatePicker.SelectedDate = firstDay;
            EndDatePicker.SelectedDate = lastDay;

            Recalculate();
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            Recalculate();
        }

        private void Recalculate()
        {
            // Защита от вызовов пока окно грузится
            if (StartDatePicker == null || EndDatePicker == null) return;
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null) return;

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            if (start > end)
            {
                ErrorText.Text = "Дата начала не может быть позже даты окончания.";
                TotalOrdersText.Text = "Всего заказов: 0";
                TotalSumText.Text = "Общая сумма продаж: 0,00 ₽";
                TopProductsListView.ItemsSource = null;
                return;
            }

            ErrorText.Text = "";

            using (var context = new ShoeYakupovaEntities1())
            {
                // Фильтрую в БД, чтобы не грузить лишнее
                var orders = context.Order
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .ToList();

                TotalOrdersText.Text = "Всего заказов: " + orders.Count;

                // Считаю сумму по цене на момент заказа
                decimal total = 0;
                foreach (var order in orders)
                {
                    if (order.OrderItem == null) continue;
                    foreach (var item in order.OrderItem)
                        total += item.Quantity * item.PriceAtOrderTime;
                }
                TotalSumText.Text = "Общая сумма продаж: " + total.ToString("N2") + " ₽";

                var items = new List<OrderItem>();
                foreach (var order in orders)
                {
                    if (order.OrderItem == null) continue;
                    items.AddRange(order.OrderItem);
                }

                // Группирую по ID, суммирую кол-во, беру топ-5
                var top = items
                    .Where(i => i.CommodityPosition != null && i.CommodityPosition.Product != null)
                    .GroupBy(i => i.CommodityPosition.Product.ProductID)
                    .Select(g => new
                    {
                        ProductName = g.First().CommodityPosition.Product.ProductName,
                        ManufacturerName = g.First().CommodityPosition.Product.Manufacturer != null
                                            ? g.First().CommodityPosition.Product.Manufacturer.ManufacturerName
                                            : "Не указан",
                        TotalPairs = g.Sum(i => i.Quantity)
                    })
                    .OrderByDescending(x => x.TotalPairs)
                    .Take(5)
                    .ToList();

                TopProductsListView.ItemsSource = top;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период.");
                return;
            }

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания.");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "PDF файл (*.pdf)|*.pdf";
            dialog.FileName = "Отчёт_" + start.ToString("dd.MM.yyyy") + "_" + end.ToString("dd.MM.yyyy") + ".pdf";

            if (dialog.ShowDialog() != true) return;
            string path = dialog.FileName;

            using (var context = new ShoeYakupovaEntities1())
            {
                var orders = context.Order
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .ToList();

                decimal total = 0;
                var allItems = new List<OrderItem>();
                foreach (var order in orders)
                {
                    if (order.OrderItem == null) continue;
                    foreach (var item in order.OrderItem)
                    {
                        total += item.Quantity * item.PriceAtOrderTime;
                        allItems.Add(item);
                    }
                }

                var top = allItems
                    .Where(i => i.CommodityPosition != null && i.CommodityPosition.Product != null)
                    .GroupBy(i => i.CommodityPosition.Product.ProductID)
                    .Select(g => new
                    {
                        ProductName = g.First().CommodityPosition.Product.ProductName,
                        ManufacturerName = g.First().CommodityPosition.Product.Manufacturer != null
                                            ? g.First().CommodityPosition.Product.Manufacturer.ManufacturerName
                                            : "Не указан",
                        TotalPairs = g.Sum(i => i.Quantity)
                    })
                    .OrderByDescending(x => x.TotalPairs)
                    .Take(5)
                    .ToList();

                // Подключаю Arial для кириллицы в PDF
                BaseFont baseFont = BaseFont.CreateFont(
                    "C:\\Windows\\Fonts\\arial.ttf",
                    BaseFont.IDENTITY_H,
                    BaseFont.EMBEDDED);

                Font titleFont = new Font(baseFont, 18, Font.BOLD);
                Font boldFont = new Font(baseFont, 12, Font.BOLD);
                Font normalFont = new Font(baseFont, 12);

                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
                doc.Open();

                // Пишу полный путь, т.к. есть конфликт с Paragraph из WPF
                doc.Add(new iTextSharp.text.Paragraph("Отчёт по продажам", titleFont));
                doc.Add(new iTextSharp.text.Paragraph("Период: " + start.ToString("dd.MM.yyyy") + " — " + end.ToString("dd.MM.yyyy"), normalFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                doc.Add(new iTextSharp.text.Paragraph("Всего заказов: " + orders.Count, boldFont));
                doc.Add(new iTextSharp.text.Paragraph("Общая сумма продаж: " + total.ToString("N2") + " ₽", boldFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                doc.Add(new iTextSharp.text.Paragraph("Топ-5 моделей по проданным парам", boldFont));
                doc.Add(new iTextSharp.text.Paragraph(" ", normalFont));

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.AddCell(new Phrase("Модель", boldFont));
                table.AddCell(new Phrase("Производитель", boldFont));
                table.AddCell(new Phrase("Пар продано", boldFont));

                foreach (var item in top)
                {
                    table.AddCell(new Phrase(item.ProductName, normalFont));
                    table.AddCell(new Phrase(item.ManufacturerName, normalFont));
                    table.AddCell(new Phrase(item.TotalPairs.ToString(), normalFont));
                }

                doc.Add(table);
                doc.Close();

                MessageBox.Show("Отчёт сохранён: " + path);
            }
        }
    }
}