using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShoeYakupova2
{
    public partial class CartWindow : Window
    {
        private Client _currentUser;

        public CartWindow(Client user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadCartItems();
        }

        private void LoadCartItems()
        {
            var items = new List<OrderItemView>();

            using (var context = new ShoeYakupovaEntities1())
            {
                // Иду по словарю корзины и достаю данные о товаре из БД
                foreach (var pair in Cart.Items)
                {
                    int commodityPositionId = pair.Key;
                    int quantity = pair.Value;

                    var cp = context.CommodityPosition
                        .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

                    if (cp == null) continue;

                    // Формирую объект для отображения в таблице
                    var view = new OrderItemView
                    {
                        ProductName = cp.Product != null ? cp.Product.ProductName : "(товар неизвестен)",
                        SizeText = cp.Size != null ? cp.Size.SizeValue.ToString() : "—",
                        Quantity = quantity,
                        Price = cp.Product != null ? cp.Product.Price : 0,
                        LineSum = (cp.Product != null ? cp.Product.Price : 0) * quantity
                    };

                    items.Add(view);
                }
            }

            CartItemsListView.ItemsSource = items;
            UpdateTotal(items);
        }

        private void UpdateTotal(List<OrderItemView> items)
        {
            decimal total = items.Sum(i => i.LineSum);
            TotalText.Text = "Итого: " + total.ToString("N2") + " ₽";
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (Cart.IsEmpty)
            {
                MessageBox.Show("Корзина пуста.");
                return;
            }

            if (_currentUser == null)
            {
                MessageBox.Show("Вы не авторизованы.");
                return;
            }

            using (var context = new ShoeYakupovaEntities1())
            {
                // Загружаю все нужные позиции разом, чтобы не дергать БД в цикле
                var cpIds = Cart.Items.Keys.ToList();
                var positions = context.CommodityPosition
                    .Where(c => cpIds.Contains(c.CommodityPositionID))
                    .ToList();

                // Сначала проверяю остатки. Если хоть одного не хватает — прерываю оформление
                foreach (var pair in Cart.Items)
                {
                    var cp = positions.FirstOrDefault(c => c.CommodityPositionID == pair.Key);
                    if (cp == null)
                    {
                        MessageBox.Show("Товарная позиция не найдена.");
                        return;
                    }
                    if (cp.QuantityInStock < pair.Value)
                    {
                        string name = cp.Product != null ? cp.Product.ProductName : "(товар)";
                        string size = cp.Size != null ? cp.Size.SizeValue.ToString() : "?";
                        MessageBox.Show("Недостаточно товара на складе: " + name + ", размер " + size +
                                        ". В наличии: " + cp.QuantityInStock + " шт.");
                        return;
                    }
                }

                // Создаю сам заказ и сохраняю, чтобы получить его ID
                var newOrder = new Order
                {
                    ClientID = _currentUser.ClientID,
                    OrderDate = System.DateTime.Now
                };
                context.Order.Add(newOrder);
                context.SaveChanges();

                // Теперь добавляю позиции к заказу и списываю со склада
                foreach (var pair in Cart.Items)
                {
                    var cp = positions.First(c => c.CommodityPositionID == pair.Key);

                    var item = new OrderItem
                    {
                        OrderID = newOrder.OrderID,
                        CommodityPositionID = cp.CommodityPositionID,
                        Quantity = pair.Value,
                        PriceAtOrderTime = cp.Product != null ? cp.Product.Price : 0
                    };
                    context.OrderItem.Add(item);

                    cp.QuantityInStock -= pair.Value;
                }
                // Сохраняю изменения по позициям и остаткам
                context.SaveChanges();

                Cart.Clear();

                MessageBox.Show("Заказ оформлен. Номер: " + newOrder.OrderID);
                Close();
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.ChangeQuantity(cpId, +1);
            LoadCartItems();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.ChangeQuantity(cpId, -1);
            LoadCartItems();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.Remove(cpId);
            LoadCartItems();
        }

        // Ищу ID позиции по имени и размеру. Это нужно, так как кнопки в таблице 
        // привязаны к OrderItemView, а не к самой позиции на складе
        private int FindCommodityPositionId(OrderItemView item)
        {
            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .ToList()
                    .FirstOrDefault(c =>
                        c.Product != null &&
                        c.Product.ProductName == item.ProductName &&
                        c.Size != null &&
                        c.Size.SizeValue.ToString() == item.SizeText);

                return cp != null ? cp.CommodityPositionID : 0;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}