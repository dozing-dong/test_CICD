using System.ComponentModel.DataAnnotations;

namespace FarmGear_Application.Configs;

public class AlipayOptions
{
  [Required]
  public string AppId { get; set; } = string.Empty;

  [Required]
  public string MerchantPrivateKey { get; set; } = string.Empty;

  [Required]
  public string AlipayPublicKey { get; set; } = string.Empty;

  [Required]
  public string NotifyUrl { get; set; } = string.Empty;

  public string GatewayUrl { get; set; } = "https://openapi.alipay.com/gateway.do";

  public string Charset { get; set; } = "utf-8";

  public string SignType { get; set; } = "RSA2";
}