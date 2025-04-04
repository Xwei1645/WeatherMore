using System.Windows.Controls;

namespace WeatherMore;

public partial class IndicesSettings
{
    public IndicesSettings()
    {
        InitializeComponent();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 获取选中项的 Tag
        var selectedItem = ((ComboBox)sender).SelectedItem as ComboBoxItem;
        if (selectedItem != null)
        {
            var selectedType = selectedItem.Tag.ToString();
            // 获取 Indices 实例并调用重新加载方法
            var indices = Indices.GetInstance();
            indices.LoadIndicesDataAsync(selectedType);
        }
    }
}