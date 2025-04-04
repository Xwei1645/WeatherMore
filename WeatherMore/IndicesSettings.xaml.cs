using System.IO;
using System.Windows.Controls;
using ClassIsland.Shared.Helpers;
using Newtonsoft.Json;

namespace WeatherMore;

public partial class IndicesSettings
{
    private string _configFilePath;

    public IndicesSettings()
    {
        InitializeComponent();
        // 获取插件配置目录
        var plugin = new Plugin();
        _configFilePath = Path.Combine(plugin.PluginConfigFolder, "IndicesConfig.json");
        // 加载配置
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var config = ConfigureFileHelper.LoadConfig<IndicesConfig>(_configFilePath);
                // 根据配置设置 ComboBox 的选中项
                foreach (ComboBoxItem item in IndicesTypeComboBox.Items)
                {
                    if (item.Tag.ToString() == config.Type)
                    {
                        IndicesTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeatherMore][IndicesSettings] 加载配置时出错: {ex.Message}");
        }
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

            // 保存配置
            SaveConfig(selectedType);
        }
    }

    private void SaveConfig(string type)
    {
        try
        {
            var config = new IndicesConfig { Type = type };
            ConfigureFileHelper.SaveConfig<IndicesConfig>(_configFilePath, config);
            Console.WriteLine("[WeatherMore][IndicesSettings] 配置保存成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeatherMore][IndicesSettings] 保存配置时出错: {ex.Message}");
        }
    }
}

public class IndicesConfig
{
    public string Type { get; set; }
}