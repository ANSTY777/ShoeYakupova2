using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShoeYakupova2
{
    public partial class AuthoPage : Page
    {
        public AuthoPage()
        {
            InitializeComponent();
        }

        private void GuestBtn_Click(object sender, RoutedEventArgs e)
        {
            // Передаю null, чтобы ProductPage знал, что это гость (нет прав на корзину/заказы)
            Manager.MainFrame.Navigate(new ProductPage(null));
            MessageBox.Show("Вы вошли как гость");
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            // Trim(), чтобы случайно не ввести пробелы в начале или конце
            string login = TBLogin.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Есть пустые поля");
                return;
            }

            using (var context = new ShoeYakupovaEntities1())
            {
                // Ищу пользователя по логину (без пароля, как в задании)
                Client user = context.Client
                    .FirstOrDefault(p => p.Login == login);

                if (user != null)
                {
                    Manager.MainFrame.Navigate(new ProductPage(user));
                    TBLogin.Text = ""; // Очищаю поле после входа
                    MessageBox.Show("Вы успешно вошли в аккаунт");
                }
                else
                {
                    MessageBox.Show("Неверно введены данные! Проверьте ещё раз");
                }
            }
        }
    }
}