using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core;

namespace WeatherMore;

[ComponentInfo(
    "0D41F4CE-B503-40CF-8E74-8B6B94750D1B",
    "AQI",
    PackIconKind.AirFilter,
    "显示当前的空气质量指数（AQI）。"
)]
public partial class AQI : ComponentBase
{
    private const string CacheFilePath = "AQICache.json";
    private System.Timers.Timer _timer;

    public AQI()
    {
        InitializeComponent();
        LoadCachedData();
        // 在构造函数中初始化 _timer
        _timer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _timer.Elapsed += (sender, e) => LoadAQIDataAsync();
        _timer.AutoReset = true;
        _timer.Start();
        LoadAQIDataAsync();
    }

    private void LoadCachedData()
    {
        if (File.Exists(CacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(CacheFilePath);
                var data = JObject.Parse(json);
                var aqi = data["now"]?["aqi"]?.ToString();
                if (aqi != null)
                {
                    var formattedAqi = $"AQI {aqi}";
                    Dispatcher.Invoke(() => AQIText.Text = formattedAqi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherMore][AQI] 读取缓存时出错: {ex.Message}");
            }
        }
    }

    private async void LoadAQIDataAsync()
    {
        try
        {
            // 获取城市 id
            var locationId = (string)((dynamic)AppBase.Current).Settings.CityId;
            locationId = locationId.Split(':')[1];

            var apiKey = "83f59d5f69444ee59600c90637759aad";
            var url = $"https://devapi.qweather.com/v7/air/now?key={apiKey}&location={locationId}";

            using (var httpClient = new HttpClient())
            {
                Console.WriteLine("[WeatherMore][AQI] 向 QWEATHER 请求 AQI 数据");
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var deflateStream = new GZipStream(responseStream, CompressionMode.Decompress))
                using (var streamReader = new StreamReader(deflateStream))
                {
                    var responseBody = await streamReader.ReadToEndAsync();

                    if (!IsValidJson(responseBody))
                    {
                        Console.WriteLine("[WeatherMore][AQI] JSON 解析错误");
                        Dispatcher.Invoke(() => AQIText.Text = "AQI 加载失败");
                        return;
                    }

                    var json = JObject.Parse(responseBody);
                    // 检查 API 返回的状态码
                    if (json["code"]?.ToString() != "200")
                    {
                        Console.WriteLine($"[WeatherMore][AQI] API 返回错误: {json["code"]?.ToString()}");
                        Dispatcher.Invoke(() => AQIText.Text = "AQI 加载失败");
                        return;
                    }

                    var aqi = json["now"]?["aqi"]?.ToString();

                    if (aqi != null)
                    {
                        var formattedAqi = $"AQI {aqi}";
                        Dispatcher.Invoke(() => AQIText.Text = formattedAqi);
                        SaveDataToCache(json);
                    }
                    else
                    {
                        Console.WriteLine("[WeatherMore][AQI] AQI 未找到");
                        Dispatcher.Invoke(() => AQIText.Text = "AQI 未找到");
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"[WeatherMore][AQI] 请求错误: {e.Message}");
            Dispatcher.Invoke(() => AQIText.Text = "AQI 加载失败");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WeatherMore][AQI] 未知错误: {e.Message}");
            Dispatcher.Invoke(() => AQIText.Text = "AQI 加载失败");
        }
    }

    private void SaveDataToCache(JObject data)
    {
        try
        {
            File.WriteAllText(CacheFilePath, data.ToString());
            Console.WriteLine("[WeatherMore][AQI] 缓存 AQI 数据成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeatherMore][AQI] 缓存时出错: {ex.Message}");
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