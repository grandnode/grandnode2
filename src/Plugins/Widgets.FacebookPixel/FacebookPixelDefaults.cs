﻿namespace Widgets.FacebookPixel;

public static class FacebookPixelDefaults
{
    public const string ProviderSystemName = "Widgets.FacebookPixel";
    public const string FriendlyName = "Widgets.FacebookPixel.FriendlyName";
    public const string ConfigurationUrl = "../WidgetsFacebookPixel/Configure";
    public const string ConsentCookieSystemName = "FacebookPixel";

    public static string Page => "head_html_tag";
    public static string AddToCart => "popup_add_to_cart_content_before";
    public static string OrderDetails => "checkout_completed_top";
}