using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShoeYakupova2
{
    public partial class ProductSelectWindow : Window
    {
        // Сюда сохраняю выбранный товар, чтобы вызывающее окно (OrderWindow) 
        // могло его прочитать после закрытия этого диалога
        public Product SelectedProduct { get; private set; }

        public ProductSelectWindow()
        {
            InitializeComponent();

            using (var context = new ShoeYakupovaEntities1())
            {
                // Загружаю все товары для выбора
                ProductListView.ItemsSource = context.Product.ToList();
            }
        }

        private void ProductListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrent();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectCurrent();
        }
        private void TBSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            using (var context = new ShoeYakupovaEntities1())
            {
                var products = context.Product.ToList();

                if (!string.IsNullOrWhiteSpace(TBSearch.Text))
                {
                    string search = TBSearch.Text.ToLower();
                    products = products.Where(p => p.ProductName.ToLower().Contains(search)).ToList();
                }

                ProductListView.ItemsSource = products;
            }
        }
        private void SelectCurrent()
        {
            var product = ProductListView.SelectedItem as Product;
            if (product == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            SelectedProduct = product;
            // DialogResult = true закрывает окно и возвращает в вызывающий код, 
            // что пользователь сделал выбор
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}