using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginAutomation.Tests.Models
{
    public class ItemsModel
    {
        public List<Item> Items { get; set; }
    }
    public class Item
    {
        public ItemDetails ItemDetails { get; set; }
        public ItemDimensionsDetails ItemDimensionsDetails { get; set; }
        public ItemPackagingDetails ItemPackagingDetails { get; set; }
        public WarehouseDetails WarehouseDetails { get; set; }
        public ShipDetails ShipDetails { get; set; }
        public BoxDetails BoxDetails { get; set; }
        public CartonDetails CartonDetails { get; set; }

    }
    public class ItemDetails
    {
       public string Item { get; set; }
       public string UPCCode { get; set; }
       public string ManufacturePart { get; set; }
       public string Inventory { get; set; }
       public string ItemDescription { get; set; }
       public string Notes { get; set; }
       public string ItemType { get; set; }
       public string Status { get; set; }
       public string Taxed { get; set; }
       public string CountryOfOrigin { get; set; }
       public string ItemColor { get; set; }
       public string ReOrderPoint { get; set; }
       public string Supplier { get; set; }
       public string Brand { get; set; }
       public string PreferredShippingCarrier { get; set; }
       public string PreferredShippingMethod { get; set; }
       public string USPSPackageType { get; set; }
       public string PictureUrl { get; set; }
       public string Gender { get; set; }
       public string Size { get; set; }
       public string SellerCost { get; set; }
       public string Price { get; set; }
       public string UPSSurepost { get; set; }

        public string ItemUM { get; set; }
        public string BackOrderDate { get; set; }
        public string BackOrderAvailableQTY { get; set; }
        public string Discontinued { get; set; }
        public string FulfillmentType { get; set; }
        public string ProductType { get; set; }
    }
    public class ItemDimensionsDetails
    {
        public string ItemWeight { get; set; }
        public string ItemWeightUnits { get; set; }
        public string ItemLength { get; set; }
        public string ItemLengthUnits { get; set; }
        public string ItemWidth { get; set; }
        public string ItemWidthUnits { get; set; }
        public string ItemHeight { get; set; }
        public string ItemHeightUnits { get; set; }

        public string ItemVolume { get; set; }
        public string ItemVolumeUnits { get; set; }
    }
    public class ItemPackagingDetails
    {
      public string PackageLength  { get; set; }
      public string PackageLengthUnits { get; set; }
      public string PackageWidth { get; set; }
      public string PackageWidthUnits { get; set; }
      public string PackageHeight { get; set; }
      public string PackageHeightUnits { get; set; }
      public string ItemVolume { get; set; }
      public string ItemVolumeUnits { get; set; }
      public string PackageWeight { get; set; }
      public string PackageWeightUnits { get; set; }
      public string QuantityinStock { get; set; }
      public string QuantityinStockUnits { get; set; }
      public string MinOrderQty { get; set; }
      public string MinOrderQtyUnits { get; set; }
      public string NumberofItemsPackage { get; set; }
    }
    public class WarehouseDetails
    {
        public string Quantity { get; set; }
    }

    public class ShipDetails
    {
        public string ShipWeight { get; set; }
        public string ShipWeightUnits { get; set; }
        public string ShipLength { get; set; }
        public string ShipLengthUnits { get; set; }
        public string ShipWidth { get; set; }
        public string ShipWidthUnits { get; set; }
        public string ShipHeight { get; set; }
        public string ShipHeightUnits { get; set; }
        public string ShipVolume { get; set; }
        public string ShipVolumeUnits { get; set; }
    }
    public class BoxDetails
    {
        public string BoxWeight { get; set; }
        public string BoxWeightUnits { get; set; }
        public string BoxLength { get; set; }
        public string BoxLengthUnits { get; set; }
        public string BoxWidth { get; set; }
        public string BoxWidthUnits { get; set; }
        public string BoxHeight { get; set; }
        public string BoxHeightUnits { get; set; }
        public string BoxVolume { get; set; }
        public string BoxVolumeUnits { get; set; }
        public string ItemsPerBox { get; set; }
    }
    public class CartonDetails
    {
        public string CartonWeight { get; set; }
        public string CartonWeightUnits { get; set; }
        public string CartonLength { get; set; }
        public string CartonLengthUnits { get; set; }
        public string CartonWidth { get; set; }
        public string CartonWidthUnits { get; set; }
        public string CartonHeight { get; set; }
        public string CartonHeightUnits { get; set; }
        public string CartonVolume { get; set; }
        public string CartonVolumeUnits { get; set; }
        public string ItemPerCarton { get; set; }
    }
}
