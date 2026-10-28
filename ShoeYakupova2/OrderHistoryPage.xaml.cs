using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoeYakupova2
{
    public partial class OrderHistoryPage : Page
    {
        private Client _currentUser;
        private int UserRoles;

        public OrderHistoryPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        public OrderHistoryPage(Client user, int userRoles)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = userRoles;
            LoadOrders();
        }

        private void LoadOrders()
        {
            using (var context = new ShoeYakupovaEntities1())
            {
                // Include("...") сразу подтягивает связанные таблицы (клиента и позиции), 
                // чтобы потом не было ошибки "контекст закрыт"
                var orders = context.Order
                    .Include("Client")
                    .Include("OrderItem")
                    .ToList();

                // Формирую удобный класс OrderView для привязки к таблице
                var view = orders.Select(o => new OrderView
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate,
                    ClientFIO = o.Client != null
                        ? $"{o.Client.LastName} {o.Client.FirstName} {o.Client.Patronymic}".Trim()
                        : "(клиент неизвестен)",
                    TotalSum = o.OrderItem != null
                        ? o.OrderItem.Sum(oi => oi.Quantity * oi.PriceAtOrderTime)
                        : 0,
                    SourceOrder = o
                }).ToList();

                OrderListView.ItemsSource = view;
            }

            // Кнопки показываю только админу (1) и менеджеру (2)
            if (UserRoles == 1 || UserRoles == 2)
            {
                CreateOrderBtn.Visibility = Visibility.Visible;
                DeleteOrderBtn.Visibility = Visibility.Visible;
            }
            else
            {
                CreateOrderBtn.Visibility = Visibility.Collapsed;
                DeleteOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrderListView.SelectedItem as OrderView;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            var result = MessageBox.Show(
                "Удалить заказ №" + selected.OrderID + "?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                // Сначала удаляю все позиции заказа, иначе БД выдаст ошибку внешнего ключа
                var items = context.OrderItem
                    .Where(oi => oi.OrderID == selected.OrderID)
                    .ToList();

                foreach (var item in items)
                    context.OrderItem.Remove(item);

                // И только теперь сам заказ
                var order = context.Order.FirstOrDefault(o => o.OrderID == selected.OrderID);
                if (order != null)
                    context.Order.Remove(order);

                context.SaveChanges();
            }

            LoadOrders();
        }

        private void CreateOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Вы не авторизованы.");
                return;
            }

            using (var context = new ShoeYakupovaEntities1())
            {
                var newOrder = new Order
                {
                    ClientID = _currentUser.ClientID,
                    OrderDate = DateTime.Now
                };
                context.Order.Add(newOrder);
                context.SaveChanges();

                LoadOrders();
                MessageBox.Show("Заказ создан. Номер: " + newOrder.OrderID);
            }
        }

        private void OrderListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = OrderListView.SelectedItem as OrderView;
            if (selected == null) return;

            // Открываю окно редактирования, передаю сам заказ и права текущего юзера
            var window = new OrderWindow(selected.SourceOrder, _currentUser, UserRoles);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();

            // После закрытия окна обновляю список (вдруг что-то изменили)
            LoadOrders();
        }
    }
}