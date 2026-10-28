using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoeYakupova2
{
    public partial class ProductPage : Page
    {
        private int PageSize = 10;
        private int currentPage = 1;
        private List<ProductDisplay> _filtredProductsDisplay;
        private Client _currentUser;
        private int UserRoles;
        private bool _isUpdating = false; // Флаг, чтобы пагинация не зацикливалась

        public ProductPage(Client user)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = user?.RoleID ?? 0; // 0 — значит гость

            if (user != null)
            {
                FIOTB.Text = "Вы авторизованы как " + user.FirstName + " " +
                             user.LastName + " " + user.Patronymic;

                switch (user.RoleID)
                {
                    case 1: RoleTB.Text = "Роль: Администратор"; break;
                    case 2: RoleTB.Text = "Роль: Менеджер"; break;
                    case 3: RoleTB.Text = "Роль: Авторизованный клиент"; break;
                }
            }
            else
            {
                FIOTB.Text = "Вы вошли как гость";
                RoleTB.Text = "Роль: Гость";
            }

            ComboCateghory.SelectedIndex = 0;
            ComboBoh.SelectedIndex = 0;
            UpdateProduct();
            ApplyRolePermissions();
        }

        private void ChangePage()
        {
            _isUpdating = true; // Блокирую событие SelectionChanged, пока обновляю список страниц
            PageListBox.Items.Clear();
            int totalPage = (_filtredProductsDisplay.Count + PageSize - 1) / PageSize;
            if (totalPage == 0) totalPage = 1;

            for (int i = 1; i <= totalPage; i++)
            {
                PageListBox.Items.Add(i);
            }
            PageListBox.SelectedItem = currentPage;

            var productPage = _filtredProductsDisplay
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ProductListView.ItemsSource = productPage;
            _isUpdating = false;
        }

        private void UpdateProduct()
        {
            List<ProductDisplay> currentProduct = new List<ProductDisplay>();

            using (var context = new ShoeYakupovaEntities1())
            {
                // Include подтягивает связанные таблицы (категории, производителя и остатки) сразу, 
                // чтобы потом не вылетала ошибка ObjectDisposedException при фильтрации
                var products = context.Product
                    .Include(p => p.Category)
                    .Include(p => p.Manufacturer)
                    .Include(p => p.SubCategory)
                    .Include(p => p.CommodityPosition)
                    .ToList();

                string appFolder = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var p in products)
                {
                    // Считаю общий остаток по всем размерам этого товара
                    int totalStock = p.CommodityPosition != null
                        ? p.CommodityPosition.Sum(cp => cp.QuantityInStock)
                        : 0;

                    // Формирую полный путь к картинке (папка Images рядом с .exe)
                    string imgPath = p.ImagePath;
                    if (!string.IsNullOrWhiteSpace(imgPath))
                    {
                        imgPath = System.IO.Path.Combine(appFolder, "Images", imgPath);
                    }

                    currentProduct.Add(new ProductDisplay
                    {
                        Product = p,
                        ImagePath = imgPath,
                        TotalQuantityInStock = totalStock
                    });
                }
            }

            var filtered = currentProduct.AsEnumerable();

            // ПОИСК (с заменой ё на е, чтобы не было проблем с регистром)
            if (!string.IsNullOrEmpty(TBSearch.Text))
            {
                string searchText = TBSearch.Text.ToLower().Replace("ё", "е");
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Replace("ё", "е").Contains(searchText)) ||
                    (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchText)) ||
                    (p.Manufacturer != null && p.Manufacturer.ManufacturerName.ToLower().Contains(searchText)) ||
                    (p.SubCategory != null && p.SubCategory.SubCategoryName.ToLower().Contains(searchText))
                );
            }

            // СОРТИРОВКА
            if (ComboBoh.SelectedIndex > 0)
            {
                string sortType = (ComboBoh.SelectedItem as TextBlock)?.Text;
                switch (sortType)
                {
                    case "по наименованию от А до Я":
                        filtered = filtered.OrderBy(p => p.ProductName); break;
                    case "по производителю":
                        filtered = filtered.OrderBy(p => p.Manufacturer != null ? p.Manufacturer.ManufacturerName : ""); break;
                    case "по возрастанию цены":
                        filtered = filtered.OrderBy(p => p.Price); break;
                    case "по убыванию цены":
                        filtered = filtered.OrderByDescending(p => p.Price); break;
                }
            }

            // КАТЕГОРИИ
            if (ComboCateghory.SelectedIndex == 1)
                filtered = filtered.Where(p => p.Category != null && p.Category.CategoryName == "Детская обувь");
            else if (ComboCateghory.SelectedIndex == 2)
                filtered = filtered.Where(p => p.Category != null && p.Category.CategoryName == "Женская обувь");
            else if (ComboCateghory.SelectedIndex == 3)
                filtered = filtered.Where(p => p.Category != null && p.Category.CategoryName == "Мужская обувь");

            _filtredProductsDisplay = filtered.ToList();
            currentPage = 1;
            ChangePage();
        }

        private void ApplyRolePermissions()
        {
            // Поиск и фильтры видят все, кроме гостя
            if (UserRoles == 1 || UserRoles == 2 || UserRoles == 3)
            {
                TSearch.Visibility = Visibility.Visible;
                TCateghory.Visibility = Visibility.Visible;
                TBoh.Visibility = Visibility.Visible;
                TBSearch.Visibility = Visibility.Visible;
                ComboCateghory.Visibility = Visibility.Visible;
                ComboBoh.Visibility = Visibility.Visible;
            }
            else
            {
                TSearch.Visibility = Visibility.Collapsed;
                TCateghory.Visibility = Visibility.Collapsed;
                TBoh.Visibility = Visibility.Collapsed;
                TBSearch.Visibility = Visibility.Collapsed;
                ComboCateghory.Visibility = Visibility.Collapsed;
                ComboBoh.Visibility = Visibility.Collapsed;
            }

            // Гость не может добавлять в корзину
            if (UserRoles == 0)
                AddToOrderMenuItem.Visibility = Visibility.Collapsed;
            else
                AddToOrderMenuItem.Visibility = Visibility.Visible;

            // Заказы видят админ и менеджер
            if (UserRoles == 1 || UserRoles == 2)
                OrdersBtn.Visibility = Visibility.Visible;
            else
                OrdersBtn.Visibility = Visibility.Collapsed;

            UpdateCartButton();

            // Аналитику видит только админ
            if (UserRoles == 1)
                AnalyticsBtn.Visibility = Visibility.Visible;
            else
                AnalyticsBtn.Visibility = Visibility.Collapsed;
        }

        private void UpdateCartButton()
        {
            if (UserRoles == 0)
            {
                CartBtn.Visibility = Visibility.Collapsed;
                return;
            }
            CartBtn.Visibility = Cart.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateProduct();
        private void ComboCateghory_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateProduct();
        private void ComboBoh_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateProduct();

        private void LeftDirBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ChangePage();
            }
        }

        private void RightDirBtn_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (_filtredProductsDisplay.Count + PageSize - 1) / PageSize;
            if (currentPage < totalPages)
            {
                currentPage++;
                ChangePage();
            }
        }

        private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return; // Защита от рекурсии
            if (PageListBox.SelectedItem != null)
            {
                int page = (int)PageListBox.SelectedItem;
                if (page != currentPage)
                {
                    currentPage = page;
                    ChangePage();
                }
            }
        }

        private void MenuItemAddToOrderClick(object sender, RoutedEventArgs e)
        {
            if (UserRoles == 0)
            {
                MessageBox.Show("Войдите, чтобы добавить товар к заказу.");
                return;
            }

            var item = ProductListView.SelectedItem as ProductDisplay;
            if (item == null)
            {
                MessageBox.Show("Не выбран товар.");
                return;
            }
            var product = item.Product;

            var window = new SizeSelectWindow(product);
            window.Owner = Application.Current.MainWindow;

            if (window.ShowDialog() == true)
            {
                Cart.Add(window.SelectedCommodityPositionID, window.SelectedQuantity);
            }

            UpdateCartButton();
        }

        private void OrdersBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new OrderHistoryPage(_currentUser, UserRoles));
        }

        private void AnalyticsBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AnalyticsPage(_currentUser, UserRoles));
        }

        private void CartBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new CartWindow(_currentUser);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            UpdateCartButton();
        }
    }

    // Класс-обертка для отображения товара с подсветкой
    public class ProductDisplay
    {
        public Product Product { get; set; }
        public string ProductName => Product.ProductName;
        public string ImagePath { get; set; }
        public decimal Price => Product.Price;
        public Manufacturer Manufacturer => Product.Manufacturer;
        public Category Category => Product.Category;
        public SubCategory SubCategory => Product.SubCategory;
        public int TotalQuantityInStock { get; set; }

        // Если остаток меньше 5, подсвечиваем строку красным (привязано в XAML)
        public bool IsLowStock => TotalQuantityInStock < 5;
    }
}