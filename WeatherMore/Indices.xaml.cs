using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core;
using Newtonsoft.Json;

namespace WeatherMore;

[ComponentInfo(
    "83402E4E-8CE5-416F-A301-F2A527A65406",
    "天气指数",
    PackIconKind.Info,
    "显示当前的天气生活指数。"
)]
public partial class Indices : ComponentBase
{
    private static Indices _instance;
    private const string CacheFilePath = "IndicesCache.json";
    private System.Timers.Timer _timer;

    public static Indices GetInstance()
    {
        return _instance;
    }

    public Indices()
    {
        InitializeComponent();
        _instance = this;
        LoadCachedData();
        // 在构造函数中初始化 _timer，设置为每10分钟刷新一次
        _timer = new System.Timers.Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
        _timer.Elapsed += (sender, e) => LoadIndicesDataAsync("1");
        _timer.AutoReset = true;
        _timer.Start();
        LoadIndicesDataAsync("1");
    }

    private void LoadCachedData()
    {
        if (File.Exists(CacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(CacheFilePath);
                var data = JObject.Parse(json);
                var daily = data["daily"] as JArray;
                if (daily != null)
                {
                    foreach (var item in daily)
                    {
                        var name = item["name"]?.ToString();
                        var category = item["category"]?.ToString();

                        if (name != null && category != null)
                        {
                            var singleLineInfo = $"{name}: {category}";
                            Dispatcher.Invoke(() => IndicesText.Text = singleLineInfo);
                            break; // 找到第一个有效数据后就停止遍历
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherMore][Indices] 读取缓存时出错: {ex.Message}");
            }
        }
    }

    public async void LoadIndicesDataAsync(string type)
    {
        try
        {
            // 获取城市 id
            var locationId = (string)((dynamic)AppBase.Current).Settings.CityId;
            locationId = locationId.Split(':')[1];

            var apiKey = "83f59d5f69444ee59600c90637759aad";
            var url = $"https://devapi.qweather.com/v7/indices/1d?key={apiKey}&location={locationId}&type={type}";

            using (var httpClient = new HttpClient())
            {
                Console.WriteLine($"[WeatherMore][Indices] 向 QWEATHER 请求天气指数数据，类型: {type}");
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var deflateStream = new GZipStream(responseStream, CompressionMode.Decompress))
                using (var streamReader = new StreamReader(deflateStream))
                {
                    var responseBody = await streamReader.ReadToEndAsync();

                    if (!IsValidJson(responseBody))
                    {
                        Console.WriteLine("[WeatherMore][Indices] JSON 解析错误");
                        Dispatcher.Invoke(() => IndicesText.Text = "生活指数加载失败");
                        return;
                    }

                    var json = JObject.Parse(responseBody);
                    // 检查 API 返回的状态码
                    if (json["code"]?.ToString() != "200")
                    {
                        Console.WriteLine($"[WeatherMore][Indices] API 返回错误: {json["code"]?.ToString()}");
                        Dispatcher.Invoke(() => IndicesText.Text = "生活指数加载失败");
                        return;
                    }

                    var daily = json["daily"] as JArray;
                    if (daily != null)
                    {
                        foreach (var item in daily)
                        {
                            var name = item["name"]?.ToString();
                            var category = item["category"]?.ToString();

                            if (name != null && category != null)
                            {
                                var singleLineInfo = $"{name}: {category}";
                                Dispatcher.Invoke(() => IndicesText.Text = singleLineInfo);
                                SaveDataToCache(json);
                                break; // 找到第一个有效数据后就停止遍历
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("[WeatherMore][Indices] 生活指数数据未找到");
                        Dispatcher.Invoke(() => IndicesText.Text = "生活指数未找到");
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"[WeatherMore][Indices] 请求错误: {e.Message}");
            Dispatcher.Invoke(() => IndicesText.Text = "生活指数加载失败");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WeatherMore][Indices] 未知错误: {e.Message}");
            Dispatcher.Invoke(() => IndicesText.Text = "生活指数加载失败");
        }
    }

    private void SaveDataToCache(JObject data)
    {
        try
        {
            File.WriteAllText(CacheFilePath, data.ToString());
            Console.WriteLine("[WeatherMore][Indices] 缓存天气指数数据成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeatherMore][Indices] 缓存时出错: {ex.Message}");
        }
    }

    private bool IsValidJson(string json)
    {
        try
        {
            JToken.Parse(json);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }
}