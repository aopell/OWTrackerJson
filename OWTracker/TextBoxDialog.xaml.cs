using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OWTracker
{
    /// <summary>
    /// Interaction logic for TextBoxDialog.xaml
    /// </summary>
    public partial class TextBoxDialog : Window
    {
        public string Text { get; set; }
        public string ButtonText { get; set; }
        public DialogType Type { get; set; }
        public Color TextColor { get; set; }

        public string Result { get; private set; }

        public TextBoxDialog(string message, string buttonText, DialogType type, Color textColor = default(Color))
        {
            InitializeComponent();

            Text = message;
            ButtonText = buttonText;
            Type = type;
            TextColor = textColor;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Message.Text = Text;
            Button.Content = ButtonText;
            Message.Foreground = new SolidColorBrush(TextColor == default(Color) ? Colors.Black : TextColor);

            switch (Type)
            {
                case DialogType.TextEntry:
                    PasswordTextBox.Visibility = Visibility.Hidden;
                    break;
                case DialogType.PasswordEntry:
                    RegularTextBox.Visibility = Visibility.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (Type)
            {
                case DialogType.TextEntry:
                    Result = RegularTextBox.Text;
                    break;
                case DialogType.PasswordEntry:
                    Result = PasswordTextBox.Password;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }
    }

    public enum DialogType
    {
        TextEntry,
        PasswordEntry
    }
}
