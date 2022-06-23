public class PriceResultRoot
{
    public object[] billingOptions { get; set; }
    public object[] computePaymentOptions { get; set; }
    public object[] dropdown { get; set; }
    public object[] linuxTypes { get; set; }
    public object[] operatingSystems { get; set; }
    public object[] sizesOneYear { get; set; }
    public object[] sizesPayGo { get; set; }
    public object[] sizesThreeYear { get; set; }
    public object[] sizesSavingsOneYear { get; set; }
    public object[] sizesSavingsThreeYear { get; set; }
    public object[] sizesFiveYear { get; set; }
    public object[] softwareLicenses { get; set; }
    public object[] subscriptionOptions { get; set; }
    public object[] tiers { get; set; }
    public object[] windowsTypes { get; set; }
    public Dictionary<string, OfferItem> offers { get; set; }
    public object[] regions { get; set; }
    public object[] discounts { get; set; }
    public object resources { get; set; }
    public object schema { get; set; }
    public object skus { get; set; }
}

public class OfferItem
{
    public bool availableForML { get; set; }
    public int cores { get; set; }
    public int diskSize { get; set; }
    public bool isHidden { get; set; }
    public bool isVcpu { get; set; }
    public float ram { get; set; }
    public string series { get; set; }
    public PriceItem prices { get; set; }
    public string pricingTypes { get; set; }
    public string offerType { get; set; }
}

public class PriceItem
{
    public Dictionary<string, Price> perhour { get; set; }
    public Dictionary<string, Price> perhouroneyearreserved { get; set; }
    public Dictionary<string, Price> perhourthreeyearreserved { get; set; }
    public Dictionary<string, Price> perhourspot { get; set; }
}

public class Price
{
    public decimal value { get; set; }
}
