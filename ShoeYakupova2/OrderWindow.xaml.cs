using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShoeYakupova2
{
    public partial class OrderWindow : Window
    {
        private Order _order;
        private List<OrderItemView> _itemsView;
        private Client _currentUser;
        private int UserRoles;

        public OrderWindow(Order order, Client user, int userRoles)
        {
            InitializeComponent();
            _order = order;
            _currentUser = user;
            UserRoles = userRoles;

            if (_order == null)
            {
                MessageBox.Show("Заказ не передан.");
                Close();
                return;
            }

            Title = "Заказ №" + _order.OrderID;
            OrderNumberText.Text = _order.OrderID.ToString();

            // Загружаем дату в DatePicker
            OrderDatePicker.SelectedDate = _order.OrderDate;

            if (_order.Client != null)
            {
                ClientNameText.Text = (_order.Client.LastName + " " +
                                       _order.Client.FirstName + " " +
                                       _order.Client.Patronymic).Trim();
            }
            else
            {
                ClientNameText.Text = "(клиент не указан)";
            }

            LoadItems();

            // Редактирование разрешено только администратору (RoleID = 1)
            if (UserRoles != 1)
            {
                SaveButton.Visibility = Visibility.Collapsed;
                AddItemButton.Visibility = Visibility.Collapsed;
                OrderDatePicker.IsEnabled = false; // Запрещаем менять дату не-админам
            }
        }

        // Обработчик смены даты (доступен только админу)
        private void OrderDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserRoles != 1 || OrderDatePicker.SelectedDate == null) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                var order = context.Order.FirstOrDefault(o => o.OrderID == _order.OrderID);
                if (order != null)
                {
                    order.OrderDate = OrderDatePicker.SelectedDate.Value;
                    context.SaveChanges();
                }
            }
        }

        private void LoadItems()
        {
            var items = new List<OrderItemView>();

            using (var context = new ShoeYakupovaEntities1())
            {
                var freshOrder = context.Order
                    .FirstOrDefault(o => o.OrderID == _order.OrderID);

                if (freshOrder != null && freshOrder.OrderItem != null)
                {
                    foreach (var oi in freshOrder.OrderItem)
                    {
                        var view = new OrderItemView
                        {
                            ProductName = oi.CommodityPosition != null && oi.CommodityPosition.Product != null
                                ? oi.CommodityPosition.Product.ProductName
                                : "(товар неизвестен)",

                            SizeText = oi.CommodityPosition != null && oi.CommodityPosition.Size != null
                                ? oi.CommodityPosition.Size.SizeValue.ToString()
                                : "—",

                            Quantity = oi.Quantity,
                            Price = oi.PriceAtOrderTime,
                            LineSum = oi.Quantity * oi.PriceAtOrderTime,
                            SourceItem = oi
                        };

                        items.Add(view);
                    }
                }
            }

            _itemsView = items;
            OrderItemsListView.ItemsSource = _itemsView;
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1) return;

            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null || item.SourceItem == null) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);

                if (cp == null) return;

                if (cp.QuantityInStock <= 0)
                {
                    MessageBox.Show("Вы пытаетесь добавить товара больше, чем есть на складе.");
                    return;
                }

                cp.QuantityInStock--;
                item.SourceItem.Quantity++;
                item.Quantity = item.SourceItem.Quantity;
                item.LineSum = item.Quantity * item.Price;

                context.SaveChanges();
            }

            OrderItemsListView.Items.Refresh();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1) return;

            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null || item.SourceItem == null) return;

            if (item.SourceItem.Quantity <= 1) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);

                if (cp == null) return;

                cp.QuantityInStock++;

                var realItem = context.OrderItem
                    .FirstOrDefault(x => x.OrderItemID == item.SourceItem.OrderItemID);

                if (realItem != null)
                    realItem.Quantity--;

                context.SaveChanges();
            }

            item.SourceItem.Quantity--;
            item.Quantity = item.SourceItem.Quantity;
            item.LineSum = item.Quantity * item.Price;

            OrderItemsListView.Items.Refresh();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1) return;

            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null || item.SourceItem == null) return;

            var result = MessageBox.Show("Удалить позицию?", "Подтверждение",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);

                if (cp != null)
                    cp.QuantityInStock += item.SourceItem.Quantity;

                var real = context.OrderItem
                    .FirstOrDefault(x => x.OrderItemID == item.SourceItem.OrderItemID);

                if (real != null)
                    context.OrderItem.Remove(real);

                context.SaveChanges();
            }

            LoadItems();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1)
            {
                MessageBox.Show("Только администратор может сохранять изменения.");
                return;
            }

            MessageBox.Show("Изменения сохранены.");
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1)
            {
                MessageBox.Show("Только администратор может добавлять позиции.");
                return;
            }

            var productWindow = new ProductSelectWindow();
            productWindow.Owner = this;
            if (productWindow.ShowDialog() != true) return;

            var product = productWindow.SelectedProduct;
            if (product == null) return;

            var sizeWindow = new SizeSelectWindow(product);
            sizeWindow.Owner = this;
            if (sizeWindow.ShowDialog() != true) return;

            int cpId = sizeWindow.SelectedCommodityPositionID;
            int qty = sizeWindow.SelectedQuantity;

            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == cpId);
                if (cp == null) return;

                if (cp.QuantityInStock < qty)
                {
                    MessageBox.Show("На складе недостаточно товара.");
                    return;
                }

                var newItem = new OrderItem
                {
                    OrderID = _order.OrderID,
                    CommodityPositionID = cpId,
                    Quantity = qty,
                    PriceAtOrderTime = cp.Product != null ? cp.Product.Price : 0
                };
                context.OrderItem.Add(newItem);

                cp.QuantityInStock -= qty;

                context.SaveChanges();
            }

            LoadItems();
            MessageBox.Show("Позиция добавлена.");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}