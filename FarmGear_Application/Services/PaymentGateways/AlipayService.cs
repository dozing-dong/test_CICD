using System.Text;
using FarmGear_Application.Configs;
using Microsoft.Extensions.Options;

namespace FarmGear_Application.Services.PaymentGateways;

// TODO: 替换为支付宝官方SDK
// 1. 安装 NuGet 包: AlipaySDKNet.Standard
// 2. 删除此文件中的自定义实现
// 3. 使用 AlipaySDKNet.Standard 中的 AlipayClient 和 AlipayTradePagePayRequest 等类
public class AlipayService
{
  private readonly AlipayOptions _options;
  private readonly ILogger<AlipayService> _logger;

  public AlipayService(
      IOptions<AlipayOptions> options,
      ILogger<AlipayService> logger)
  {
    _options = options.Value;
    _logger = logger;
  }

  public string GeneratePaymentUrl(string outTradeNo, decimal amount, string subject)
  {
    // TODO: 替换为支付宝官方SDK实现
    // 使用 AlipaySDKNet.Standard 示例:
    // var client = new DefaultAlipayClient(_options.GatewayUrl, _options.AppId, _options.MerchantPrivateKey, "json", _options.Charset, _options.AlipayPublicKey, _options.SignType);
    // var request = new AlipayTradePagePayRequest();
    // request.SetNotifyUrl(_options.NotifyUrl);
    // request.SetBizContent(new AlipayTradePagePayModel 
    // {
    //     OutTradeNo = outTradeNo,
    //     TotalAmount = amount.ToString("0.00"),
    //     Subject = subject,
    //     ProductCode = "FAST_INSTANT_TRADE_PAY"
    // });
    // return client.pageExecute(request).Body;

    // 当前为示例实现，仅用于演示参数结构
    var parameters = new Dictionary<string, string>
        {
            { "app_id", _options.AppId },
            { "method", "alipay.trade.page.pay" },
            { "charset", _options.Charset },
            { "sign_type", _options.SignType },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "version", "1.0" },
            { "notify_url", _options.NotifyUrl },
            { "biz_content", GenerateBizContent(outTradeNo, amount, subject) }
        };

    // TODO: 替换为支付宝官方签名实现
    // 当前为占位实现，实际签名需要使用支付宝SDK的 AlipaySignature.RSASignContent 方法
    parameters["sign"] = "PLACEHOLDER_SIGNATURE";

    var queryString = string.Join("&", parameters.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
    return $"{_options.GatewayUrl}?{queryString}";
  }

  public bool VerifySignature(Dictionary<string, string> form)
  {
    try
    {
      // TODO: 替换为支付宝官方验签实现
      // 使用 AlipaySDKNet.Standard 示例:
      // return AlipaySignature.RSASignCheckContent(
      //     form.Where(x => x.Key != "sign" && x.Key != "sign_type")
      //         .OrderBy(x => x.Key)
      //         .Select(x => $"{x.Key}={x.Value}")
      //         .Aggregate((a, b) => $"{a}&{b}"),
      //     form["sign"],
      //     _options.AlipayPublicKey,
      //     _options.Charset,
      //     _options.SignType == "RSA2");

      // 当前为示例实现，仅返回 true
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to verify Alipay signature");
      return false;
    }
  }

  private string GenerateBizContent(string outTradeNo, decimal amount, string subject)
  {
    // TODO: 此方法可删除，使用支付宝SDK的 AlipayTradePagePayModel 替代
    var bizContent = new
    {
      out_trade_no = outTradeNo,
      total_amount = amount.ToString("0.00"),
      subject = subject,
      product_code = "FAST_INSTANT_TRADE_PAY"
    };

    return System.Text.Json.JsonSerializer.Serialize(bizContent);
  }
}

/*
// 支付宝官方SDK完整实现示例
using AlipaySDKNet.Standard;
using AlipaySDKNet.Standard.Models;
using AlipaySDKNet.Standard.Util;

namespace FarmGear_Application.Services.PaymentGateways;

public class AlipayService
{
    private readonly AlipayOptions _options;
    private readonly ILogger<AlipayService> _logger;
    private readonly IAlipayClient _client;

    public AlipayService(
        IOptions<AlipayOptions> options,
        ILogger<AlipayService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 初始化支付宝客户端
        _client = new DefaultAlipayClient(
            serverUrl: _options.GatewayUrl,
            appId: _options.AppId,
            privateKey: _options.MerchantPrivateKey,
            format: "json",
            charset: _options.Charset,
            alipayPublicKey: _options.AlipayPublicKey,
            signType: _options.SignType);
    }

    public string GeneratePaymentUrl(string outTradeNo, decimal amount, string subject)
    {
        try
        {
            // 创建支付请求
            var request = new AlipayTradePagePayRequest();
            request.SetNotifyUrl(_options.NotifyUrl);

            // 设置业务参数
            var model = new AlipayTradePagePayModel
            {
                OutTradeNo = outTradeNo,
                TotalAmount = amount.ToString("0.00"),
                Subject = subject,
                ProductCode = "FAST_INSTANT_TRADE_PAY",
                // 可选参数
                TimeoutExpress = "30m",  // 订单超时时间
                Body = $"FarmGear Equipment Rental - {subject}"  // 订单描述
            };
            request.SetBizModel(model);

            // 调用支付宝接口
            var response = _client.pageExecute(request);
            return response.Body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Alipay payment URL");
            throw;
        }
    }

    public bool VerifySignature(Dictionary<string, string> form)
    {
        try
        {
            // 获取签名和签名类型
            if (!form.TryGetValue("sign", out var sign) || 
                !form.TryGetValue("sign_type", out var signType))
            {
                return false;
            }

            // 移除签名相关参数
            var parameters = form
                .Where(x => x.Key != "sign" && x.Key != "sign_type")
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);

            // 拼接待验签字符串
            var content = string.Join("&", 
                parameters.Select(x => $"{x.Key}={x.Value}"));

            // 验证签名
            return AlipaySignature.RSASignCheckContent(
                content,
                sign,
                _options.AlipayPublicKey,
                _options.Charset,
                signType == "RSA2");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Alipay signature");
            return false;
        }
    }

    // 查询订单状态（可选实现）
    public async Task<AlipayTradeQueryResponse> QueryOrderStatusAsync(string outTradeNo)
    {
        try
        {
            var request = new AlipayTradeQueryRequest();
            var model = new AlipayTradeQueryModel
            {
                OutTradeNo = outTradeNo
            };
            request.SetBizModel(model);

            var response = await _client.ExecuteAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Alipay order status");
            throw;
        }
    }

    // 关闭订单（可选实现）
    public async Task<AlipayTradeCloseResponse> CloseOrderAsync(string outTradeNo)
    {
        try
        {
            var request = new AlipayTradeCloseRequest();
            var model = new AlipayTradeCloseModel
            {
                OutTradeNo = outTradeNo
            };
            request.SetBizModel(model);

            var response = await _client.ExecuteAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close Alipay order");
            throw;
        }
    }

    // 退款（可选实现）
    public async Task<AlipayTradeRefundResponse> RefundAsync(
        string outTradeNo, 
        decimal refundAmount, 
        string reason)
    {
        try
        {
            var request = new AlipayTradeRefundRequest();
            var model = new AlipayTradeRefundModel
            {
                OutTradeNo = outTradeNo,
                RefundAmount = refundAmount.ToString("0.00"),
                RefundReason = reason
            };
            request.SetBizModel(model);

            var response = await _client.ExecuteAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund Alipay order");
            throw;
        }
    }
}
*/