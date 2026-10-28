using System;
using System.Windows;
using System.Windows.Controls;

namespace ShoeYakupova2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Запускаю приложение со страницы авторизации
            MainFrame.Navigate(new AuthoPage());

            // Сохраняю ссылку на фрейм в статический класс Manager, 
            // чтобы из любой страницы можно было делать переходы
            Manager.MainFrame = MainFrame;
        }

        private void MainFrame_ContentRendered(object sender, EventArgs e)
        {
            // Если есть куда вернуться (CanGoBack), показываю кнопку "Назад"
            if (MainFrame.CanGoBack)
            {
                BtnBack.Visibility = Visibility.Visible;
            }
            else
            {
                // На главной странице (авторизация) кнопка скрыта
                BtnBack.Visibility = Visibility.Hidden;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на предыдущую страницу
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }
    }
}