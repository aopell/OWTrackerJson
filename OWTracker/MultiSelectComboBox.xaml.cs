using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OWTracker
{
    /// <summary> 
    /// Interaction logic for MultiSelectComboBox.xaml 
    /// </summary> 
    public partial class MultiSelectComboBox : UserControl
    {
        private ObservableCollection<Node> _nodeList;
        public MultiSelectComboBox()
        {
            InitializeComponent();
            _nodeList = new ObservableCollection<Node>();
        }

        #region Dependency Properties 

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<object>), typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null,
                                                                                                                                                      new PropertyChangedCallback(MultiSelectComboBox.OnItemsSourceChanged)));

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(List<object>), typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null,
                                                                                                                                                        new PropertyChangedCallback(MultiSelectComboBox.OnSelectedItemsChanged)));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));

        public static readonly DependencyProperty AllTextProperty =
            DependencyProperty.Register("AllText", typeof(string), typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));



        public List<object> ItemsSource
        {
            get { return (List<object>)GetValue(ItemsSourceProperty); }
            set
            {
                SetValue(ItemsSourceProperty, value);
            }
        }

        public List<object> SelectedItems
        {
            get { return (List<object>)GetValue(SelectedItemsProperty); }
            set
            {
                SetValue(SelectedItemsProperty, value);
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string DefaultText
        {
            get { return (string)GetValue(DefaultTextProperty); }
            set { SetValue(DefaultTextProperty, value); }
        }

        public string AllText
        {
            get { return (string)GetValue(AllTextProperty); }
            set { SetValue(AllTextProperty, value); }
        }

        public bool NoneSelected => SelectedItems == null || SelectedItems.Count == 0;
        public bool AllSelected => SelectedItems != null && SelectedItems.Count == ItemsSource.Count;

        #endregion

        #region Events 
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MultiSelectComboBox control = (MultiSelectComboBox)d;
            control.DisplayInControl();
        }

        public event RoutedEventHandler SelectedItemsChanged;

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MultiSelectComboBox control = (MultiSelectComboBox)d;
            control.SelectNodes();
            control.SetText();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox clickedBox = (CheckBox)sender;

            if (clickedBox.Content == AllText)
            {
                if (clickedBox.IsChecked.Value)
                {
                    foreach (Node node in _nodeList)
                    {
                        node.IsSelected = true;
                    }
                }
                else
                {
                    foreach (Node node in _nodeList)
                    {
                        node.IsSelected = false;
                    }
                }

            }
            else
            {
                int _selectedCount = 0;
                foreach (Node s in _nodeList)
                {
                    if (s.IsSelected && s.Title != AllText)
                        _selectedCount++;
                }
                if (_selectedCount == _nodeList.Count - 1)
                    _nodeList.FirstOrDefault(i => i.Title == AllText).IsSelected = true;
                else
                    _nodeList.FirstOrDefault(i => i.Title == AllText).IsSelected = false;
            }
            SetSelectedItems();
            SetText();
            SelectedItemsChanged?.Invoke(this, new RoutedEventArgs());
        }
        #endregion


        #region Methods 
        private void SelectNodes()
        {
            foreach (var val in SelectedItems)
            {
                Node node = _nodeList.FirstOrDefault(i => i.Title == val.ToString());
                if (node != null)
                    node.IsSelected = true;
            }
        }

        private void SetSelectedItems()
        {
            if (SelectedItems == null)
                SelectedItems = new List<object>();
            SelectedItems.Clear();
            foreach (Node node in _nodeList)
            {
                if (node.IsSelected && node.Title != AllText)
                {
                    if (this.ItemsSource.Count > 0)

                        SelectedItems.Add(node.Title);
                }
            }
        }

        private void DisplayInControl()
        {
            _nodeList.Clear();
            if (this.ItemsSource.Count > 0)
                _nodeList.Add(new Node(AllText));
            foreach (var val in this.ItemsSource)
            {
                Node node = new Node(val.ToString());
                _nodeList.Add(node);
            }
            MultiSelectCombo.ItemsSource = _nodeList;
        }

        private void SetText()
        {
            if (SelectedItems == null || SelectedItems.Count == 0) Text = DefaultText;
            else if (SelectedItems.Count == ItemsSource.Count) Text = AllText;
            else if (SelectedItems.Count == 1) Text = SelectedItems[0].ToString();
            else if (SelectedItems.Count > 1) Text = $"{SelectedItems.Count} SELECTED";
        }


        #endregion

        public void SelectAll()
        {
            foreach (Node node in _nodeList)
            {
                node.IsSelected = true;
            }

            SetSelectedItems();
            SetText();
            SelectedItemsChanged?.Invoke(this, new RoutedEventArgs());
        }

        public void SelectNone()
        {
            foreach (Node node in _nodeList)
            {
                node.IsSelected = false;
            }

            SetSelectedItems();
            SetText();
            SelectedItemsChanged?.Invoke(this, new RoutedEventArgs());
        }
    }

    public class Node : INotifyPropertyChanged
    {

        private string _title;
        private bool _isSelected;
        #region ctor 
        public Node(string title)
        {
            Title = title;
        }
        #endregion

        #region Properties 
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}