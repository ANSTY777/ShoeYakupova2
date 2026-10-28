using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShoeYakupova2
{
    public partial class SizeSelectWindow : Window
    {
        public int SelectedCommodityPositionID { get; private set; }
        public int SelectedQuantity { get; private set; }
        private Product _product;
        private int _qty = 1;

        public SizeSelectWindow(Product product)
        {
            InitializeComponent();
            _product = product;

            ProductNameText.Text = product.ProductName;
            LoadSizes();
            UpdateQtyText();
        }

        // Загружаю только те размеры, которые есть в наличии (QuantityInStock > 0)
        private void LoadSizes()
        {
            using (var context = new ShoeYakupovaEntities1())
            {
                var list = context.CommodityPosition
                    .ToList()
                    .Where(c => c.ProductID == _product.ProductID && c.QuantityInStock > 0)
                    .Select(c => new SizeItem
                    {
                        CommodityPositionID = c.CommodityPositionID,
                        SizeValue = c.Size != null ? c.Size.SizeValue : 0,
                        InStock = c.QuantityInStock
                    })
                    .ToList();

                SizeComboBox.ItemsSource = list;
                SizeComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateQtyText()
        {
            QtyText.Text = _qty.ToString();
        }

        private int GetSelectedInStock()
        {
            var item = SizeComboBox.SelectedItem as SizeItem;
            return item != null ? item.InStock : 0;
        }

        // Если пользователь переключил размер, а у него остаток меньше — сбрасываю количество
        private void SizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int inStock = GetSelectedInStock();
            if (inStock > 0 && _qty > inStock)
            {
                _qty = inStock;
                UpdateQtyText();
            }
            else if (inStock == 0)
            {
                _qty = 1;
                UpdateQtyText();
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            // Не даю добавить больше, чем есть на складе
            if (_qty + 1 > GetSelectedInStock()) return;

            _qty++;
            UpdateQtyText();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_qty <= 1) return;

            _qty--;
            UpdateQtyText();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var item = SizeComboBox.SelectedItem as SizeItem;
            if (item == null)
            {
                MessageBox.Show("Выберите размер.");
                return;
            }

            // Сохраняю выбор, чтобы ProductPage или OrderWindow могли его забрать
            SelectedCommodityPositionID = item.CommodityPositionID;
            SelectedQuantity = _qty;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Класс-обертка для ComboBox
    public class SizeItem
    {
        public int CommodityPositionID { get; set; }
        public decimal SizeValue { get; set; }
        public int InStock { get; set; }

        public string DisplayText
        {
            get { return SizeValue + " (в наличии: " + InStock + " шт.)"; }
        }
    }
}