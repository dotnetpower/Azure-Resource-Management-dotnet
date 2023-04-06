namespace AzureMonitor.Models
{
    // make class from https://learn.microsoft.com/en-us/rest/api/resources/resources/list-by-resource-group#genericresourceexpanded

    public class GenericResourceExpanded
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string location { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public string kind { get; set; }
        public string managedBy { get; set; }
        public Sku sku { get; set; }
        public Plan plan { get; set; }
        public string properties { get; set; }
        public Identity identity { get; set; }
        public string zones { get; set; }
        public string extendedLocation { get; set; }
        public SystemData systemData { get; set; }
    }


}